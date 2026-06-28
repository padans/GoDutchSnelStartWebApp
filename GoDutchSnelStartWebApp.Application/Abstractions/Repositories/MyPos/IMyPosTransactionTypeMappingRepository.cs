using GoDutchSnelStartWebApp.Domain.Entities.MyPos;

namespace GoDutchSnelStartWebApp.Application.Abstractions.Repositories.MyPos;

public interface IMyPosTransactionTypeMappingRepository
{
    Task<MyPosTransactionTypeMapping?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MyPosTransactionTypeMapping?> GetByCodeAsync(Guid tenantId, string transactionCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MyPosTransactionTypeMapping>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task UpsertAsync(MyPosTransactionTypeMapping mapping, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, DateTime modifiedUtc, CancellationToken cancellationToken = default);
}
