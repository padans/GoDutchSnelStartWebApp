using GoDutchSnelStartWebApp.Domain.Entities;

namespace GoDutchSnelStartWebApp.Application.Abstractions.Repositories;

public interface ISnelStartAdministrationRepository
{
    Task<SnelStartAdministration?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SnelStartAdministration>> GetByTenantIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task CreateAsync(
        SnelStartAdministration administration,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        SnelStartAdministration administration,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}