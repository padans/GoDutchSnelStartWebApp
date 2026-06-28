namespace GoDutchSnelStartWebApp.Portal.Models.MyPos;

public sealed class MyPosTransactionTotalViewModel
{
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

    public string BtwBerekening { get; set; } = "Geen";
    public string? BtwSoort { get; set; }
    public decimal? BtwPercentage { get; set; }

    public decimal BrutoControleBedrag { get; set; }
    public decimal NettoControleBedrag { get; set; }
    public decimal BtwControleBedrag { get; set; }

    public bool HasMapping { get; set; }
    public bool HasActiveMapping { get; set; }
    public bool IsReadyForExport { get; set; }
    public string? MappingWarning { get; set; }

    public string BtwDisplayName
    {
        get
        {
            if (string.Equals(BtwBerekening, "Geen", StringComparison.OrdinalIgnoreCase))
            {
                return "Geen";
            }

            if (!string.IsNullOrWhiteSpace(BtwSoort) && BtwPercentage.HasValue)
            {
                return $"{BtwBerekening} - {BtwSoort} ({BtwPercentage.Value:0.##}%)";
            }

            return BtwBerekening;
        }
    }
}