using GoDutchSnelStartWebApp.Domain.Entities;

namespace GoDutchSnelStartWebApp.Application.Abstractions.Repositories;

public interface IBankAccountSnelStartLinkRepository
{
    Task<BankAccountSnelStartLink?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<BankAccountSnelStartLink?> GetByBankAccountIdAsync(
        Guid bankAccountId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BankAccountSnelStartLink>> GetActiveAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BankAccountSnelStartLink>> GetDueForAutoSyncAsync(
        DateTime dueUtc,
        CancellationToken cancellationToken = default);

    Task CreateAsync(
        BankAccountSnelStartLink link,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        BankAccountSnelStartLink link,
        CancellationToken cancellationToken = default);

    Task UpdateAutoSyncScheduleAsync(
        Guid id,
        DateTime? lastRunUtc,
        DateTime? nextRunUtc,
        DateTime modifiedUtc,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
