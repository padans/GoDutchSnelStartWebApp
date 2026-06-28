using GoDutchSnelStartWebApp.Application.MyPos.Dtos;

namespace GoDutchSnelStartWebApp.Application.MyPos.Interfaces;

public interface IMyPosExportBatchService
{
    Task<MyPosExportBatchDto> CreateConceptBatchAsync(
        Guid tenantId,
        CreateMyPosExportBatchRequest request,
        CancellationToken cancellationToken = default);

    Task<MyPosExportBatchDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MyPosExportBatchDto>> GetByTenantAsync(
        Guid tenantId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);
}
