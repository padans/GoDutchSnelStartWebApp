namespace GoDutchSnelStartWebApp.Portal.Models;

public sealed class SnelStartGrootboekLookupViewModel
{
    public Guid Id { get; set; }
    public string? Nummer { get; set; }
    public string? Omschrijving { get; set; }

    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Nummer) && !string.IsNullOrWhiteSpace(Omschrijving))
            {
                return $"{Nummer} - {Omschrijving}";
            }

            if (!string.IsNullOrWhiteSpace(Nummer))
            {
                return Nummer;
            }

            if (!string.IsNullOrWhiteSpace(Omschrijving))
            {
                return Omschrijving;
            }

            return Id.ToString();
        }
    }
}