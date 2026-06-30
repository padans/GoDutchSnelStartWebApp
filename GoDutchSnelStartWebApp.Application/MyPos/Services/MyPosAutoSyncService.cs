using GoDutchSnelStartWebApp.Application.Abstractions.Repositories.MyPos;
using GoDutchSnelStartWebApp.Application.Configuration;
using GoDutchSnelStartWebApp.Application.MyPos.Dtos;
using GoDutchSnelStartWebApp.Application.MyPos.Interfaces;
using GoDutchSnelStartWebApp.Application.Notifications.Dtos;
using GoDutchSnelStartWebApp.Application.Notifications.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GoDutchSnelStartWebApp.Application.MyPos.Services;

public sealed class MyPosAutoSyncService : IMyPosAutoSyncService
{
    private readonly ITenantMyPosConnectionRepository _connectionRepository;
    private readonly IMyPosTransactionImportService _importService;
    private readonly IMyPosExportBatchService _exportBatchService;
    private readonly IMyPosExportBatchExportService _exportService;
    private readonly IMyPosAutoSyncSettingsRepository _settingsRepository;
    private readonly IMyPosTransactionTypeMappingRepository _mappingRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly ILogger<MyPosAutoSyncService> _logger;
    private readonly MyPosAutoSyncOptions _options;

    public MyPosAutoSyncService(
        ITenantMyPosConnectionRepository connectionRepository,
        IMyPosTransactionImportService importService,
        IMyPosExportBatchService exportBatchService,
        IMyPosExportBatchExportService exportService,
        IMyPosAutoSyncSettingsRepository settingsRepository,
        IMyPosTransactionTypeMappingRepository mappingRepository,
        INotificationRepository notificationRepository,
        IEmailNotificationService emailNotificationService,
        ILogger<MyPosAutoSyncService> logger,
        IOptions<MyPosAutoSyncOptions> options)
    {
        _connectionRepository = connectionRepository;
        _importService = importService;
        _exportBatchService = exportBatchService;
        _exportService = exportService;
        _settingsRepository = settingsRepository;
        _mappingRepository = mappingRepository;
        _notificationRepository = notificationRepository;
        _emailNotificationService = emailNotificationService;
        _logger = logger;
        _options = options.Value;
    }

    public async Task RunOnceAsync(CancellationToken cancellationToken = default)
    {
        var lookbackHours = await GetLookbackHoursAsync(cancellationToken);
        var toUtc = DateTime.UtcNow;
        var fromUtc = toUtc.AddHours(-lookbackHours);

        var connections = await _connectionRepository.GetAllActiveAsync(cancellationToken);

        _logger.LogInformation(
            "myPOS auto sync gestart. Actieve koppelingen: {Count}. Periode: {FromUtc} - {ToUtc}.",
            connections.Count,
            fromUtc,
            toUtc);

        foreach (var connection in connections)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var importResult = await _importService.FetchAndStoreAsync(
                    connection.TenantId,
                    connection.Id,
                    fromUtc,
                    toUtc,
                    cancellationToken);

                _logger.LogInformation(
                    "myPOS transacties opgehaald voor koppeling {ConnectionId}, tenant {TenantId}. Opgehaald: {Fetched}, Verwerkt: {Processed}.",
                    connection.Id,
                    connection.TenantId,
                    importResult.FetchedCount,
                    importResult.InsertedOrUpdatedCount);

                var unmappedTypes = await GetUnmappedTransactionTypesAsync(
                    connection.TenantId,
                    fromUtc,
                    toUtc,
                    cancellationToken);

                if (unmappedTypes.Count > 0)
                {
                    _logger.LogWarning(
                        "myPOS auto sync gestopt voor tenant {TenantId}: {Count} transactietype(s) zonder actieve SnelStart-koppeling gevonden. " +
                        "Exportbatch wordt NIET aangemaakt totdat alle types gekoppeld zijn. Ontbrekende types: {Types}",
                        connection.TenantId,
                        unmappedTypes.Count,
                        string.Join(", ", unmappedTypes));

                    await SendUnmappedTypesNotificationAsync(
                        connection.TenantId,
                        unmappedTypes,
                        cancellationToken);

                    continue;
                }

                await CreateAndExportBatchAsync(connection.TenantId, connection.Id, fromUtc, toUtc, cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(
                    ex,
                    "myPOS auto sync mislukt voor koppeling {ConnectionId}, tenant {TenantId}.",
                    connection.Id,
                    connection.TenantId);
            }
        }

