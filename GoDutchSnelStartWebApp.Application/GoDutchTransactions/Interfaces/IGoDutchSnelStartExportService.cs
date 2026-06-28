using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Dtos;

namespace GoDutchSnelStartWebApp.Application.GoDutchTransactions.Interfaces;

public interface IGoDutchSnelStartExportService
{
    Task<string> GenerateForSnelStartAsync(
        Guid tenantId,
        Guid bankAccountId,
        string iban,
        DateTime from,
        DateTime to,
        SnelStartExportFormat format,
        CancellationToken cancellationToken = default);
}