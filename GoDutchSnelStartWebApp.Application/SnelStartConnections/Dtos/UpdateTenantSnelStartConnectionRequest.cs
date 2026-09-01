namespace GoDutchSnelStartWebApp.Application.SnelStartConnections.Dtos;

public sealed class UpdateTenantSnelStartConnectionRequest
{
    /// <summary>
    /// Current supported value: CustomKey. Future value: OAuth.
    /// </summary>
    public string ConnectionType { get; set; } = "CustomKey";

    public string AuthUrl { get; set; } = "https://auth.snelstart.nl/b2b/token";
    public string ApiBaseUrl { get; set; } = "https://b2bapi.snelstart.nl/v2";

    /// <summary>
    /// Free-text name of the SnelStart administration. Null = keep the existing value;
    /// empty string = clear it; any other value overwrites.
    /// </summary>
    public string? AdministrationName { get; set; }

    /// <summary>
    /// Leave empty/null to keep the existing encrypted value.
    /// </summary>
    public string? SubscriptionKey { get; set; }

    /// <summary>
    /// Leave empty/null to keep the existing encrypted value.
    /// </summary>
    public string? ClientKey { get; set; }

    public bool IsActive { get; set; } = true;
}
