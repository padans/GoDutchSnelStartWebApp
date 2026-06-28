namespace GoDutchSnelStartWebApp.Portal.Models;

public sealed class SnelStartBtwTariefLookupViewModel
{
    public string BtwSoort { get; set; } = string.Empty;

    public decimal BtwPercentage { get; set; }

    public DateTime? DatumVanaf { get; set; }

    public DateTime? DatumTotEnMet { get; set; }

    public string DisplayName
    {
        get
        {
            var percentageText = BtwPercentage.ToString("0.##");
            return $"{BtwSoort} ({percentageText}%)";
        }
    }
}