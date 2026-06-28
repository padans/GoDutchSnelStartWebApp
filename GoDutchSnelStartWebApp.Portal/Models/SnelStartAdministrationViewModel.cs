namespace GoDutchSnelStartWebApp.Portal.Models;

public sealed class SnelStartAdministrationViewModel
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    // Afhankelijk van de backend-DTO kan de naam als Name, AdministrationName of DisplayName terugkomen.
    public string? Name { get; set; }
    public string? AdministrationName { get; set; }
    public string? DisplayName { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
}
