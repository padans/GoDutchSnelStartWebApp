namespace GoDutchSnelStartWebApp.Portal.Models.MyPos;

public sealed class MyPosExportBatchViewModel
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid? TenantMyPosConnectionId { get; set; }

    public string ExportTarget { get; set; } = "SnelStartBankboek";

    public Guid? SnelStartBankDagboekId { get; set; }
    public string? SnelStartBankDagboekNummer { get; set; }
    public string? SnelStartBankDagboekNaam { get; set; }
    public string? SnelStartBankIban { get; set; }

    public int? BookYear { get; set; }
    public DateTime? PeriodFromLocalDate { get; set; }
    public DateTime? PeriodToLocalDate { get; set; }
    public string? BookYearValidationMessage { get; set; }

    public DateTime PeriodFromUtc { get; set; }
    public DateTime PeriodToUtc { get; set; }

    public string Status { get; set; } = "Concept";
    public string Currency { get; set; } = "EUR";

    public int RawTransactionCount { get; set; }
    public int LineCount { get; set; }
    public decimal TotalAmount { get; set; }

    public bool IsReadyForExport { get; set; }
    public string? ValidationMessage { get; set; }

    public string? SnelStartReference { get; set; }
    public DateTime? ExportedUtc { get; set; }
    public string? ErrorMessage { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }

    public List<MyPosExportBatchLineViewModel> Lines { get; set; } = [];

    public string BankboekDisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(SnelStartBankDagboekNummer) &&
                !string.IsNullOrWhiteSpace(SnelStartBankDagboekNaam))
            {
                return $"{SnelStartBankDagboekNummer} - {SnelStartBankDagboekNaam}";
            }

            return "-";
        }
    }
}