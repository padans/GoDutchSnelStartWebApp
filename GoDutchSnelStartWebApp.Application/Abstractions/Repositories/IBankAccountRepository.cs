using GoDutchSnelStartWebApp.Domain.Entities;

namespace GoDutchSnelStartWebApp.Application.Abstractions.Repositories;

public interface IBankAccountRepository
{
    Task<IReadOnlyList<BankAccount>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<BankAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task CreateAsync(BankAccount bankAccount, CancellationToken cancellationToken = default);
    Task UpdateAsync(BankAccount bankAccount, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}