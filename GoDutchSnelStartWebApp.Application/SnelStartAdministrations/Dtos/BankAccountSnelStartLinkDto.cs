namespace GoDutchSnelStartWebApp.Application.SnelStartAdministrations.Dtos;

public sealed class BankAccountSnelStartLinkDto
{
    public Guid Id { get; set; }
    public Guid BankAccountId { get; set; }
    public Guid SnelStartAdministrationId { get; set; }

    public string ExportFormat { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public bool AutoSyncEnabled { get; set; }
    public int SyncIntervalMinutes { get; set; }
    public DateTime? LastRunUtc { get; set; }
    public DateTime? NextRunUtc { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
}
