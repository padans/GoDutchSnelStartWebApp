namespace GoDutchSnelStartWebApp.Application.BankAccountSettings.Dtos;

public sealed class UpdateBankAccountSettingsRequest
{
    public string? SnelStartAuthUrl { get; set; }
    public string? SnelStartApiBaseUrl { get; set; }
    public string? SnelStartClientKey { get; set; }
    public string? SnelStartSubscriptionKey { get; set; }

    public string ExportFormat { get; set; } = "MT940";
    public bool SyncEnabled { get; set; } = true;
}
