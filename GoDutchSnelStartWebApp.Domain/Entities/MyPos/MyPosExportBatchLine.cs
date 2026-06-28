using GoDutchSnelStartWebApp.Domain.ValueObjects;

namespace GoDutchSnelStartWebApp.Domain.Entities.MyPos;

public sealed class MyPosExportBatchLine
{
    public Guid Id { get; init; }
    public Guid BatchId { get; set; }
    public Guid TenantId { get; set; }

    public string TransactionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "EUR";

    public DateTime FirstTransactionUtc { get; set; }
    public DateTime LastTransactionUtc { get; set; }

    public SnelStartGrootboekRef? SnelStartGrootboek { get; set; }

    public bool HasMapping { get; set; }
    public bool HasActiveMapping { get; set; }
    public bool IsReadyForExport { get; set; }
    public string? MappingWarning { get; set; }

    public DateTime CreatedUtc { get; init; }

    public string BtwBerekening { get; set; } = "Geen";

    public string? BtwSoort { get; set; }

    public decimal? BtwPercentage { get; set; }
}
