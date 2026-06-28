using GoDutchSnelStartWebApp.Application.Abstractions.Repositories.MyPos;
using GoDutchSnelStartWebApp.Application.MyPos.Dtos;
using GoDutchSnelStartWebApp.Application.MyPos.Interfaces;
using GoDutchSnelStartWebApp.Domain.Entities.MyPos;
using GoDutchSnelStartWebApp.Domain.Enums;
using GoDutchSnelStartWebApp.Domain.ValueObjects;

namespace GoDutchSnelStartWebApp.Application.MyPos.Services;

public sealed class MyPosExportBatchService : IMyPosExportBatchService
{
    private readonly IMyPosExportBatchRepository _exportBatchRepository;
    private readonly IMyPosRawTransactionRepository _rawTransactionRepository;
    private readonly IMyPosTransactionTotalService _transactionTotalService;
    private readonly ITenantMyPosConnectionRepository _connectionRepository;

    public MyPosExportBatchService(
     IMyPosExportBatchRepository exportBatchRepository,
     IMyPosRawTransactionRepository rawTransactionRepository,
     IMyPosTransactionTotalService transactionTotalService,
     ITenantMyPosConnectionRepository connectionRepository)
    {
        _exportBatchRepository = exportBatchRepository;
        _rawTransactionRepository = rawTransactionRepository;
        _transactionTotalService = transactionTotalService;
        _connectionRepository = connectionRepository;
    }

    private sealed record BookYearValidationResult(
    bool IsValid,
    int? BookYear,
    DateTime PeriodFromLocalDate,
    DateTime PeriodToLocalDate,
    string Message);

    public async Task<MyPosExportBatchDto> CreateConceptBatchAsync(
        Guid tenantId,
        CreateMyPosExportBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (request.FromUtc == default || request.ToUtc == default)
        {
            throw new ArgumentException("FromUtc and ToUtc are required.", nameof(request));
        }

        if (request.ToUtc <= request.FromUtc)
        {
            throw new ArgumentException("ToUtc must be later than FromUtc.", nameof(request));
        }

        var bookYearValidation = ValidateBookYearPeriod(
                request.FromUtc,
                request.ToUtc);

        var connection = request.TenantMyPosConnectionId.HasValue
            ? await _connectionRepository.GetByIdAsync(request.TenantMyPosConnectionId.Value, cancellationToken)
            : await _connectionRepository.GetByTenantIdAsync(tenantId, cancellationToken);

        if (connection is null || connection.TenantId != tenantId)
        {
            throw new InvalidOperationException("myPOS connection not found for this tenant.");
        }

        var rawTransactions = request.IncludeExported
            ? await _rawTransactionRepository.GetByTenantAsync(tenantId, request.FromUtc, request.ToUtc, cancellationToken)
            : await _rawTransactionRepository.GetUnexportedAsync(tenantId, request.FromUtc, request.ToUtc, cancellationToken);

        var totals = await _transactionTotalService.GetTotalsAsync(
            tenantId,
            request.FromUtc,
            request.ToUtc,
            request.IncludeExported,
            cancellationToken);

        var nowUtc = DateTime.UtcNow;
        var lines = totals
            .OrderBy(x => x.TransactionType)
            .ThenBy(x => x.Currency)
            .Select(total => new MyPosExportBatchLine
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                TransactionType = total.TransactionType,
                Description = total.Description,
                TransactionCount = total.TransactionCount,
                TotalAmount = total.TotalAmount,
                Currency = total.Currency,
                FirstTransactionUtc = total.FirstTransactionUtc,
                LastTransactionUtc = total.LastTransactionUtc,
                SnelStartGrootboek = total.SnelStartGrootboekId is not null
                    ? new SnelStartGrootboekRef(total.SnelStartGrootboekId.Value, total.SnelStartGrootboekNummer ?? string.Empty, total.SnelStartGrootboekNaam ?? string.Empty)
                    : null,
                BtwBerekening = total.BtwBerekening,
                BtwSoort = total.BtwSoort,
                BtwPercentage = total.BtwPercentage,
                HasMapping = total.HasMapping,
                HasActiveMapping = total.HasActiveMapping,
                IsReadyForExport = total.IsReadyForExport,
                MappingWarning = total.MappingWarning,
                CreatedUtc = nowUtc
            })
            .ToList();

