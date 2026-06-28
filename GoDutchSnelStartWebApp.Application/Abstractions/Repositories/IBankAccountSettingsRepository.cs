using GoDutchSnelStartWebApp.Domain.Entities;

namespace GoDutchSnelStartWebApp.Application.Abstractions.Repositories;

public interface IBankAccountSettingsRepository
{
    Task<BankAccountSetting?> GetByBankAccountIdAsync(Guid bankAccountId, CancellationToken cancellationToken = default);
    Task<BankAccountSetting?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task CreateAsync(BankAccountSetting settings, CancellationToken cancellationToken = default);
    Task UpdateAsync(BankAccountSetting settings, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}