namespace GoDutchSnelStartWebApp.Portal.Models.MyPos;

public sealed class CreateMyPosConnectionRequestViewModel
{
    public string AuthUrl { get; set; } = "https://auth-api.mypos.com/oauth/token";
    public string TransactionsApiBaseUrl { get; set; } = "https://transactions-api.mypos.com/v1.1";
    public string ClientId { get; set; } = string.Empty;
    public string? ClientSecret { get; set; }
    public string? ApiKey { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? SnelStartBankDagboekId { get; set; }
    public string? SnelStartBankDagboekNummer { get; set; }
    public string? SnelStartBankDagboekNaam { get; set; }
    public string? SnelStartBankIban { get; set; } = "IE91MPOS99039025035759";
}
