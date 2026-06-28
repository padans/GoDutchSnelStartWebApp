using GoDutchSnelStartWebApp.Domain.Entities;

namespace GoDutchSnelStartWebApp.Application.Abstractions.Repositories;

public interface IGoDutchImportRunRepository
{
    Task<GoDutchImportRun?> GetLastSuccessfulByBankAccountIdAsync(
        Guid bankAccountId,
        CancellationToken cancellationToken = default);

    Task<GoDutchImportRun?> GetLastCompletedByBankAccountIdAsync(
        Guid bankAccountId,
        CancellationToken cancellationToken = default);

    Task CreateAsync(
        GoDutchImportRun importRun,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        GoDutchImportRun importRun,
        CancellationToken cancellationToken = default);
    Task<GoDutchImportRun?> GetLastCompletedAsync(
    CancellationToken cancellationToken = default);
}