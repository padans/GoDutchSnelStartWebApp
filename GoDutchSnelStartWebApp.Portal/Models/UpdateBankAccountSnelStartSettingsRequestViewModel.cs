namespace GoDutchSnelStartWebApp.Portal.Models;

public sealed class UpdateBankAccountSnelStartSettingsRequestViewModel
{
    public string AccountName { get; set; } = string.Empty;
    public string Iban { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public Guid? SnelStartDagboekId { get; set; }
    public string? SnelStartDagboekCode { get; set; }
    public string? SnelStartDagboekNaam { get; set; }

    public Guid? SnelStartGrootboekId { get; set; }
    public string? SnelStartGrootboekNummer { get; set; }
    public string? SnelStartGrootboekNaam { get; set; }
}