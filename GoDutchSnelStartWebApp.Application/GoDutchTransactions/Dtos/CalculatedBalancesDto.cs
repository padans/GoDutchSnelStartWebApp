namespace GoDutchSnelStartWebApp.Application.GoDutchTransactions.Dtos;

public sealed class CalculatedBalancesDto
{
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public decimal TotalAmount { get; set; }

    public bool HasTransactions { get; set; }

    /// <summary>
    /// Geeft aan of er transacties binnen de gevraagde exportperiode aanwezig waren.
    /// </summary>
    public bool HasPeriodTransactions { get; set; }
}