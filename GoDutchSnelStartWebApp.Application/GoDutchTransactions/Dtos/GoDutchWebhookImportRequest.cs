namespace GoDutchSnelStartWebApp.Application.GoDutchTransactions.Dtos;

public sealed class GoDutchWebhookImportRequest
{
    public Guid TenantId { get; set; }
    public Guid BankAccountId { get; set; }

    public string Iban { get; set; } = string.Empty;

    public DateTime From { get; set; }
    public DateTime To { get; set; }

    public string? TriggerSource { get; set; }
    public string? RawPayload { get; set; }
}