namespace GoDutchSnelStartWebApp.Portal.Models;

public sealed class CreateSnelStartGrootboekRequestViewModel
{
    public int Nummer { get; set; }
    public string Omschrijving { get; set; } = string.Empty;
    public string RekeningCode { get; set; } = "WinstEnVerlies";
    public string Grootboekfunctie { get; set; } = "Diversen";
    public bool KostenplaatsVerplicht { get; set; }
    public bool Nonactief { get; set; }
    public IReadOnlyList<string> BtwSoort { get; set; } = ["Geen"];
}
