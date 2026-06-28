using GoDutchSnelStartWebApp.Application.MyPos.Dtos;

namespace GoDutchSnelStartWebApp.Application.MyPos.Interfaces;

public interface IMyPosTransactionTypeMappingService
{
    Task<IReadOnlyList<MyPosTransactionTypeMappingDto>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<MyPosTransactionTypeMappingDto> UpsertAsync(Guid tenantId, UpsertMyPosTransactionTypeMappingRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
}
