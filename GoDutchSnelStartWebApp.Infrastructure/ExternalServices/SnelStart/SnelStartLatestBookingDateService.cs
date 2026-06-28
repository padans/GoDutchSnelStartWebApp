using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Application.Abstractions.Security;
using GoDutchSnelStartWebApp.Application.SnelStart.Interfaces;
using Microsoft.Extensions.Logging;

namespace GoDutchSnelStartWebApp.Infrastructure.ExternalServices.SnelStart;

public sealed class SnelStartLatestBookingDateService : ISnelStartLatestBookingDateService
{
    private readonly HttpClient _httpClient;
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly IBankAccountSettingsRepository _settingsRepository;
    private readonly ISecretEncryptionService _encryptionService;
    private readonly ILogger<SnelStartLatestBookingDateService> _logger;

    public SnelStartLatestBookingDateService(
        HttpClient httpClient,
        IBankAccountRepository bankAccountRepository,
        IBankAccountSettingsRepository settingsRepository,
        ISecretEncryptionService encryptionService,
        ILogger<SnelStartLatestBookingDateService> logger)
    {
        _httpClient = httpClient;
        _bankAccountRepository = bankAccountRepository;
        _settingsRepository = settingsRepository;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<DateTime?> GetLatestBookingDateAsync(
        Guid tenantId,
        Guid bankAccountId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bankAccount = await _bankAccountRepository.GetByIdAsync(bankAccountId, cancellationToken);

            if (bankAccount is null || bankAccount.TenantId != tenantId)
            {
                _logger.LogWarning("BankAccount niet gevonden.");
                return null;
            }

            if (bankAccount.SnelStartDagboek is null)
            {
                _logger.LogWarning("Geen SnelStartDagboekId ingesteld.");
                return null;
            }

            var settings = await _settingsRepository.GetByBankAccountIdAsync(bankAccountId, cancellationToken);

            if (settings is null)
            {
                _logger.LogWarning("Geen SnelStart settings gevonden.");
                return null;
            }

            var accessToken = await GetAccessTokenAsync(
                settings.SnelStartAuthUrl!,
                settings.SnelStartClientKey!,
                cancellationToken);

            var subscriptionKey = _encryptionService.Decrypt(settings.SnelStartSubscriptionKeyEncrypted!);

            var baseUrl = settings.SnelStartApiBaseUrl!.TrimEnd('/');

            if (!baseUrl.EndsWith("/v2", StringComparison.OrdinalIgnoreCase))
            {
                baseUrl += "/v2";
            }

            var endpoint =
                $"{baseUrl}/bankboekingen?$filter=dagboek/id eq {bankAccount.SnelStartDagboek.Id}&$orderby=datum desc&$top=1";

            _logger.LogInformation("SnelStart latest booking endpoint: {Endpoint}", endpoint);

            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "SnelStart latest booking date call failed. StatusCode: {StatusCode}, Response: {Response}",
                    (int)response.StatusCode,
                    body);

                return null;
            }

            using var doc = JsonDocument.Parse(body);

            var root = doc.RootElement;

            JsonElement? items = null;

            if (root.ValueKind == JsonValueKind.Array)
            {
                items = root;
            }
            else if (root.TryGetProperty("value", out var value) && value.ValueKind == JsonValueKind.Array)
            {
                items = value;
            }

            if (items is null || items.Value.GetArrayLength() == 0)
            {
                return null;
            }

            var first = items.Value[0];

            if (!first.TryGetProperty("datum", out var datumProp))
            {
                return null;
            }

            var rawDate = datumProp.GetString();

            if (DateTime.TryParse(rawDate, out var date))
            {
                _logger.LogInformation("Laatste boekingsdatum gevonden: {Date}", date);
                return date;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij ophalen laatste boekingsdatum uit SnelStart.");
            return null;
        }
    }

    private async Task<string> GetAccessTokenAsync(
        string authUrl,
        string clientKey,
        CancellationToken cancellationToken)
    {
        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, authUrl)
        {
            Content = new StringContent(
         $"grant_type=clientkey&clientkey={Uri.EscapeDataString(clientKey.Trim())}",
         System.Text.Encoding.UTF8,
         "application/x-www-form-urlencoded")
        };

        using var response = await _httpClient.SendAsync(tokenRequest, cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Token retrieval failed.");
        }

        using var doc = JsonDocument.Parse(body);

        return doc.RootElement.GetProperty("access_token").GetString()!;
    }
}