using GoDutchSnelStartWebApp.Portal.Models;

namespace GoDutchSnelStartWebApp.Portal.Api.Interfaces;

public interface IGoDutchBackendApiClient
{
    Task<TenantGoDutchConnectionViewModel?> GetTenantGoDutchConnectionAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task SaveTenantGoDutchConnectionAsync(
        Guid tenantId,
        TenantGoDutchConnectionViewModel request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoDutchAccountLookupViewModel>> GetGoDutchAccountsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
