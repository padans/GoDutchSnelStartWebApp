namespace GoDutchSnelStartWebApp.Application.SnelStartLookups.Dtos;

public sealed class CreateSnelStartGrootboekRequest
{
    public int Nummer { get; set; }
    public string Omschrijving { get; set; } = string.Empty;

    /// <summary>
    /// SnelStart value. Usually "Balans" or "WinstEnVerlies".
    /// </summary>
    public string RekeningCode { get; set; } = "WinstEnVerlies";

    /// <summary>
    /// SnelStart value. Default "Diversen".
    /// </summary>
    public string Grootboekfunctie { get; set; } = "Diversen";

    public bool KostenplaatsVerplicht { get; set; }
    public bool Nonactief { get; set; }
    public IReadOnlyList<string> BtwSoort { get; set; } = ["Geen"];
}
