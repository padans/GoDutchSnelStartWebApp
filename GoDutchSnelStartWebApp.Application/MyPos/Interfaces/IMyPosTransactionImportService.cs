using GoDutchSnelStartWebApp.Application.ConnectivityTests.Dtos;
using GoDutchSnelStartWebApp.Application.MyPos.Dtos;

namespace GoDutchSnelStartWebApp.Application.MyPos.Interfaces;

public interface IMyPosTransactionImportService
{
    Task<MyPosTransactionImportResultDto> FetchAndStoreAsync(Guid tenantId, Guid tenantMyPosConnectionId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MyPosRawTransactionDto>> GetRawTransactionsAsync(Guid tenantId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Voert alleen de myPOS OAuth-tokenuitwisseling uit om de Klantnummer/Klantgeheim te verifiëren.
    /// Gooit niet; geeft het resultaat (incl. foutmelding) terug.
    /// </summary>
    Task<ConnectionTestResultDto> TestConnectionAsync(Guid tenantId, Guid tenantMyPosConnectionId, CancellationToken cancellationToken = default);
}
