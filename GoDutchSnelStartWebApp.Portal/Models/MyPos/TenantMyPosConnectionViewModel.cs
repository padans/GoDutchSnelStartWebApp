namespace GoDutchSnelStartWebApp.Portal.Models.MyPos;

public sealed class TenantMyPosConnectionViewModel
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public string? Name { get; set; }

    public string? AuthUrl { get; set; }
    public string? TransactionsApiBaseUrl { get; set; }
    public string? ClientId { get; set; }

    public bool HasClientSecret { get; set; }
    public bool HasApiKey { get; set; }

    public Guid? SnelStartBankDagboekId { get; set; }
    public string? SnelStartBankDagboekNummer { get; set; }
    public string? SnelStartBankDagboekNaam { get; set; }
    public string? SnelStartBankIban { get; set; }

    public bool IsActive { get; set; }
}