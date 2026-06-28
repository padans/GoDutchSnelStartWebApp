namespace GoDutchSnelStartWebApp.Application.BankAccounts.Dtos;

public sealed class BankAccountResyncResultDto
{
    public Guid BankAccountId { get; set; }
    public string Iban { get; set; } = string.Empty;

    public DateTime PeriodFromUtc { get; set; }
    public DateTime PeriodToUtc { get; set; }

    public Guid? ImportRunId { get; set; }
    public string Status { get; set; } = "Unknown";
    public string Message { get; set; } = string.Empty;

    public int GoDutchTransactionCount { get; set; }
    public int PeriodTransactionCount { get; set; }
    public int BalanceAnchorCount { get; set; }
    public int ExportTransactionCount { get; set; }
    public int NotExportedTransactionCount { get; set; }

    public decimal? OpeningBalance { get; set; }
    public DateTime? OpeningBalanceDate { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal? ClosingBalance { get; set; }
    public DateTime? ClosingBalanceDate { get; set; }

    public bool HasBalanceAnchor { get; set; }

    public bool DownloadSucceeded { get; set; }
    public bool SnelStartUploadAttempted { get; set; }
    public bool UploadSucceeded { get; set; }
    public bool IsDuplicateImport { get; set; }

    public int RetryCount { get; set; }
    public string? Details { get; set; }
}
