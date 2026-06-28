using GoDutchSnelStartWebApp.Domain.Entities;

namespace GoDutchSnelStartWebApp.Application.Abstractions.Repositories;

public interface ITenantGoDutchConnectionRepository
{
    Task<TenantGoDutchConnection?> GetByTenantIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<TenantGoDutchConnection?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task CreateAsync(
        TenantGoDutchConnection connection,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        TenantGoDutchConnection connection,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        DateTime modifiedUtc,
        CancellationToken cancellationToken = default);
}