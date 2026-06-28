namespace GoDutchSnelStartWebApp.Domain.Entities;

public sealed class TenantGoDutchConnection
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }

    public string ApiBaseUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;

    public string? PasswordEncrypted { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; init; }
    public DateTime? ModifiedUtc { get; set; }
}