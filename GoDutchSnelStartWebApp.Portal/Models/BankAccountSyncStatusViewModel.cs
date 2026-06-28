namespace GoDutchSnelStartWebApp.Portal.Models;

public sealed class BankAccountSyncStatusViewModel
{
    public string Status { get; set; } = "NeverRun";
    public DateTime? LastRunAt { get; set; }
    public int TransactionCount { get; set; }
    public int RetryCount { get; set; }
    public string? TriggerSource { get; set; }
    public string? Message { get; set; }
}