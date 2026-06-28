namespace GoDutchSnelStartWebApp.Application.SnelStartConnections.Dtos;

public sealed class TenantSnelStartConnectionDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string ConnectionType { get; set; } = "CustomKey";
    public string AuthUrl { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = string.Empty;
    public bool HasSubscriptionKey { get; set; }
    public bool HasClientKey { get; set; }
    public bool HasOAuthAccessToken { get; set; }
    public bool HasOAuthRefreshToken { get; set; }
    public DateTime? OAuthExpiresUtc { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
}
