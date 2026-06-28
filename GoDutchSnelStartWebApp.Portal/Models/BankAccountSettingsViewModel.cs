namespace GoDutchSnelStartWebApp.Portal.Models;

public sealed class BankAccountSettingsViewModel
{
    public Guid? Id { get; set; }
    public Guid BankAccountId { get; set; }

    public string? SnelStartAuthUrl { get; set; }
    public string? SnelStartApiBaseUrl { get; set; }
    public string? SnelStartClientKey { get; set; }
    public string? SnelStartSubscriptionKey { get; set; }

    public string ExportFormat { get; set; } = "MT940";
    public bool SyncEnabled { get; set; } = true;

    public bool HasSnelStartSubscriptionKey { get; set; }
}
