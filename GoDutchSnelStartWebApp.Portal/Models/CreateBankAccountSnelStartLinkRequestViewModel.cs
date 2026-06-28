namespace GoDutchSnelStartWebApp.Portal.Models;

public sealed class CreateBankAccountSnelStartLinkRequestViewModel
{
    public Guid BankAccountId { get; set; }
    public Guid SnelStartAdministrationId { get; set; }

    public string ExportFormat { get; set; } = "Mt940";
    public bool IsActive { get; set; } = true;

    public bool AutoSyncEnabled { get; set; } = true;
    public int SyncIntervalMinutes { get; set; } = 15;
}
