namespace GoDutchSnelStartWebApp.Portal.Models;

public sealed class CreateSnelStartAdministrationRequestViewModel
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AdministrationClientKey { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
