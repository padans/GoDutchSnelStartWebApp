using GoDutchSnelStartWebApp.Application.SnelStartConnections.Dtos;

namespace GoDutchSnelStartWebApp.Application.SnelStartConnections.Interfaces;

public interface ITenantSnelStartConnectionService
{
    Task<TenantSnelStartConnectionDto?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(Guid tenantId, CreateTenantSnelStartConnectionRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid tenantId, Guid id, UpdateTenantSnelStartConnectionRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
}
