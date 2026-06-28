namespace GoDutchSnelStartWebApp.Application.GoDutchTransactions.Dtos;

public sealed class Mt940Request
{
    public string Iban { get; set; } = string.Empty;
    public string Currency { get; set; } = "EUR";

    public string StatementReference { get; set; } = string.Empty;
    public string StatementNumber { get; set; } = "1";

    public DateTime StatementDate { get; set; }
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }

    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }

    public IReadOnlyList<BankTransactionDto> Transactions { get; set; } = new List<BankTransactionDto>();
}