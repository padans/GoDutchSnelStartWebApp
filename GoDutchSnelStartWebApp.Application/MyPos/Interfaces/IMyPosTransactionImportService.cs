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

    /// <summary>
    /// Berekent begin- en eindsaldo per (sub-)periode, uitgaande van een door de gebruiker opgegeven
    /// referentiesaldo op een referentietijdstip. Er is geen myPOS-endpoint dat een saldo teruggeeft
    /// met de huidige koppelcredentials; dit is een terugrekening op basis van de transacties.
    /// </summary>
    Task<MyPosBalanceOverviewResultDto> GetBalanceOverviewAsync(Guid tenantId, MyPosBalanceOverviewRequestDto request, CancellationToken cancellationToken = default);
}
