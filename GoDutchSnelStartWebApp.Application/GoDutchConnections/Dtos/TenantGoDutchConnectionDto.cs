namespace GoDutchSnelStartWebApp.Application.GoDutchConnections.Dtos;

public sealed class TenantGoDutchConnectionDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public string ApiBaseUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;

    public bool HasPassword { get; set; }
    public bool IsActive { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
}