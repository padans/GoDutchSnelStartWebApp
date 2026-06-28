namespace GoDutchSnelStartWebApp.Application.GoDutchTransactions.Dtos;

public sealed class GoDutchTransactionDto
{
    public string Id { get; set; } = string.Empty;

    public DateTime? BookingDate { get; set; }
    public DateTime? ValueDate { get; set; }

    public decimal Amount { get; set; }

    /// <summary>
    /// Saldo na verwerking van deze transactie (vanuit GoDutch API)
    /// </summary>
    public decimal? BalanceAfter { get; set; }

    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Status van de transactie (bijv. booked / pending)
    /// </summary>
    public string Status { get; set; } = string.Empty;
}