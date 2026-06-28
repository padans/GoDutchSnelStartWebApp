using GoDutchSnelStartWebApp.Domain.ValueObjects;

namespace GoDutchSnelStartWebApp.Domain.Entities;

public sealed class BankAccount
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public string Iban { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedUtc { get; init; }
    public DateTime? ModifiedUtc { get; set; }
    public SnelStartGrootboekRef? SnelStartGrootboek { get; set; }
    public SnelStartDagboekRef? SnelStartDagboek { get; set; }
}
