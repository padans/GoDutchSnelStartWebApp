using GoDutchSnelStartWebApp.Domain.Enums;

namespace GoDutchSnelStartWebApp.Domain.Entities;

public sealed class BankAccountSetting
{
    public Guid Id { get; init; }
    public Guid BankAccountId { get; init; }

    public string? SnelStartAuthUrl { get; set; }
    public string? SnelStartApiBaseUrl { get; set; }
    public string? SnelStartClientKey { get; set; }
    public string? SnelStartSubscriptionKeyEncrypted { get; set; }

    public SnelStartExportFormat ExportFormat { get; set; }
    public bool SyncEnabled { get; set; }

    public DateTime CreatedUtc { get; init; }
    public DateTime? ModifiedUtc { get; set; }
}