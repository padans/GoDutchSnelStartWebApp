using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Application.Abstractions.Repositories.SnelStart;
using GoDutchSnelStartWebApp.Application.Abstractions.Security;
using GoDutchSnelStartWebApp.Application.SnelStartLookups;
using GoDutchSnelStartWebApp.Application.SnelStartLookups.Dtos;
using GoDutchSnelStartWebApp.Application.SnelStartLookups.Interfaces;
using GoDutchSnelStartWebApp.Domain.Entities.SnelStart;
using GoDutchSnelStartWebApp.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace GoDutchSnelStartWebApp.Infrastructure.ExternalServices.SnelStart;

public sealed class SnelStartLookupService : ISnelStartLookupService
{

    private readonly HttpClient _httpClient;
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly IBankAccountSettingsRepository _bankAccountSettingsRepository;
    private readonly ITenantSnelStartConnectionRepository _tenantSnelStartConnectionRepository;
    private readonly ISecretEncryptionService _secretEncryptionService;
    private readonly ILogger<SnelStartLookupService> _logger;

    public SnelStartLookupService(
        HttpClient httpClient,
        IBankAccountRepository bankAccountRepository,
        IBankAccountSettingsRepository bankAccountSettingsRepository,
        ITenantSnelStartConnectionRepository tenantSnelStartConnectionRepository,
        ISecretEncryptionService secretEncryptionService,
        ILogger<SnelStartLookupService> logger)
    {
        _httpClient = httpClient;
        _bankAccountRepository = bankAccountRepository;
        _bankAccountSettingsRepository = bankAccountSettingsRepository;
        _tenantSnelStartConnectionRepository = tenantSnelStartConnectionRepository;
        _secretEncryptionService = secretEncryptionService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SnelStartDagboekLookupDto>> GetDagboekenAsync(
        Guid tenantId,
        Guid bankAccountId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "SnelStart dagboeken ophalen gestart. TenantId: {TenantId}, BankAccountId: {BankAccountId}.",
            tenantId,
            bankAccountId);

        var settings = await GetValidatedSettingsAsync(tenantId, bankAccountId, cancellationToken);

        var accessToken = await GetAccessTokenAsync(
            settings.SnelStartAuthUrl!,
            settings.SnelStartClientKey!,
            cancellationToken);

        var subscriptionKey = _secretEncryptionService.Decrypt(settings.SnelStartSubscriptionKeyEncrypted!);

        if (string.IsNullOrWhiteSpace(subscriptionKey))
        {
            throw new InvalidOperationException("SnelStart subscription key could not be decrypted.");
        }

        var baseUrl = settings.SnelStartApiBaseUrl!.TrimEnd('/');
        var endpoint = $"{baseUrl}/dagboeken?$top=200";

        var result = await GetLookupItemsAsync(
            endpoint,
            accessToken,
            subscriptionKey,
            item => new SnelStartDagboekLookupDto
            {
                Id = TryGetGuid(item, "id") ?? Guid.Empty,
                Nummer = TryGetString(item, "nummer"),
                Omschrijving = TryGetString(item, "omschrijving")
            },
            "dagboeken",
            tenantId,
            cancellationToken);

        return result
            .Where(x => x.Id != Guid.Empty)
            .OrderBy(x => x.Nummer)
            .ThenBy(x => x.Omschrijving)
            .ToList();
    }

    public async Task<IReadOnlyList<SnelStartGrootboekLookupDto>> GetGrootboekenAsync(
        Guid tenantId,
        Guid bankAccountId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "SnelStart grootboeken ophalen gestart. TenantId: {TenantId}, BankAccountId: {BankAccountId}.",
            tenantId,
            bankAccountId);

        var settings = await GetValidatedSettingsAsync(tenantId, bankAccountId, cancellationToken);

        var accessToken = await GetAccessTokenAsync(
            settings.SnelStartAuthUrl!,
            settings.SnelStartClientKey!,
            cancellationToken);

        var subscriptionKey = _secretEncryptionService.Decrypt(settings.SnelStartSubscriptionKeyEncrypted!);

        if (string.IsNullOrWhiteSpace(subscriptionKey))
        {
            throw new InvalidOperationException("SnelStart subscription key could not be decrypted.");
        }

        var baseUrl = settings.SnelStartApiBaseUrl!.TrimEnd('/');

