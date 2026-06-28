using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Dtos;

namespace GoDutchSnelStartWebApp.Application.GoDutchTransactions.Interfaces;

public interface IGoDutchSnelStartAutoImportService
{
    Task<SnelStartImportResult> ImportAsync(
        Guid tenantId,
        Guid bankAccountId,
        string iban,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
}