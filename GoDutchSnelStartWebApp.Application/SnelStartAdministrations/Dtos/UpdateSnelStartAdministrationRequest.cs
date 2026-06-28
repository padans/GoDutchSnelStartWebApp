namespace GoDutchSnelStartWebApp.Application.SnelStartAdministrations.Dtos;

public sealed class UpdateSnelStartAdministrationRequest
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? AdministrationClientKey { get; set; }

    public bool IsActive { get; set; }
}
