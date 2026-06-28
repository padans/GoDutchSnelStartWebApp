namespace GoDutchSnelStartWebApp.Application.BankAccountSettings.Dtos;

public sealed class BankAccountSettingsDto
{
    public Guid Id { get; set; }
    public Guid BankAccountId { get; set; }

    public string? SnelStartAuthUrl { get; set; }
    public string? SnelStartApiBaseUrl { get; set; }
    public string? SnelStartClientKey { get; set; }

    public string ExportFormat { get; set; } = string.Empty;
    public bool SyncEnabled { get; set; }

    public bool HasSnelStartSubscriptionKey { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
}
