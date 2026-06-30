namespace GoDutchSnelStartWebApp.Portal.Models;

public sealed class OnboardTenantRequestViewModel
{
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? KvkNumber { get; set; }

    public bool GoDutchEnabled { get; set; }
    public bool MyPosEnabled { get; set; }

    public bool IsTrial { get; set; } = true;
    public int TrialDurationDays { get; set; } = 7;
}
