namespace GoDutchSnelStartWebApp.Portal.Models.MyPos;

public sealed class MyPosExportBatchLineViewModel
{
    public Guid Id { get; set; }
    public Guid BatchId { get; set; }
    public Guid TenantId { get; set; }

    public string TransactionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "EUR";

    public DateTime FirstTransactionUtc { get; set; }
    public DateTime LastTransactionUtc { get; set; }

    public Guid? SnelStartGrootboekId { get; set; }
    public string? SnelStartGrootboekNummer { get; set; }
    public string? SnelStartGrootboekNaam { get; set; }

    public bool HasMapping { get; set; }
    public bool HasActiveMapping { get; set; }
    public bool IsReadyForExport { get; set; }
    public string? MappingWarning { get; set; }

    public string GrootboekDisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(SnelStartGrootboekNummer) &&
                !string.IsNullOrWhiteSpace(SnelStartGrootboekNaam))
            {
                return $"{SnelStartGrootboekNummer} - {SnelStartGrootboekNaam}";
            }

            return "-";
        }
    }
}