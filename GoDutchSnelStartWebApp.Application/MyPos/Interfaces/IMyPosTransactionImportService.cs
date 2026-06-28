using GoDutchSnelStartWebApp.Application.MyPos.Dtos;

namespace GoDutchSnelStartWebApp.Application.MyPos.Interfaces;

public interface IMyPosTransactionImportService
{
    Task<MyPosTransactionImportResultDto> FetchAndStoreAsync(Guid tenantId, Guid tenantMyPosConnectionId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MyPosRawTransactionDto>> GetRawTransactionsAsync(Guid tenantId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
}
