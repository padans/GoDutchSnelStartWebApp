namespace GoDutchSnelStartWebApp.Application.GoDutchTransactions.Dtos;

public sealed class SnelStartImportRequest
{
    public Guid TenantId { get; set; }
    public Guid BankAccountId { get; set; }
    public string Iban { get; set; } = string.Empty;
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public SnelStartExportFormat Format { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}