namespace GoDutchSnelStartWebApp.Application.GoDutchTransactions.Dtos;

public sealed class BankTransactionDto
{
    public string Id { get; set; } = string.Empty;
    public DateTime? BookingDate { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal? BalanceAfter { get; set; }
    public string? Currency { get; set; }
    public string? Status { get; set; }

    /// <summary>
    /// True als de transactie binnen de door de caller gevraagde periode valt.
    /// </summary>
    public bool IsInRequestedPeriod { get; set; }

    /// <summary>
    /// True als deze transactie alleen is meegenomen als saldo-context
    /// van vóór de gevraagde periode.
    /// </summary>
    public bool IsBalanceAnchor { get; set; }
}