namespace GoDutchSnelStartWebApp.Application.MyPos.Dtos;

public sealed class MyPosTransactionTotalDto
{
    public Guid TenantId { get; set; }

    public string TransactionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = string.Empty;

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
    public decimal GrossAmount { get; set; }

    public decimal NetAmount { get; set; }

    public decimal VatAmount { get; set; }
}