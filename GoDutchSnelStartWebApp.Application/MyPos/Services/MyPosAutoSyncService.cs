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
        var dbSettings = await GetSettingsAsync(cancellationToken);
        var (fromUtc, toUtc) = ResolveRange(dbSettings);

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

    public async Task<MyPosAutoSyncSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var db = await _settingsRepository.GetAsync(cancellationToken);
            if (db is not null) return db;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "myPOS auto-sync instellingen uit database ophalen mislukt, appsettings-waarde wordt gebruikt.");
        }

        return new MyPosAutoSyncSettingsDto
        {
            Enabled = _options.Enabled,
            IntervalMinutes = _options.IntervalMinutes < 1 ? 1 : _options.IntervalMinutes,
            SyncMode = "Lookback",
            LookbackHours = _options.LookbackHours < 1 ? 1 : _options.LookbackHours,
            PeriodType = "Day"
        };
    }

    public async Task UpdateSettingsAsync(MyPosAutoSyncSettingsDto settings, CancellationToken cancellationToken = default)
    {
        var normalized = new MyPosAutoSyncSettingsDto
        {
            Enabled         = settings.Enabled,
            IntervalMinutes = settings.IntervalMinutes < 1 ? 1 : settings.IntervalMinutes,
            SyncMode        = string.IsNullOrWhiteSpace(settings.SyncMode)  ? "Lookback" : settings.SyncMode,
            LookbackHours   = settings.LookbackHours < 1 ? 1 : settings.LookbackHours,
            PeriodType      = string.IsNullOrWhiteSpace(settings.PeriodType) ? "Day"     : settings.PeriodType
        };

        await _settingsRepository.UpsertAsync(normalized, cancellationToken);

        _logger.LogInformation(
            "myPOS auto-sync instellingen bijgewerkt. Enabled: {Enabled}, Interval: {Interval} min, SyncMode: {SyncMode}, Lookback: {Lookback} uur, PeriodType: {PeriodType}.",
            normalized.Enabled,
            normalized.IntervalMinutes,
            normalized.SyncMode,
            normalized.LookbackHours,
            normalized.PeriodType);
    }

    private (DateTime FromUtc, DateTime ToUtc) ResolveRange(MyPosAutoSyncSettingsDto settings)
    {
        if (string.Equals(settings.SyncMode, "Period", StringComparison.OrdinalIgnoreCase))
        {
            return CalculatePeriodRange(settings.PeriodType);
        }

        var toUtc = DateTime.UtcNow;
        var hours = settings.LookbackHours < 1 ? 1 : settings.LookbackHours;
        return (toUtc.AddHours(-hours), toUtc);
    }

    private static (DateTime FromUtc, DateTime ToUtc) CalculatePeriodRange(string periodType)
    {
        var nlZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        var nowNl = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, nlZone);

        DateTime fromNl, toNl;

        switch (periodType?.ToUpperInvariant())
        {
            case "WEEK":
                // Vorige maandag t/m vorige zondag
                var daysFromMonday = ((int)nowNl.DayOfWeek + 6) % 7;
                var thisMonday = nowNl.Date.AddDays(-daysFromMonday);
                fromNl = thisMonday.AddDays(-7);
                toNl   = thisMonday.AddTicks(-1);
                break;

            case "MONTH":
                var firstThisMonth = new DateTime(nowNl.Year, nowNl.Month, 1);
                fromNl = firstThisMonth.AddMonths(-1);
                toNl   = firstThisMonth.AddTicks(-1);
                break;

            case "QUARTER":
                var currentQ = (nowNl.Month - 1) / 3;
                var firstThisQ = new DateTime(nowNl.Year, currentQ * 3 + 1, 1);
                fromNl = firstThisQ.AddMonths(-3);
                toNl   = firstThisQ.AddTicks(-1);
                break;

            case "YEAR":
                fromNl = new DateTime(nowNl.Year - 1, 1, 1);
                toNl   = new DateTime(nowNl.Year, 1, 1).AddTicks(-1);
                break;

            default: // DAY
                fromNl = nowNl.Date.AddDays(-1);
                toNl   = nowNl.Date.AddTicks(-1);
                break;
        }

        return (
            TimeZoneInfo.ConvertTimeToUtc(fromNl, nlZone),
            TimeZoneInfo.ConvertTimeToUtc(toNl, nlZone)
        );
    }
}
