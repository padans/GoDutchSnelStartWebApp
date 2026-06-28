namespace GoDutchSnelStartWebApp.Portal.Models.MyPos;

public sealed class MyPosTransactionTypeMappingViewModel
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

    public Guid? SelectedGrootboekId
    {
        get => SnelStartGrootboekId;
        set => SnelStartGrootboekId = value;
    }

    public string GrootboekDisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(SnelStartGrootboekNummer) &&
                !string.IsNullOrWhiteSpace(SnelStartGrootboekNaam))
            {
                return $"{SnelStartGrootboekNummer} - {SnelStartGrootboekNaam}";
            }

            if (!string.IsNullOrWhiteSpace(SnelStartGrootboekNummer))
            {
                return SnelStartGrootboekNummer;
            }

            if (!string.IsNullOrWhiteSpace(SnelStartGrootboekNaam))
            {
                return SnelStartGrootboekNaam;
            }

            return "-";
        }
    }

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