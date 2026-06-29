using GoDutchSnelStartWebApp.Domain.Entities.MyPos;

namespace GoDutchSnelStartWebApp.Application.Abstractions.Repositories.MyPos;

public interface ITenantMyPosConnectionRepository
{
    Task<TenantMyPosConnection?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<TenantMyPosConnection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantMyPosConnection>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task CreateAsync(TenantMyPosConnection connection, CancellationToken cancellationToken = default);
    Task UpdateAsync(TenantMyPosConnection connection, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, DateTime modifiedUtc, CancellationToken cancellationToken = default);
}
