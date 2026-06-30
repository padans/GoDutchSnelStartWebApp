using GoDutchSnelStartWebApp.Application.MyPos.Dtos;
using GoDutchSnelStartWebApp.Domain.Entities.MyPos;


namespace GoDutchSnelStartWebApp.Application.Abstractions.Repositories.MyPos;

public interface IMyPosRawTransactionRepository
{
    Task<MyPosRawTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MyPosRawTransaction?> GetByMyPosTransactionIdAsync(Guid tenantId, long myPosTransactionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MyPosRawTransaction>> GetByTenantAsync(Guid tenantId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MyPosRawTransaction>> GetUnexportedAsync(Guid tenantId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    Task<MyPosRawTransactionUpsertResultDto> UpsertRangeAsync( IEnumerable<MyPosRawTransaction> transactions,CancellationToken cancellationToken = default);
    Task MarkExportedAsync(Guid exportBatchId, IEnumerable<long> myPosTransactionIds, CancellationToken cancellationToken = default);
    Task<int> MarkExportedForBatchAsync(
     Guid tenantId,
     Guid exportBatchId,
     DateTime fromUtc,
     DateTime toUtc,
     DateTime importedUtc,
     CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetDistinctTransactionTypesAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
