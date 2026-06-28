using GoDutchSnelStartWebApp.Application.SnelStartAdministrations.Dtos;

namespace GoDutchSnelStartWebApp.Application.SnelStartAdministrations.Interfaces;

public interface IBankAccountSnelStartLinkService
{
    Task<BankAccountSnelStartLinkDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<BankAccountSnelStartLinkDto?> GetByBankAccountIdAsync(
        Guid bankAccountId,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(
        CreateBankAccountSnelStartLinkRequest request,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        UpdateBankAccountSnelStartLinkRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}