namespace GoDutchSnelStartWebApp.Application.MyPos.Dtos;

public sealed class TenantMyPosConnectionDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public string AuthUrl { get; set; } = string.Empty;
    public string TransactionsApiBaseUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;

    public bool HasClientSecret { get; set; }
    public bool HasApiKey { get; set; }

    public Guid? SnelStartBankDagboekId { get; set; }
    public string? SnelStartBankDagboekNummer { get; set; }
    public string? SnelStartBankDagboekNaam { get; set; }
    public string? SnelStartBankIban { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
}