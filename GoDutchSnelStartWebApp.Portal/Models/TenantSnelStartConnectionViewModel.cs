namespace GoDutchSnelStartWebApp.Portal.Models;

public sealed class TenantSnelStartConnectionViewModel
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string ConnectionType { get; set; } = "CustomKey";
    public string AuthUrl { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = string.Empty;
    public bool HasSubscriptionKey { get; set; }
    public bool HasClientKey { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
}

public sealed class UpdateTenantSnelStartConnectionRequestViewModel
{
    public string ConnectionType { get; set; } = "CustomKey";
    public string AuthUrl { get; set; } = "https://auth.snelstart.nl/b2b/token";
    public string ApiBaseUrl { get; set; } = "https://b2bapi.snelstart.nl/v2";
    public string? SubscriptionKey { get; set; }
    public string? ClientKey { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CreateTenantSnelStartConnectionRequestViewModel
{
    public string ConnectionType { get; set; } = "CustomKey";
    public string AuthUrl { get; set; } = "https://auth.snelstart.nl/b2b/token";
    public string ApiBaseUrl { get; set; } = "https://b2bapi.snelstart.nl/v2";
    public string? SubscriptionKey { get; set; }
    public string? ClientKey { get; set; }
    public bool IsActive { get; set; } = true;
}
