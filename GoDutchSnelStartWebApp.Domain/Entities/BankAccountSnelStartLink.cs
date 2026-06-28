using GoDutchSnelStartWebApp.Domain.Enums;

namespace GoDutchSnelStartWebApp.Domain.Entities;

public sealed class BankAccountSnelStartLink
{
    public Guid Id { get; init; }
    public Guid BankAccountId { get; set; }
    public Guid SnelStartAdministrationId { get; set; }

    public SnelStartExportFormat ExportFormat { get; set; }
    public bool IsActive { get; set; }

    public bool AutoSyncEnabled { get; set; } = true;
    public int SyncIntervalMinutes { get; set; } = 15;
    public DateTime? LastRunUtc { get; set; }
    public DateTime? NextRunUtc { get; set; }

    public DateTime CreatedUtc { get; init; }
    public DateTime? ModifiedUtc { get; set; }
}
