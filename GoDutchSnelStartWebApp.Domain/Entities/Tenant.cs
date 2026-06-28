using GoDutchSnelStartWebApp.Domain.Enums;

namespace GoDutchSnelStartWebApp.Domain.Entities;

public sealed class Tenant
{
    public Guid Id { get; init; }

    /// <summary>
    /// Interne of functionele naam van de tenant.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Externe klantcode / relatienummer indien van toepassing.
    /// </summary>
    public string? CustomerCode { get; set; }

    /// <summary>
    /// Officiële bedrijfsnaam.
    /// </summary>
    public string? CompanyName { get; set; }

    /// <summary>
    /// Naam van contactpersoon voor onboarding / support.
    /// </summary>
    public string? ContactName { get; set; }

    /// <summary>
    /// Primair e-mailadres.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Telefoonnummer.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Legacy veld - wordt uitgefaseerd. Gebruik BankAccount.Iban.
    /// Nog niet verwijderen zolang bestaande code of mappings dit gebruiken.
    /// </summary>
    public string? DefaultIban { get; set; }

    public TenantStatus Status { get; set; } = TenantStatus.Draft;

    /// <summary>
    /// Functionele activatie van tenant.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Startdatum proefperiode.
    /// </summary>
    public DateTime? TrialStartsUtc { get; set; }

    /// <summary>
    /// Einddatum proefperiode.
    /// </summary>
    public DateTime? TrialEndsUtc { get; set; }

    /// <summary>
    /// Moment waarop onboarding functioneel als afgerond geldt.
    /// </summary>
    public DateTime? OnboardingCompletedUtc { get; set; }

    public DateTime CreatedUtc { get; init; }
    public DateTime? ModifiedUtc { get; set; }
}