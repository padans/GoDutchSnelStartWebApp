using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Dtos;

namespace GoDutchSnelStartWebApp.Application.GoDutchTransactions.Interfaces;

public interface ISnelStartBankStatementImporter
{
    Task<SnelStartImportResult> ImportAsync(
        SnelStartImportRequest request,
        CancellationToken cancellationToken = default);
}