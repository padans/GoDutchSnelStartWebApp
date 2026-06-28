namespace GoDutchSnelStartWebApp.Application.GoDutchTransactions.Interfaces;

public interface IGoDutchCamt053ExportService
{
    Task<string> GenerateCamt053Async(
        Guid tenantId,
        Guid bankAccountId,
        string iban,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
}