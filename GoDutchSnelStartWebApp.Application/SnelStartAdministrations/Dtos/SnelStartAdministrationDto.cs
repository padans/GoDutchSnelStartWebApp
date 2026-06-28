namespace GoDutchSnelStartWebApp.Application.SnelStartAdministrations.Dtos;

public sealed class SnelStartAdministrationDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }
    public bool HasClientKey { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
}