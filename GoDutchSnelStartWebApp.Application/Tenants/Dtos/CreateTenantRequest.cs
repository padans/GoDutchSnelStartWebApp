
namespace GoDutchSnelStartWebApp.Application.Tenants.Dtos;

public sealed class CreateTenantRequest
{
    public string Name { get; set; } = string.Empty;
    public string? CustomerCode { get; set; }

    public string? CompanyName { get; set; }
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    /// <summary>
    /// Legacy veld - voorlopig nog toegestaan voor backward compatibility.
    /// Nieuwe flow gebruikt dit veld bij voorkeur niet meer.
    /// </summary>
    public string? DefaultIban { get; set; }

    public string? Status { get; set; }
    public bool? IsActive { get; set; }

    public DateTime? TrialStartsUtc { get; set; }
    public DateTime? TrialEndsUtc { get; set; }
    public DateTime? OnboardingCompletedUtc { get; set; }
}