using GoDutchSnelStartWebApp.Application.MyPos.Dtos;

namespace GoDutchSnelStartWebApp.Application.MyPos.Interfaces;

public interface IMyPosTransactionTotalService
{
    Task<IReadOnlyList<MyPosTransactionTotalDto>> GetTotalsAsync(
        Guid tenantId,
        DateTime fromUtc,
        DateTime toUtc,
        bool includeExported = false,
        CancellationToken cancellationToken = default);
}
