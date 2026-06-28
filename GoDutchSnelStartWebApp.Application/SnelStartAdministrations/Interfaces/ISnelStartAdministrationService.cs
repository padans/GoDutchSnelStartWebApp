using GoDutchSnelStartWebApp.Application.SnelStartAdministrations.Dtos;

namespace GoDutchSnelStartWebApp.Application.SnelStartAdministrations.Interfaces;

public interface ISnelStartAdministrationService
{
    Task<SnelStartAdministrationDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SnelStartAdministrationDto>> GetByTenantIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(
        CreateSnelStartAdministrationRequest request,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        UpdateSnelStartAdministrationRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}