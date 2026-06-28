using GoDutchSnelStartWebApp.Application.BankAccounts.Dtos;

namespace GoDutchSnelStartWebApp.Application.BankAccounts.Interfaces;

public interface IBankAccountResyncService
{
    Task<BankAccountResyncResultDto> ForceResyncFromDateAsync(
        Guid tenantId,
        Guid bankAccountId,
        DateTime fromUtc,
        CancellationToken cancellationToken = default);
}
