
namespace GoDutchSnelStartWebApp.Application.Tenants.Dtos;

public sealed class CreateTenantRequest
{
    public string Name { get; set; } = string.Empty;
    public string? CustomerCode { get; set; }

    public string? CompanyName { get; set; }
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? KvkNumber { get; set; }

    public bool GoDutchEnabled { get; set; }
    public bool MyPosEnabled { get; set; }

    /// <summary>
    /// Legacy veld - voorlopig nog toegestaan voor backward compatibility.
    /// Nieuwe flow gebruikt dit veld bij voorkeur niet meer.
    /// </summary>
    public string? DefaultIban { get; set; }

    public string? Status { get; set; }
    public bool? IsActive { get; set; }

    public bool IsTrial { get; set; }
    public int TrialDurationDays { get; set; } = 7;

    public DateTime? TrialStartsUtc { get; set; }
    public DateTime? TrialEndsUtc { get; set; }
    public DateTime? OnboardingCompletedUtc { get; set; }
}