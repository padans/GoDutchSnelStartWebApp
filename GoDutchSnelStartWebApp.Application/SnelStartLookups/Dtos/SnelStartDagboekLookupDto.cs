namespace GoDutchSnelStartWebApp.Application.SnelStartLookups.Dtos;

public sealed class SnelStartDagboekLookupDto
{
    public Guid Id { get; set; }
    public string? Nummer { get; set; }
    public string? Omschrijving { get; set; }
}