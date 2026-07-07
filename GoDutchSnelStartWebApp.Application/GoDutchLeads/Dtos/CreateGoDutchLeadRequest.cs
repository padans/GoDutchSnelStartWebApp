namespace GoDutchSnelStartWebApp.Application.GoDutchLeads.Dtos;

public sealed class CreateGoDutchLeadRequest
{
    public string BedrijfsNaam { get; set; } = string.Empty;
    public string ContactPersoon { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefoon { get; set; }
    public int? AantalBankrekeningen { get; set; }
}