        var bankConfigurationIsReady =
             connection.IsActive &&
             connection.SnelStartBankDagboek is not null &&
             !string.IsNullOrWhiteSpace(connection.SnelStartBankIban);

        var linesAreReady = lines.Count > 0 && lines.All(x => x.IsReadyForExport);
        var isReadyForExport =
            bookYearValidation.IsValid &&
            bankConfigurationIsReady &&
            linesAreReady;

        var validationMessage = BuildValidationMessage(
            rawTransactions.Count,
            lines,
            connection.IsActive,
            connection.SnelStartBankDagboek is not null,
            connection.SnelStartBankIban,
            bookYearValidation);

        var batch = new MyPosExportBatch
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TenantMyPosConnectionId = connection.Id,
            ExportTarget = MyPosExportTarget.SnelStartBankboek,
            SnelStartBankDagboek = connection.SnelStartBankDagboek,
            BookYear = bookYearValidation.BookYear,
            PeriodFromLocalDate = bookYearValidation.PeriodFromLocalDate,
            PeriodToLocalDate = bookYearValidation.PeriodToLocalDate,
            BookYearValidationMessage = bookYearValidation.Message,
            SnelStartBankIban = connection.SnelStartBankIban,

            PeriodFromUtc = request.FromUtc,
            PeriodToUtc = request.ToUtc,
            Status = isReadyForExport ? MyPosExportBatchStatus.Ready : MyPosExportBatchStatus.Concept,
            Currency = lines.Select(x => x.Currency).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1
                ? lines.FirstOrDefault()?.Currency ?? "EUR"
                : "Mixed",
            RawTransactionCount = rawTransactions.Count,
            LineCount = lines.Count,
            TotalAmount = lines.Sum(x => x.TotalAmount),
            IsReadyForExport = isReadyForExport,
            ValidationMessage = validationMessage,
            CreatedUtc = nowUtc,
            Lines = lines
        };

        foreach (var line in batch.Lines)
        {
            line.BatchId = batch.Id;
            line.TenantId = tenantId;
        }

        await _exportBatchRepository.CreateAsync(batch, cancellationToken);

        return Map(batch);
    }

    public async Task<MyPosExportBatchDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var batch = await _exportBatchRepository.GetByIdAsync(id, cancellationToken);
        return batch is null ? null : Map(batch);
    }

    public async Task<IReadOnlyList<MyPosExportBatchDto>> GetByTenantAsync(
        Guid tenantId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var batches = await _exportBatchRepository.GetByTenantAsync(tenantId, fromUtc, toUtc, cancellationToken);
        return batches.Select(Map).ToList();
    }

    private static string BuildValidationMessage(
     int rawTransactionCount,
     IReadOnlyCollection<MyPosExportBatchLine> lines,
     bool connectionIsActive,
     bool hasSnelStartBankDagboek,
     string? snelStartBankIban,
     BookYearValidationResult bookYearValidation)
    {
        if (!bookYearValidation.IsValid)
        {
            return bookYearValidation.Message;
        }

        if (!connectionIsActive)
        {
            return "myPOS koppeling is niet actief.";
        }

        if (!hasSnelStartBankDagboek)
        {
            return "myPOS koppeling is nog niet gekoppeld aan een SnelStart-bankdagboek.";
        }

        if (string.IsNullOrWhiteSpace(snelStartBankIban))
        {
            return "myPOS koppeling heeft nog geen virtuele SnelStart-bankrekening/IBAN.";
        }

        if (rawTransactionCount == 0)
        {
            return "Geen raw myPOS transacties gevonden voor deze periode.";
        }

        if (lines.Count == 0)
        {
            return "Geen totalisatieregels gevonden voor deze periode.";
        }

        var missingCount = lines.Count(x => !x.IsReadyForExport);

        if (missingCount > 0)
        {
            return $"Conceptbatch aangemaakt. {missingCount} totalisatieregel(s) zijn nog niet klaar voor export. Controleer de myPOS mappings.";
        }

        return $"Batch is klaar voor export naar SnelStart-bankboek voor boekjaar {bookYearValidation.BookYear}.";
    }
    private static MyPosExportBatchDto Map(MyPosExportBatch batch)
    {
        return new MyPosExportBatchDto
        {
            Id = batch.Id,
            TenantId = batch.TenantId,
            TenantMyPosConnectionId = batch.TenantMyPosConnectionId,
            ExportTarget = batch.ExportTarget.ToString(),
            SnelStartBankDagboekId = batch.SnelStartBankDagboek?.Id,
            SnelStartBankDagboekNummer = batch.SnelStartBankDagboek?.Code,
            SnelStartBankDagboekNaam = batch.SnelStartBankDagboek?.Naam,
            SnelStartBankIban = batch.SnelStartBankIban,
            BookYear = batch.BookYear,
            PeriodFromLocalDate = batch.PeriodFromLocalDate,
            PeriodToLocalDate = batch.PeriodToLocalDate,
            BookYearValidationMessage = batch.BookYearValidationMessage,
            PeriodFromUtc = batch.PeriodFromUtc,
            PeriodToUtc = batch.PeriodToUtc,
            Status = batch.Status.ToString(),
            Currency = batch.Currency,
            RawTransactionCount = batch.RawTransactionCount,
            LineCount = batch.LineCount,
            TotalAmount = batch.TotalAmount,
            IsReadyForExport = batch.IsReadyForExport,
            ValidationMessage = batch.ValidationMessage,
            SnelStartReference = batch.SnelStartReference,
            ExportedUtc = batch.ExportedUtc,
            ErrorMessage = batch.ErrorMessage,
            CreatedUtc = batch.CreatedUtc,
            ModifiedUtc = batch.ModifiedUtc,
            Lines = batch.Lines.Select(MapLine).ToList()
        };
    }

    private static MyPosExportBatchLineDto MapLine(MyPosExportBatchLine line)
    {
        return new MyPosExportBatchLineDto
        {
            Id = line.Id,
            BatchId = line.BatchId,
            TenantId = line.TenantId,
            TransactionType = line.TransactionType,
            Description = line.Description,
            TransactionCount = line.TransactionCount,
            TotalAmount = line.TotalAmount,
            Currency = line.Currency,
            FirstTransactionUtc = line.FirstTransactionUtc,
            LastTransactionUtc = line.LastTransactionUtc,
            SnelStartGrootboekId = line.SnelStartGrootboek?.Id,
            SnelStartGrootboekNummer = line.SnelStartGrootboek?.Nummer,
            SnelStartGrootboekNaam = line.SnelStartGrootboek?.Naam,
            BtwBerekening = line.BtwBerekening,
            BtwSoort = line.BtwSoort,
            BtwPercentage = line.BtwPercentage,
            HasMapping = line.HasMapping,
            HasActiveMapping = line.HasActiveMapping,
            IsReadyForExport = line.IsReadyForExport,
            MappingWarning = line.MappingWarning
        };
    }

    private static BookYearValidationResult ValidateBookYearPeriod(
    DateTime fromUtc,
    DateTime toUtc)
    {
        var timeZone = GetDutchTimeZone();

        var fromLocal = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc),
            timeZone);

        var toLocal = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(toUtc, DateTimeKind.Utc),
            timeZone);

        var fromLocalDate = fromLocal.Date;
        var toLocalDate = toLocal.Date;

        if (fromLocalDate.Year != toLocalDate.Year)
        {
            return new BookYearValidationResult(
                IsValid: false,
                BookYear: null,
                PeriodFromLocalDate: fromLocalDate,
                PeriodToLocalDate: toLocalDate,
                Message:
                    $"De exportperiode loopt over meerdere boekjaren: {fromLocalDate:dd-MM-yyyy} t/m {toLocalDate:dd-MM-yyyy}. " +
                    "Kies een periode binnen één boekjaar.");
        }

        return new BookYearValidationResult(
            IsValid: true,
            BookYear: fromLocalDate.Year,
            PeriodFromLocalDate: fromLocalDate,
            PeriodToLocalDate: toLocalDate,
            Message: $"Periode valt binnen boekjaar {fromLocalDate.Year}.");
    }

    private static TimeZoneInfo GetDutchTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");
        }
    }
}
