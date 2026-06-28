using GoDutchSnelStartWebApp.Domain.Enums;

namespace GoDutchSnelStartWebApp.Domain.Entities.SnelStart;

public sealed class TenantSnelStartConnection
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }

    public SnelStartConnectionType ConnectionType { get; set; } = SnelStartConnectionType.CustomKey;

    public string AuthUrl { get; set; } = "https://auth.snelstart.nl/b2b/token";
    public string ApiBaseUrl { get; set; } = "https://b2bapi.snelstart.nl/v2";

    public string? SubscriptionKeyEncrypted { get; set; }
    public string? ClientKeyEncrypted { get; set; }

    public string? OAuthAccessTokenEncrypted { get; set; }
    public string? OAuthRefreshTokenEncrypted { get; set; }
    public DateTime? OAuthExpiresUtc { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedUtc { get; init; }
    public DateTime? ModifiedUtc { get; set; }
}
