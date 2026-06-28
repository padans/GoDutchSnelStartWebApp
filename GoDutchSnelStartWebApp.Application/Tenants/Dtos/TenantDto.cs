namespace GoDutchSnelStartWebApp.Application.Tenants.Dtos;

public sealed class TenantDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? CustomerCode { get; set; }

    public string? CompanyName { get; set; }
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    public string? DefaultIban { get; set; }

    public string Status { get; set; } = "Draft";
    public bool IsActive { get; set; }

    public DateTime? TrialStartsUtc { get; set; }
    public DateTime? TrialEndsUtc { get; set; }
    public DateTime? OnboardingCompletedUtc { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
}