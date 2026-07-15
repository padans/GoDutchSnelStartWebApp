namespace GoDutchSnelStartWebApp.Domain.Entities;

public sealed class SnelStartAdministration
{
    public Guid Id { get; init; }
    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? NameSnelStartAdministration { get; set; }
    public string AdministrationClientKeyEncrypted { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedUtc { get; init; }
    public DateTime? ModifiedUtc { get; set; }
}