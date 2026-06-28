namespace GoDutchSnelStartWebApp.Application.GoDutchTransactions.Dtos;

public sealed class SnelStartImportResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }

    public int TransactionCount { get; set; }
    public string? Iban { get; set; }
    public string? AdministrationName { get; set; }

    public bool DownloadSucceeded { get; set; }
    public bool UploadSucceeded { get; set; }
    public bool IsDuplicateImport { get; set; }

    public string? ExternalReference { get; set; }
    public string? RawResponse { get; set; }
    public int RetryCount { get; set; }
}