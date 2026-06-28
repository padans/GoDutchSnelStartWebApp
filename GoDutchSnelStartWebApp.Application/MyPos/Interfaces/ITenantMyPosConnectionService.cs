using GoDutchSnelStartWebApp.Application.MyPos.Dtos;

namespace GoDutchSnelStartWebApp.Application.MyPos.Interfaces;

public interface ITenantMyPosConnectionService
{
    Task<TenantMyPosConnectionDto?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<TenantMyPosConnectionDto> CreateAsync(Guid tenantId, CreateTenantMyPosConnectionRequest request, CancellationToken cancellationToken = default);
    Task<TenantMyPosConnectionDto> UpdateAsync(Guid tenantId, Guid id, UpdateTenantMyPosConnectionRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
}
