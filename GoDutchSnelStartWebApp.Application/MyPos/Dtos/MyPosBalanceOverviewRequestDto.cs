namespace GoDutchSnelStartWebApp.Application.MyPos.Dtos;

/// <summary>
/// Vraagt een berekend saldo-overzicht op, uitgaande van een door de gebruiker opgegeven
/// referentiesaldo op een referentietijdstip. Er bestaat geen myPOS-endpoint dat een saldo
/// teruggeeft; het saldo per (sub-)periode wordt terugberekend uit de transacties.
/// </summary>
public sealed class MyPosBalanceOverviewRequestDto
{
    public Guid TenantMyPosConnectionId { get; set; }

    public decimal ReferenceBalance { get; set; }

    public DateTime ReferenceUtc { get; set; }

    public DateTime FromUtc { get; set; }

    public DateTime ToUtc { get; set; }

    /// <summary>
    /// "Month", "Quarter", "Year" of "None" (één blok voor de hele periode).
    /// </summary>
    public string Granularity { get; set; } = "Month";
}
