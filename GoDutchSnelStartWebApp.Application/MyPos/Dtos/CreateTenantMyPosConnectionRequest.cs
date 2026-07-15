namespace GoDutchSnelStartWebApp.Application.MyPos.Dtos;

public sealed class CreateTenantMyPosConnectionRequest
{
    public string? Name { get; set; }

    public string AuthUrl { get; set; } = "https://auth-api.mypos.com/oauth/token";
    public string TransactionsApiBaseUrl { get; set; } = "https://transactions-api.mypos.com/v1.1";
    public string ClientId { get; set; } = string.Empty;
    
    // leeg laten = bestaande encrypted waarde behouden
    public string? ClientSecret { get; set; }

    // leeg laten = bestaande encrypted waarde behouden
    public string? ApiKey { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? SnelStartBankDagboekId { get; set; }
    public string? SnelStartBankDagboekNummer { get; set; }
    public string? SnelStartBankDagboekNaam { get; set; }
    public string? SnelStartBankIban { get; set; }
}
