using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GoDutchSnelStartWebApp.Application.ConnectivityTests.Dtos;
using GoDutchSnelStartWebApp.Application.ConnectivityTests.Interfaces;
using Microsoft.Extensions.Logging;

namespace GoDutchSnelStartWebApp.Infrastructure.ExternalServices;

public sealed class SnelStartConnectionTestClient : ISnelStartConnectionTestClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SnelStartConnectionTestClient> _logger;

    public SnelStartConnectionTestClient(
        HttpClient httpClient,
        ILogger<SnelStartConnectionTestClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ConnectionTestResultDto> TestAsync(
        string authUrl,
        string apiBaseUrl,
        string? clientKey,
        string? decryptedSubscriptionKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(authUrl))
            {
                return new ConnectionTestResultDto
                {
                    Success = false,
                    Provider = "SnelStart",
                    Message = "SnelStart auth URL is missing."
                };
            }

            if (string.IsNullOrWhiteSpace(apiBaseUrl))
            {
                return new ConnectionTestResultDto
                {
                    Success = false,
                    Provider = "SnelStart",
                    Message = "SnelStart API base URL is missing."
                };
            }

            if (string.IsNullOrWhiteSpace(clientKey))
            {
                return new ConnectionTestResultDto
                {
                    Success = false,
                    Provider = "SnelStart",
                    Message = "SnelStart client key is missing."
                };
            }

            if (string.IsNullOrWhiteSpace(decryptedSubscriptionKey))
            {
                return new ConnectionTestResultDto
                {
                    Success = false,
                    Provider = "SnelStart",
                    Message = "SnelStart subscription key is missing."
                };
            }

            var trimmedClientKey = clientKey.Trim();

            using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, authUrl)
            {
                Content = new StringContent(
                    $"grant_type=clientkey&clientkey={Uri.EscapeDataString(trimmedClientKey)}",
                    Encoding.UTF8,
                    "application/x-www-form-urlencoded")
            };

            _logger.LogWarning(
                "SnelStart token debug. Class: {Class}, AuthUrl: {AuthUrl}, ClientKeyLength: {ClientKeyLength}, ClientKeyStart: {ClientKeyStart}, ClientKeyEnd: {ClientKeyEnd}, ContentType: {ContentType}.",
                nameof(SnelStartConnectionTestClient),
                authUrl,
                trimmedClientKey.Length,
                SafeStart(trimmedClientKey),
                SafeEnd(trimmedClientKey),
                tokenRequest.Content?.Headers.ContentType?.ToString());

            using var tokenResponse = await _httpClient.SendAsync(tokenRequest, cancellationToken);
            var tokenBody = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);

            var hasAccessToken = false;
            var accessTokenLength = 0;

            try
            {
                using var debugTokenDoc = JsonDocument.Parse(tokenBody);

                if (debugTokenDoc.RootElement.TryGetProperty("access_token", out var debugAccessTokenElement))
                {
                    var debugAccessToken = debugAccessTokenElement.GetString();
                    hasAccessToken = !string.IsNullOrWhiteSpace(debugAccessToken);
                    accessTokenLength = debugAccessToken?.Length ?? 0;
                }
            }
            catch (JsonException)
            {
                // Niet elke foutresponse is JSON. Alleen metadata loggen, nooit token-body.
            }

            _logger.LogInformation(
                "SnelStart token response ontvangen. Class: {Class}, StatusCode: {StatusCode}, BodyLength: {BodyLength}, HasAccessToken: {HasAccessToken}, AccessTokenLength: {AccessTokenLength}.",
                nameof(SnelStartConnectionTestClient),
                (int)tokenResponse.StatusCode,
                tokenBody.Length,
                hasAccessToken,
                accessTokenLength);

            if (!tokenResponse.IsSuccessStatusCode)
            {
                return new ConnectionTestResultDto
                {
                    Success = false,
                    Provider = "SnelStart",
                    Message = $"SnelStart token request failed with HTTP {(int)tokenResponse.StatusCode}. Response: {Truncate(tokenBody)}",
                    TestedUrl = authUrl,
                    StatusCode = (int)tokenResponse.StatusCode
                };
            }

            using var tokenDoc = JsonDocument.Parse(tokenBody);

            if (!tokenDoc.RootElement.TryGetProperty("access_token", out var accessTokenElement))
            {
                return new ConnectionTestResultDto
                {
                    Success = false,
                    Provider = "SnelStart",
                    Message = "SnelStart token response does not contain access_token.",
                    TestedUrl = authUrl
                };
            }

            var accessToken = accessTokenElement.GetString();

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return new ConnectionTestResultDto
                {
                    Success = false,
                    Provider = "SnelStart",
                    Message = "SnelStart returned an empty access token.",
                    TestedUrl = authUrl
                };
            }

            var normalizedApiBaseUrl = apiBaseUrl.Trim().TrimEnd('/');

            var probe1 = $"{normalizedApiBaseUrl}/administraties?$top=1";
            var probe2 = $"{normalizedApiBaseUrl}/artikelen?$top=1";

            _logger.LogInformation(
                "SnelStart token opgehaald. API probe wordt gestart. Probe1: {Probe1}, Probe2: {Probe2}",
                probe1,
                probe2);

            var firstAttempt = await TryProbeAsync(
                probe1,
                accessToken,
                decryptedSubscriptionKey,
                cancellationToken);

            if (firstAttempt.Success)
            {
                return firstAttempt;
            }

            _logger.LogWarning(
                "Eerste SnelStart API probe mislukt. TestedUrl: {TestedUrl}, StatusCode: {StatusCode}, Message: {Message}. Tweede probe wordt geprobeerd.",
                firstAttempt.TestedUrl,
                firstAttempt.StatusCode,
                firstAttempt.Message);

            var secondAttempt = await TryProbeAsync(
                probe2,
                accessToken,
                decryptedSubscriptionKey,
                cancellationToken);

            return secondAttempt;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "SnelStart verbindingstest is onverwacht mislukt. AuthUrl: {AuthUrl}, ApiBaseUrl: {ApiBaseUrl}",
                authUrl,
                apiBaseUrl);

            return new ConnectionTestResultDto
            {
                Success = false,
                Provider = "SnelStart",
                Message = $"SnelStart test failed: {ex.Message}",
                TestedUrl = authUrl
            };
        }
    }

    private async Task<ConnectionTestResultDto> TryProbeAsync(
        string probeUrl,
        string accessToken,
        string subscriptionKey,
        CancellationToken cancellationToken)
    {
        using var probeRequest = new HttpRequestMessage(HttpMethod.Get, probeUrl);
        probeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        probeRequest.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

        using var probeResponse = await _httpClient.SendAsync(probeRequest, cancellationToken);
        var probeBody = await probeResponse.Content.ReadAsStringAsync(cancellationToken);

        _logger.LogWarning(
            "SnelStart API probe debug. Class: {Class}, Url: {Url}, StatusCode: {StatusCode}, Body: {Body}",
            nameof(SnelStartConnectionTestClient),
            probeUrl,
            (int)probeResponse.StatusCode,
            Truncate(probeBody));

        return new ConnectionTestResultDto
        {
            Success = probeResponse.IsSuccessStatusCode,
            Provider = "SnelStart",
            Message = probeResponse.IsSuccessStatusCode
                ? "SnelStart token retrieval and API probe succeeded."
                : $"SnelStart API probe failed with HTTP {(int)probeResponse.StatusCode}.",
            TestedUrl = probeUrl,
            StatusCode = (int)probeResponse.StatusCode
        };
    }

    private static string SafeStart(string? value)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return "-";
        }

        return trimmed.Length <= 6
            ? trimmed
            : trimmed[..6];
    }

    private static string SafeEnd(string? value)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return "-";
        }

        return trimmed.Length <= 6
            ? trimmed
            : trimmed[^6..];
    }

    private static string Truncate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Length <= 500
            ? value
            : value[..500] + "...";
    }
}