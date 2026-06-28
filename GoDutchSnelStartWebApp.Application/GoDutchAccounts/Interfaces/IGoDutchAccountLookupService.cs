using GoDutchSnelStartWebApp.Application.GoDutchAccounts.Dtos;

namespace GoDutchSnelStartWebApp.Application.GoDutchAccounts.Interfaces;

public interface IGoDutchAccountLookupService
{
    Task<IReadOnlyList<GoDutchAccountLookupDto>> GetAccountsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
