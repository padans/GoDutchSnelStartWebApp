namespace GoDutchSnelStartWebApp.Domain.Entities.MyPos;

public sealed class MyPosRawTransaction
{
    public Guid Id { get; init; }

    public Guid TenantId { get; init; }
    public Guid TenantMyPosConnectionId { get; init; }

    public long MyPosTransactionId { get; set; }

    public string? AccountNumber { get; set; }
    public string? Ruid { get; set; }
    public int? ReferenceNumberType { get; set; }
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
    public string RawJson { get; set; } = string.Empty;

    public DateTime ImportedUtc { get; init; }
    public bool IsExported { get; set; }
    public Guid? ExportBatchId { get; set; }
}
