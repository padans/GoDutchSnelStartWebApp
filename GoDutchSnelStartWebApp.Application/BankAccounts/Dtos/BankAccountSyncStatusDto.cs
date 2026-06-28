namespace GoDutchSnelStartWebApp.Application.BankAccounts.Dtos;

public sealed class BankAccountSyncStatusDto
{
    public string Status { get; init; } = string.Empty;
    public DateTime? LastRunAt { get; init; }
    public int TransactionCount { get; init; }
    public int RetryCount { get; init; }
    public string? TriggerSource { get; init; }
    public string? Message { get; init; }
}
