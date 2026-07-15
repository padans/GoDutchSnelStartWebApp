namespace GoDutchSnelStartWebApp.Application.MyPos.Dtos;

public sealed class UpdateTenantMyPosConnectionRequest
{
    public string? Name { get; set; }

    public string AuthUrl { get; set; } = string.Empty;
    public string TransactionsApiBaseUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;

    // Leave empty/null to keep existing encrypted value.
    public string? ClientSecret { get; set; }

    // Leave empty/null to keep existing encrypted value.
    public string? ApiKey { get; set; }

    public Guid? SnelStartBankDagboekId { get; set; }
    public string? SnelStartBankDagboekNummer { get; set; }
    public string? SnelStartBankDagboekNaam { get; set; }
    public string? SnelStartBankIban { get; set; }

    public bool IsActive { get; set; } = true;
}