namespace GoDutchSnelStartWebApp.Portal.Configuration;

public sealed class PortalTenantOptions
{
    public const string SectionName = "PortalTenant";

    public Guid TenantId { get; set; }
}