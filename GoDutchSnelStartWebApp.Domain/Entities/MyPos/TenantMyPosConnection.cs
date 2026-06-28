using GoDutchSnelStartWebApp.Domain.ValueObjects;

namespace GoDutchSnelStartWebApp.Domain.Entities.MyPos;

public sealed class TenantMyPosConnection
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }

    public string AuthUrl { get; set; } = string.Empty;
    public string TransactionsApiBaseUrl { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;
    public string? ClientSecretEncrypted { get; set; }
    public string? ApiKeyEncrypted { get; set; }

    public SnelStartDagboekRef? SnelStartBankDagboek { get; set; }
    public string? SnelStartBankIban { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedUtc { get; init; }
    public DateTime? ModifiedUtc { get; set; }
}