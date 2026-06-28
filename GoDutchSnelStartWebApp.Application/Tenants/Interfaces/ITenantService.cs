using GoDutchSnelStartWebApp.Application.Tenants.Dtos;
using GoDutchSnelStartWebApp.Web.Contracts.Tenants;

namespace GoDutchSnelStartWebApp.Application.Tenants.Interfaces;

public interface ITenantService
{
    Task<IReadOnlyList<TenantDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TenantDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(CreateTenantRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, UpdateTenantRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}