using GoDutchSnelStartWebApp.Application.BankAccounts.Dtos;

namespace GoDutchSnelStartWebApp.Application.BankAccounts.Interfaces;

public interface IBankAccountService
{
    Task<IReadOnlyList<BankAccountDto>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<BankAccountDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(Guid tenantId, CreateBankAccountRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid tenantId, Guid id, UpdateBankAccountRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);

    Task<BankAccountDto?> GetByIdAsync(Guid tenantId, Guid bankAccountId, CancellationToken cancellationToken = default);

    Task<BankAccountSyncStatusDto> GetSyncStatusAsync(Guid tenantId, Guid bankAccountId, CancellationToken cancellationToken = default);
}