        _logger.LogInformation("myPOS auto sync pollronde afgerond.");
    }

    private async Task SendUnmappedTypesNotificationAsync(
        Guid tenantId,
        IReadOnlyList<string> unmappedTypes,
        CancellationToken cancellationToken)
    {
        var typesList = string.Join(", ", unmappedTypes);
        var title = $"myPOS: {unmappedTypes.Count} ongemapte transactietype(s)";
        var message =
            $"De myPOS auto-sync voor tenant {tenantId} is gestopt omdat de volgende transactietypes " +
            $"niet gekoppeld zijn aan een SnelStart-grootboekrekening:\n\n{typesList}\n\n" +
            $"Koppel deze types via het portal onder myPOS > Transactietype mappings.";

        try
        {
            await _notificationRepository.InsertAsync(new NotificationDto
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Severity = "Warning",
                Title = title,
                Message = message,
                CreatedUtc = DateTime.UtcNow
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "In-app notificatie opslaan mislukt voor tenant {TenantId}.", tenantId);
        }

        await _emailNotificationService.SendAsync(
            subject: $"[Actie vereist] {title}",
            body: message,
            cancellationToken);
    }

    private async Task<IReadOnlyList<string>> GetUnmappedTransactionTypesAsync(
        Guid tenantId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        var transactions = await _importService.GetRawTransactionsAsync(tenantId, fromUtc, toUtc, cancellationToken);

        if (transactions.Count == 0)
        {
            return [];
        }

        var typesInPeriod = transactions
            .Select(t => t.TransactionType)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var mappings = await _mappingRepository.GetByTenantAsync(tenantId, cancellationToken);

        var mappedTypes = mappings
            .Where(m => m.IsActive && m.SnelStartGrootboek is not null)
            .Select(m => m.TransactionCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return typesInPeriod
            .Where(type => !mappedTypes.Contains(type))
            .OrderBy(type => type)
            .ToList();
    }

    private async Task CreateAndExportBatchAsync(
        Guid tenantId,
        Guid connectionId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        var batch = await _exportBatchService.CreateConceptBatchAsync(
            tenantId,
            new CreateMyPosExportBatchRequest
            {
                TenantMyPosConnectionId = connectionId,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                IncludeExported = false
            },
            cancellationToken);

        if (batch.RawTransactionCount == 0)
        {
            _logger.LogInformation(
                "myPOS auto sync: geen nieuwe transacties voor tenant {TenantId}. Geen exportbatch aangemaakt.",
                tenantId);
            return;
        }

        _logger.LogInformation(
            "myPOS exportbatch aangemaakt voor tenant {TenantId}. BatchId: {BatchId}, Ready: {Ready}, Regels: {Lines}, Bedrag: {Amount:C}. {ValidationMessage}",
            tenantId,
            batch.Id,
            batch.IsReadyForExport,
            batch.LineCount,
            batch.TotalAmount,
            batch.ValidationMessage);

        if (!batch.IsReadyForExport)
        {
            _logger.LogWarning(
                "myPOS auto sync: batch {BatchId} voor tenant {TenantId} is niet klaar voor export. Batch is opgeslagen maar niet geëxporteerd. Reden: {ValidationMessage}",
                batch.Id,
                tenantId,
                batch.ValidationMessage);
            return;
        }

        var exportResult = await _exportService.ExportToSnelStartBankboekAsync(tenantId, batch.Id, cancellationToken);

        if (exportResult.Success)
        {
            _logger.LogInformation(
                "myPOS auto sync: batch {BatchId} voor tenant {TenantId} succesvol geëxporteerd naar SnelStart. Regels: {Exported}, Referentie: {Reference}.",
                batch.Id,
                tenantId,
                exportResult.ExportedLineCount,
                exportResult.SnelStartReference);
        }
        else
        {
            _logger.LogError(
                "myPOS auto sync: export van batch {BatchId} voor tenant {TenantId} mislukt. Regels mislukt: {Failed}. Bericht: {Message}",
                batch.Id,
                tenantId,
                exportResult.FailedLineCount,
                exportResult.Message);
        }
    }

    private async Task<int> GetLookbackHoursAsync(CancellationToken cancellationToken)
    {
        try
        {
            var dbSettings = await _settingsRepository.GetAsync(cancellationToken);
            if (dbSettings is not null)
            {
                return dbSettings.LookbackHours < 1 ? 1 : dbSettings.LookbackHours;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "myPOS LookbackHours uit database ophalen mislukt, appsettings-waarde wordt gebruikt.");
        }

        return _options.LookbackHours < 1 ? 1 : _options.LookbackHours;
    }
}
