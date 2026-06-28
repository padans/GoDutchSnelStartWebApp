namespace GoDutchSnelStartWebApp.Application.SnelStartConnections.Dtos;

public sealed class CreateTenantSnelStartConnectionRequest
{
    /// <summary>
    /// Current supported value: CustomKey. Future value: OAuth.
    /// </summary>
    public string ConnectionType { get; set; } = "CustomKey";

    public string AuthUrl { get; set; } = "https://auth.snelstart.nl/b2b/token";
    public string ApiBaseUrl { get; set; } = "https://b2bapi.snelstart.nl/v2";

    /// <summary>
    /// Required for CustomKey mode. Never returned by the API.
    /// </summary>
    public string? SubscriptionKey { get; set; }

    /// <summary>
    /// Required for CustomKey mode. Never returned by the API.
    /// </summary>
    public string? ClientKey { get; set; }

    public bool IsActive { get; set; } = true;
}
