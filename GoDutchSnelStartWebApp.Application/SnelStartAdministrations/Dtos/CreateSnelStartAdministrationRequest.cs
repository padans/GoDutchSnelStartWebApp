namespace GoDutchSnelStartWebApp.Application.SnelStartAdministrations.Dtos;

public sealed class CreateSnelStartAdministrationRequest
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameSnelStartAdministration { get; set; }
    public string AdministrationClientKey { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}