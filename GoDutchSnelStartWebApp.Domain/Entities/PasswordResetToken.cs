namespace GoDutchSnelStartWebApp.Domain.Entities;

public sealed class PasswordResetToken
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiresUtc { get; init; }
    public DateTime? UsedUtc { get; set; }
    public DateTime CreatedUtc { get; init; }

    public bool IsExpired => DateTime.UtcNow > ExpiresUtc;
    public bool IsUsed => UsedUtc.HasValue;
    public bool IsValid => !IsExpired && !IsUsed;
}
