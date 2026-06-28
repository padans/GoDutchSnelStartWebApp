using GoDutchSnelStartWebApp.Application.ConnectivityTests.Dtos;

namespace GoDutchSnelStartWebApp.Application.ConnectivityTests.Interfaces;

public interface ISnelStartConnectionTestClient
{
    Task<ConnectionTestResultDto> TestAsync(
        string authUrl,
        string apiBaseUrl,
        string? clientKey,
        string? decryptedSubscriptionKey,
        CancellationToken cancellationToken = default);
}