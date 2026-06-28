using GoDutchSnelStartWebApp.Domain.Entities.SnelStart;

namespace GoDutchSnelStartWebApp.Application.Abstractions.Repositories.SnelStart;

public interface ITenantSnelStartConnectionRepository
{
    Task<TenantSnelStartConnection?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<TenantSnelStartConnection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task CreateAsync(TenantSnelStartConnection connection, CancellationToken cancellationToken = default);
    Task UpdateAsync(TenantSnelStartConnection connection, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, DateTime modifiedUtc, CancellationToken cancellationToken = default);
}
