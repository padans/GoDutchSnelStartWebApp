namespace GoDutchSnelStartWebApp.Portal.Models.MyPos;

public sealed class UpdateTenantMyPosConnectionRequestViewModel
{
    public string AuthUrl { get; set; } = string.Empty;
    public string TransactionsApiBaseUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;

    // Leeg/null laten betekent: bestaande encrypted waarde behouden.
    public string? ClientSecret { get; set; }

    // Leeg/null laten betekent: bestaande encrypted waarde behouden.
    public string? ApiKey { get; set; }

    public Guid? SnelStartBankDagboekId { get; set; }
    public string? SnelStartBankDagboekNummer { get; set; }
    public string? SnelStartBankDagboekNaam { get; set; }
    public string? SnelStartBankIban { get; set; }

    public bool IsActive { get; set; } = true;
}