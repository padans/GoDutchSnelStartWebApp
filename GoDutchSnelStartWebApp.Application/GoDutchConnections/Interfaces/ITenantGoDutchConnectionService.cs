using GoDutchSnelStartWebApp.Application.GoDutchConnections.Dtos;

namespace GoDutchSnelStartWebApp.Application.GoDutchConnections.Interfaces;

public interface ITenantGoDutchConnectionService
{
    Task<TenantGoDutchConnectionDto?> GetByTenantIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(
        Guid tenantId,
        CreateTenantGoDutchConnectionRequest request,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Guid tenantId,
        Guid id,
        UpdateTenantGoDutchConnectionRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid tenantId,
        Guid id,
        CancellationToken cancellationToken = default);
}