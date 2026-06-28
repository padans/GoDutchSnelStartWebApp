namespace GoDutchSnelStartWebApp.Portal.Models.MyPos;

public sealed class MyPosRawTransactionViewModel
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid TenantMyPosConnectionId { get; set; }
    public long MyPosTransactionId { get; set; }
    public string? AccountNumber { get; set; }
    public string? Ruid { get; set; }
    public string? BillingDescriptor { get; set; }
    public string? PanMasked { get; set; }
    public string? Description { get; set; }
    public string? PaymentReference { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string? TransactionCurrency { get; set; }
    public decimal TransactionAmount { get; set; }
    public string? OriginalCurrency { get; set; }
    public decimal? OriginalAmount { get; set; }
    public DateTime TransactionUtc { get; set; }
    public string? Sign { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? TerminalId { get; set; }
    public string? SerialNumber { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public DateTime ImportedUtc { get; set; }
    public bool IsExported { get; set; }
    public Guid? ExportBatchId { get; set; }
}