        return await GetGrootboekenCoreAsync(
            tenantId,
            baseUrl,
            accessToken,
            subscriptionKey,
            cancellationToken);
    }

    public async Task<IReadOnlyList<SnelStartGrootboekLookupDto>> GetTenantGrootboekenAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Tenantgerichte SnelStart grootboeken ophalen gestart. TenantId: {TenantId}.",
            tenantId);

        var connection = await GetValidatedTenantConnectionAsync(tenantId, cancellationToken);

        var subscriptionKey = _secretEncryptionService.Decrypt(connection.SubscriptionKeyEncrypted!);
        var clientKey = _secretEncryptionService.Decrypt(connection.ClientKeyEncrypted!);

        if (string.IsNullOrWhiteSpace(subscriptionKey))
        {
            throw new InvalidOperationException("SnelStart subscription key could not be decrypted.");
        }

        if (string.IsNullOrWhiteSpace(clientKey))
        {
            throw new InvalidOperationException("SnelStart client key could not be decrypted.");
        }

        var accessToken = await GetAccessTokenAsync(
            connection.AuthUrl,
            clientKey,
            cancellationToken);

        var baseUrl = NormalizeApiBaseUrl(connection.ApiBaseUrl);

        return await GetGrootboekenCoreAsync(
            tenantId,
            baseUrl,
            accessToken,
            subscriptionKey,
            cancellationToken);
    }

    public async Task<SnelStartGrootboekLookupDto> CreateTenantGrootboekAsync(
        Guid tenantId,
        CreateSnelStartGrootboekRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.Nummer <= 0)
        {
            throw new InvalidOperationException("Grootboek number is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Omschrijving))
        {
            throw new InvalidOperationException("Description is required.");
        }

        _logger.LogInformation(
            "Tenantgericht SnelStart grootboek aanmaken gestart. TenantId: {TenantId}, Nummer: {Nummer}, Omschrijving: {Omschrijving}.",
            tenantId,
            request.Nummer,
            request.Omschrijving);

        var connection = await GetValidatedTenantConnectionAsync(tenantId, cancellationToken);

        var subscriptionKey = _secretEncryptionService.Decrypt(connection.SubscriptionKeyEncrypted!);
        var clientKey = _secretEncryptionService.Decrypt(connection.ClientKeyEncrypted!);

        if (string.IsNullOrWhiteSpace(subscriptionKey))
        {
            throw new InvalidOperationException("SnelStart subscription key could not be decrypted.");
        }

        if (string.IsNullOrWhiteSpace(clientKey))
        {
            throw new InvalidOperationException("SnelStart client key could not be decrypted.");
        }

        var accessToken = await GetAccessTokenAsync(
            connection.AuthUrl,
            clientKey,
            cancellationToken);

        var baseUrl = NormalizeApiBaseUrl(connection.ApiBaseUrl);
        var endpoint = $"{baseUrl}/grootboeken";

        var payload = new
        {
            omschrijving = request.Omschrijving.Trim(),
            kostenplaatsVerplicht = request.KostenplaatsVerplicht,
            rekeningCode = string.IsNullOrWhiteSpace(request.RekeningCode)
                ? "WinstEnVerlies"
                : request.RekeningCode.Trim(),
            nonactief = request.Nonactief,
            nummer = request.Nummer,
            grootboekfunctie = string.IsNullOrWhiteSpace(request.Grootboekfunctie)
                ? "Diversen"
                : request.Grootboekfunctie.Trim(),
            btwSoort = request.BtwSoort is { Count: > 0 }
                ? request.BtwSoort
                : ["Geen"]
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpRequest.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "SnelStart grootboek aanmaken mislukt. TenantId: {TenantId}, StatusCode: {StatusCode}, Response: {Response}.",
                tenantId,
                (int)response.StatusCode,
                Truncate(body));

            throw new SnelStartLookupException(
                $"SnelStart grootboek aanmaken mislukt met HTTP {(int)response.StatusCode}.",
                (int)response.StatusCode);
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            _logger.LogInformation(
                "SnelStart grootboek aangemaakt, maar response body was leeg. Grootboeken worden opnieuw opgehaald. TenantId: {TenantId}, Nummer: {Nummer}.",
                tenantId,
                request.Nummer);

            var refreshed = await GetTenantGrootboekenAsync(tenantId, cancellationToken);
            var created = refreshed.FirstOrDefault(x => string.Equals(x.Nummer, request.Nummer.ToString(), StringComparison.OrdinalIgnoreCase));

            if (created is not null)
            {
                return created;
            }

            return new SnelStartGrootboekLookupDto
            {
                Nummer = request.Nummer.ToString(),
                Omschrijving = request.Omschrijving.Trim()
            };
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var createdDto = new SnelStartGrootboekLookupDto
        {
            Id = TryGetGuid(root, "id") ?? Guid.Empty,
            Nummer = TryGetString(root, "nummer") ?? request.Nummer.ToString(),
            Omschrijving = TryGetString(root, "omschrijving") ?? request.Omschrijving.Trim()
        };

        _logger.LogInformation(
            "SnelStart grootboek aangemaakt. TenantId: {TenantId}, GrootboekId: {GrootboekId}, Nummer: {Nummer}.",
            tenantId,
            createdDto.Id,
            createdDto.Nummer);

        return createdDto;
    }

    private async Task<IReadOnlyList<SnelStartGrootboekLookupDto>> GetGrootboekenCoreAsync(
        Guid tenantId,
        string baseUrl,
        string accessToken,
        string subscriptionKey,
        CancellationToken cancellationToken)
    {
        var endpoint = $"{baseUrl}/grootboeken?$top=500";

        var result = await GetLookupItemsAsync(
            endpoint,
            accessToken,
            subscriptionKey,
            item => new SnelStartGrootboekLookupDto
            {
                Id = TryGetGuid(item, "id") ?? Guid.Empty,
                Nummer = TryGetString(item, "nummer"),
                Omschrijving = TryGetString(item, "omschrijving")
            },
            "grootboeken",
            tenantId,
            cancellationToken);

        var ordered = result
            .Where(x => x.Id != Guid.Empty)
            .OrderBy(x => x.Nummer)
            .ThenBy(x => x.Omschrijving)
            .ToList();

        _logger.LogInformation(
            "SnelStart grootboeken ophalen afgerond. TenantId: {TenantId}, Count: {Count}.",
            tenantId,
            ordered.Count);

        return ordered;
    }

    private async Task<List<T>> GetLookupItemsAsync<T>(
        string endpoint,
        string accessToken,
        string subscriptionKey,
        Func<JsonElement, T> map,
        string lookupName,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "SnelStart {LookupName} endpoint opgebouwd. TenantId: {TenantId}, Endpoint: {Endpoint}.",
            lookupName,
            tenantId,
            endpoint);

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "SnelStart {LookupName} ophalen mislukt. TenantId: {TenantId}, StatusCode: {StatusCode}, Response: {Response}.",
                lookupName,
                tenantId,
                (int)response.StatusCode,
                Truncate(body));

            throw new SnelStartLookupException(
                $"SnelStart {lookupName} ophalen mislukt met HTTP {(int)response.StatusCode}.",
                (int)response.StatusCode);
        }

        using var doc = JsonDocument.Parse(body);
        var items = GetItemsArray(doc.RootElement);

        if (items is null)
        {
            _logger.LogWarning(
                "SnelStart {LookupName} response heeft geen geldig array-formaat. TenantId: {TenantId}.",
                lookupName,
                tenantId);

            return [];
        }

        var result = new List<T>();

        foreach (var item in items.Value.EnumerateArray())
        {
            result.Add(map(item));
        }

        return result;
    }

    private async Task<Domain.Entities.BankAccountSetting> GetValidatedSettingsAsync(
        Guid tenantId,
        Guid bankAccountId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Valideren van BankAccount en settings gestart. TenantId: {TenantId}, BankAccountId: {BankAccountId}.",
            tenantId,
            bankAccountId);

        var bankAccount = await _bankAccountRepository.GetByIdAsync(bankAccountId, cancellationToken);
        if (bankAccount is null || bankAccount.TenantId != tenantId)
        {
            throw new KeyNotFoundException("Bank account not found.");
        }

        var settings = await _bankAccountSettingsRepository.GetByBankAccountIdAsync(bankAccountId, cancellationToken);
        if (settings is null)
        {
            throw new KeyNotFoundException("Bank account settings not found.");
        }

        if (string.IsNullOrWhiteSpace(settings.SnelStartAuthUrl))
        {
            throw new InvalidOperationException("SnelStartAuthUrl is missing.");
        }

        if (string.IsNullOrWhiteSpace(settings.SnelStartApiBaseUrl))
        {
            throw new InvalidOperationException("SnelStartApiBaseUrl is missing.");
        }

        if (string.IsNullOrWhiteSpace(settings.SnelStartClientKey))
        {
            throw new InvalidOperationException("SnelStartClientKey is missing.");
        }

        if (string.IsNullOrWhiteSpace(settings.SnelStartSubscriptionKeyEncrypted))
        {
            throw new InvalidOperationException("SnelStartSubscriptionKey is missing.");
        }

        return settings;
    }

    private async Task<TenantSnelStartConnection> GetValidatedTenantConnectionAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty)
        {
            throw new InvalidOperationException("TenantId is missing.");
        }

        var connection = await _tenantSnelStartConnectionRepository.GetByTenantIdAsync(
            tenantId,
            cancellationToken);

        if (connection is null)
        {
            throw new KeyNotFoundException("Tenant SnelStart connection not found.");
        }

        if (!connection.IsActive)
        {
            throw new InvalidOperationException("Tenant SnelStart connection is not active.");
        }

        if (connection.ConnectionType != SnelStartConnectionType.CustomKey)
        {
            throw new NotSupportedException("Only CustomKey SnelStart connections are currently supported. OAuth will be added later.");
        }

        if (string.IsNullOrWhiteSpace(connection.AuthUrl))
        {
            throw new InvalidOperationException("SnelStart AuthUrl is missing.");
        }

        if (string.IsNullOrWhiteSpace(connection.ApiBaseUrl))
        {
            throw new InvalidOperationException("SnelStart ApiBaseUrl is missing.");
        }

        if (string.IsNullOrWhiteSpace(connection.ClientKeyEncrypted))
        {
            throw new InvalidOperationException("SnelStart ClientKey is missing.");
        }

        if (string.IsNullOrWhiteSpace(connection.SubscriptionKeyEncrypted))
        {
            throw new InvalidOperationException("SnelStart SubscriptionKey is missing.");
        }

        return connection;
    }

    private async Task<string> GetAccessTokenAsync(
        string authUrl,
        string clientKey,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "SnelStart access token ophalen gestart. AuthUrl: {AuthUrl}, ClientKeyLength: {ClientKeyLength}.",
            authUrl,
            clientKey.Trim().Length);

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, authUrl)
        {
            Content = new StringContent(
                $"grant_type=clientkey&clientkey={Uri.EscapeDataString(clientKey.Trim())}",
                System.Text.Encoding.UTF8,
                "application/x-www-form-urlencoded")
        };

        using var tokenResponse = await _httpClient.SendAsync(tokenRequest, cancellationToken);
        var tokenBody = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "SnelStart token ophalen mislukt. StatusCode: {StatusCode}, ResponseLength: {ResponseLength}.",
                (int)tokenResponse.StatusCode,
                tokenBody.Length);

            throw new SnelStartLookupException(
                $"SnelStart token request failed with HTTP {(int)tokenResponse.StatusCode}.",
                (int)tokenResponse.StatusCode);
        }

        using var tokenDoc = JsonDocument.Parse(tokenBody);

        if (!tokenDoc.RootElement.TryGetProperty("access_token", out var accessTokenElement))
        {
            _logger.LogWarning(
                "SnelStart token response bevat geen access_token. ResponseLength: {ResponseLength}.",
                tokenBody.Length);

            throw new SnelStartLookupException("SnelStart token response does not contain an access_token.");
        }

        var accessToken = accessTokenElement.GetString();

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new SnelStartLookupException("SnelStart returned an empty access_token.");
        }

        _logger.LogDebug("SnelStart access token succesvol opgehaald.");

        return accessToken;
    }

    private static string NormalizeApiBaseUrl(string value)
    {
        var baseUrl = value.Trim().TrimEnd('/');

        return baseUrl.EndsWith("/v2", StringComparison.OrdinalIgnoreCase)
            ? baseUrl
            : baseUrl + "/v2";
    }

    private static JsonElement? GetItemsArray(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            return root;
        }

        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("value", out var value) &&
            value.ValueKind == JsonValueKind.Array)
        {
            return value;
        }

        return null;
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : property.ToString();
    }

    private static Guid? TryGetGuid(JsonElement element, string propertyName)
    {
        var raw = TryGetString(element, propertyName);

        return Guid.TryParse(raw, out var guid)
            ? guid
            : null;
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

    public async Task<IReadOnlyList<SnelStartBtwTariefLookupDto>> GetTenantBtwTarievenAsync(
    Guid tenantId,
    CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Tenantgerichte SnelStart btw-tarieven ophalen gestart. TenantId: {TenantId}.",
            tenantId);

        var connection = await GetValidatedTenantConnectionAsync(tenantId, cancellationToken);

        var subscriptionKey = _secretEncryptionService.Decrypt(connection.SubscriptionKeyEncrypted!);
        var clientKey = _secretEncryptionService.Decrypt(connection.ClientKeyEncrypted!);

        if (string.IsNullOrWhiteSpace(subscriptionKey))
        {
            throw new InvalidOperationException("SnelStart subscription key could not be decrypted.");
        }

        if (string.IsNullOrWhiteSpace(clientKey))
        {
            throw new InvalidOperationException("SnelStart client key could not be decrypted.");
        }

        var accessToken = await GetAccessTokenAsync(
            connection.AuthUrl,
            clientKey,
            cancellationToken);

        var baseUrl = NormalizeApiBaseUrl(connection.ApiBaseUrl);

        return await GetBtwTarievenCoreAsync(
            tenantId,
            baseUrl,
            accessToken,
            subscriptionKey,
            cancellationToken);
    }

    private async Task<IReadOnlyList<SnelStartBtwTariefLookupDto>> GetBtwTarievenCoreAsync(
    Guid tenantId,
    string baseUrl,
    string accessToken,
    string subscriptionKey,
    CancellationToken cancellationToken)
    {
        var endpoint = $"{baseUrl}/btwtarieven";

        var result = await GetLookupItemsAsync(
            endpoint,
            accessToken,
            subscriptionKey,
            item => new SnelStartBtwTariefLookupDto
            {
                BtwSoort = TryGetString(item, "btwSoort") ?? string.Empty,
                BtwPercentage = TryGetDecimal(item, "btwPercentage") ?? 0m,
                DatumVanaf = TryGetDateTime(item, "datumVanaf"),
                DatumTotEnMet = TryGetDateTime(item, "datumTotEnMet")
            },
            "btw-tarieven",
            tenantId,
            cancellationToken);

        var ordered = result
            .Where(x => !string.IsNullOrWhiteSpace(x.BtwSoort))
            .OrderBy(x => x.BtwSoort)
            .ThenByDescending(x => x.DatumVanaf)
            .ToList();

        _logger.LogInformation(
            "SnelStart btw-tarieven ophalen afgerond. TenantId: {TenantId}, Count: {Count}.",
            tenantId,
            ordered.Count);

        return ordered;
    }

    private static decimal? TryGetDecimal(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number &&
            property.TryGetDecimal(out var value))
        {
            return value;
        }

        var raw = property.ToString();

        return decimal.TryParse(
            raw,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : null;
    }

    private static DateTime? TryGetDateTime(JsonElement element, string propertyName)
    {
        var raw = TryGetString(element, propertyName);

        return DateTime.TryParse(
            raw,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal,
            out var value)
            ? value
            : null;
    }
    public async Task<IReadOnlyList<SnelStartDagboekLookupDto>> GetTenantDagboekenAsync(
    Guid tenantId,
    CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Tenantgerichte SnelStart dagboeken ophalen gestart. TenantId: {TenantId}.",
            tenantId);

        var connection = await GetValidatedTenantConnectionAsync(tenantId, cancellationToken);

        var subscriptionKey = _secretEncryptionService.Decrypt(connection.SubscriptionKeyEncrypted!);
        var clientKey = _secretEncryptionService.Decrypt(connection.ClientKeyEncrypted!);

        if (string.IsNullOrWhiteSpace(subscriptionKey))
        {
            throw new InvalidOperationException("SnelStart subscription key could not be decrypted.");
        }

        if (string.IsNullOrWhiteSpace(clientKey))
        {
            throw new InvalidOperationException("SnelStart client key could not be decrypted.");
        }

        var accessToken = await GetAccessTokenAsync(
            connection.AuthUrl,
            clientKey,
            cancellationToken);

        var baseUrl = NormalizeApiBaseUrl(connection.ApiBaseUrl);

        return await GetDagboekenCoreAsync(
            tenantId,
            baseUrl,
            accessToken,
            subscriptionKey,
            cancellationToken);
    }
    private async Task<IReadOnlyList<SnelStartDagboekLookupDto>> GetDagboekenCoreAsync(
    Guid tenantId,
    string baseUrl,
    string accessToken,
    string subscriptionKey,
    CancellationToken cancellationToken)
    {
        var endpoint = $"{baseUrl}/dagboeken?$top=200";

        var result = await GetLookupItemsAsync(
            endpoint,
            accessToken,
            subscriptionKey,
            item => new SnelStartDagboekLookupDto
            {
                Id = TryGetGuid(item, "id") ?? Guid.Empty,
                Nummer = TryGetString(item, "nummer"),
                Omschrijving = TryGetString(item, "omschrijving")
            },
            "dagboeken",
            tenantId,
            cancellationToken);

        var ordered = result
            .Where(x => x.Id != Guid.Empty)
            .OrderBy(x => x.Nummer)
            .ThenBy(x => x.Omschrijving)
            .ToList();

        _logger.LogInformation(
            "SnelStart dagboeken ophalen afgerond. TenantId: {TenantId}, Count: {Count}.",
            tenantId,
            ordered.Count);

        return ordered;
    }
}
