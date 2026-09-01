using GoDutchSnelStartWebApp.Application.SnelStartLookups.Dtos;

namespace GoDutchSnelStartWebApp.Application.SnelStartLookups.Interfaces;

public interface ISnelStartLookupService
{
    Task<IReadOnlyList<SnelStartDagboekLookupDto>> GetDagboekenAsync(
        Guid tenantId,
        Guid bankAccountId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SnelStartGrootboekLookupDto>> GetGrootboekenAsync(
        Guid tenantId,
        Guid bankAccountId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SnelStartGrootboekLookupDto>> GetTenantGrootboekenAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<SnelStartGrootboekLookupDto> CreateTenantGrootboekAsync(
        Guid tenantId,
        CreateSnelStartGrootboekRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SnelStartBtwTariefLookupDto>> GetTenantBtwTarievenAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SnelStartDagboekLookupDto>> GetTenantDagboekenAsync(
    Guid tenantId,
    CancellationToken cancellationToken = default);
}