namespace GoDutchSnelStartWebApp.Application.BankAccounts.Dtos;

public sealed class UpdateBankAccountRequest
{
    public string Iban { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    // SnelStart grootboek
    public Guid? SnelStartGrootboekId { get; set; }
    public string? SnelStartGrootboekNummer { get; set; }
    public string? SnelStartGrootboekNaam { get; set; }

    // SnelStart dagboek
    public Guid? SnelStartDagboekId { get; set; }
    public string? SnelStartDagboekCode { get; set; }
    public string? SnelStartDagboekNaam { get; set; }
}