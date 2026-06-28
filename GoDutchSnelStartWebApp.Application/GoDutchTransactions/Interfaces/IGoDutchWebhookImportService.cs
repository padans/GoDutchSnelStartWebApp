using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Dtos;

namespace GoDutchSnelStartWebApp.Application.GoDutchTransactions.Interfaces;

public interface IGoDutchWebhookImportService
{
    Task<SnelStartImportResult> ImportAsync(
        GoDutchWebhookImportRequest request,
        CancellationToken cancellationToken = default);
}