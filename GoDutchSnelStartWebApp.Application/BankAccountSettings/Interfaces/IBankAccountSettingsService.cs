using GoDutchSnelStartWebApp.Application.BankAccountSettings.Dtos;
using GoDutchSnelStartWebApp.Application.ConnectivityTests.Dtos;

namespace GoDutchSnelStartWebApp.Application.BankAccountSettings.Interfaces;

public interface IBankAccountSettingsService
{
    Task<BankAccountSettingsDto?> GetByBankAccountIdAsync(
        Guid tenantId,
        Guid bankAccountId,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(
        Guid tenantId,
        Guid bankAccountId,
        CreateBankAccountSettingsRequest request,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Guid tenantId,
        Guid bankAccountId,
        Guid id,
        UpdateBankAccountSettingsRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid tenantId,
        Guid bankAccountId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ConnectionTestResultDto> TestSnelStartAsync(
        Guid tenantId,
        Guid bankAccountId,
        CancellationToken cancellationToken = default);
}