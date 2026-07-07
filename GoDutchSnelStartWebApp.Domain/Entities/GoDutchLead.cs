namespace GoDutchSnelStartWebApp.Domain.Entities;

public sealed class GoDutchLead
{
    public Guid Id { get; set; }
    public string BedrijfsNaam { get; set; } = string.Empty;
    public string ContactPersoon { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefoon { get; set; }
    public int? AantalBankrekeningen { get; set; }
    public string Status { get; set; } = "Nieuw";
    public DateTime CreatedUtc { get; set; }
}
