using GoDutchSnelStartWebApp.Domain.ValueObjects;

namespace GoDutchSnelStartWebApp.Domain.Entities.MyPos;

public sealed class MyPosTransactionTypeMapping
{
    public Guid Id { get; init; }

    public Guid TenantId { get; init; }

    public string TransactionCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public SnelStartGrootboekRef? SnelStartGrootboek { get; set; }

    public string BtwBerekening { get; set; } = "Geen";
    public string? BtwSoort { get; set; }
    public decimal? BtwPercentage { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedUtc { get; init; }
    public DateTime? ModifiedUtc { get; set; }
}