namespace GoDutchSnelStartWebApp.Application.MyPos.Dtos;

public sealed class MyPosTransactionTypeMappingDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public string TransactionCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public Guid? SnelStartGrootboekId { get; set; }
    public string? SnelStartGrootboekNummer { get; set; }
    public string? SnelStartGrootboekNaam { get; set; }

    public string BtwBerekening { get; set; } = "Geen";
    public string? BtwSoort { get; set; }
    public decimal? BtwPercentage { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
}