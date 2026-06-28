using GoDutchSnelStartWebApp.Domain.Enums;
using GoDutchSnelStartWebApp.Domain.ValueObjects;

namespace GoDutchSnelStartWebApp.Domain.Entities.MyPos;

public sealed class MyPosExportBatch
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid? TenantMyPosConnectionId { get; init; }

    public MyPosExportTarget ExportTarget { get; init; } = MyPosExportTarget.SnelStartBankboek;

    public SnelStartDagboekRef? SnelStartBankDagboek { get; set; }
    public string? SnelStartBankIban { get; set; }

    public int? BookYear { get; set; }
    public DateTime? PeriodFromLocalDate { get; set; }
    public DateTime? PeriodToLocalDate { get; set; }
    public string? BookYearValidationMessage { get; set; }

    public DateTime PeriodFromUtc { get; set; }
    public DateTime PeriodToUtc { get; set; }

    public MyPosExportBatchStatus Status { get; set; } = MyPosExportBatchStatus.Concept;
    public string Currency { get; set; } = "EUR";

    public int RawTransactionCount { get; set; }
    public int LineCount { get; set; }
    public decimal TotalAmount { get; set; }

    public bool IsReadyForExport { get; set; }
    public string? ValidationMessage { get; set; }

    public string? SnelStartReference { get; set; }
    public DateTime? ExportedUtc { get; set; }
    public string? ErrorMessage { get; set; }

    public DateTime CreatedUtc { get; init; }
    public DateTime? ModifiedUtc { get; set; }

    public List<MyPosExportBatchLine> Lines { get; set; } = [];
}