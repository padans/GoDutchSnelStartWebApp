using GoDutchSnelStartWebApp.Domain.Enums;

namespace GoDutchSnelStartWebApp.Domain.Entities;

public sealed class GoDutchImportRun
{
    private GoDutchImportRun() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid BankAccountId { get; private set; }
    public string Iban { get; private set; } = string.Empty;

    public DateTime PeriodFromUtc { get; private set; }
    public DateTime PeriodToUtc { get; private set; }

    public ImportRunTriggerSource TriggerSource { get; private set; }
    public ImportRunStatus Status { get; private set; }

    public int TransactionCount { get; private set; }
    public string? Message { get; private set; }

    public DateTime StartedUtc { get; private set; }
    public DateTime? CompletedUtc { get; private set; }

    public int RetryCount { get; private set; }

    public bool WasRetried => RetryCount > 0;

    public static GoDutchImportRun Start(
        Guid tenantId,
        Guid bankAccountId,
        string iban,
        DateTime periodFrom,
        DateTime periodTo,
        ImportRunTriggerSource triggerSource)
    {
        return new GoDutchImportRun
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BankAccountId = bankAccountId,
            Iban = iban,
            PeriodFromUtc = periodFrom,
            PeriodToUtc = periodTo,
            TriggerSource = triggerSource,
            Status = ImportRunStatus.Started,
            StartedUtc = DateTime.UtcNow
        };
    }

    public void MarkSucceeded(int transactionCount, int retryCount = 0, string? message = null)
    {
        Status = ImportRunStatus.Succeeded;
        TransactionCount = transactionCount;
        RetryCount = retryCount;
        Message = message;
        CompletedUtc = DateTime.UtcNow;
    }

    public void MarkSkipped(int retryCount = 0, string? message = null)
    {
        Status = ImportRunStatus.Skipped;
        RetryCount = retryCount;
        Message = message;
        CompletedUtc = DateTime.UtcNow;
    }

    public void MarkFailed(int retryCount = 0, string? message = null)
    {
        Status = ImportRunStatus.Failed;
        RetryCount = retryCount;
        Message = message;
        CompletedUtc = DateTime.UtcNow;
    }

    public static GoDutchImportRun Reconstitute(
        Guid id,
        Guid tenantId,
        Guid bankAccountId,
        string iban,
        DateTime periodFrom,
        DateTime periodTo,
        ImportRunTriggerSource triggerSource,
        ImportRunStatus status,
        int transactionCount,
        int retryCount,
        string? message,
        DateTime startedUtc,
        DateTime? completedUtc)
    {
        return new GoDutchImportRun
        {
            Id = id,
            TenantId = tenantId,
            BankAccountId = bankAccountId,
            Iban = iban,
            PeriodFromUtc = periodFrom,
            PeriodToUtc = periodTo,
            TriggerSource = triggerSource,
            Status = status,
            TransactionCount = transactionCount,
            RetryCount = retryCount,
            Message = message,
            StartedUtc = startedUtc,
            CompletedUtc = completedUtc
        };
    }
}
