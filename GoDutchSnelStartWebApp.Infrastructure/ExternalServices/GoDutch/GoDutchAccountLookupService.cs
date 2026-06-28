using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Application.Abstractions.Security;
using GoDutchSnelStartWebApp.Application.Configuration;
using GoDutchSnelStartWebApp.Application.GoDutchAccounts.Dtos;
using GoDutchSnelStartWebApp.Application.GoDutchAccounts.Interfaces;
using GoDutchSnelStartWebApp.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GoDutchSnelStartWebApp.Infrastructure.ExternalServices.GoDutch;

public sealed class GoDutchAccountLookupService : IGoDutchAccountLookupService
{
    private readonly HttpClient _httpClient;
    private readonly ITenantGoDutchConnectionRepository _connectionRepository;
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly ISecretEncryptionService _encryptionService;
    private readonly ILogger<GoDutchAccountLookupService> _logger;
    private readonly GoDutchApiRetryOptions _retryOptions;
    private readonly Random _random = new();

    public GoDutchAccountLookupService(
        HttpClient httpClient,
        ITenantGoDutchConnectionRepository connectionRepository,
        IBankAccountRepository bankAccountRepository,
        ISecretEncryptionService encryptionService,
        ILogger<GoDutchAccountLookupService> logger,
        IOptions<GoDutchApiRetryOptions> retryOptions)
    {
        _httpClient = httpClient;
        _connectionRepository = connectionRepository;
        _bankAccountRepository = bankAccountRepository;
        _encryptionService = encryptionService;
        _logger = logger;
        _retryOptions = retryOptions.Value;
    }

    public async Task<IReadOnlyList<GoDutchAccountLookupDto>> GetAccountsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var connection = await _connectionRepository.GetByTenantIdAsync(tenantId, cancellationToken);
        ConfigureHttpClient(connection, tenantId);

        var existingBankAccounts = (await _bankAccountRepository.GetByTenantIdAsync(
            tenantId,
            cancellationToken)).ToList();

        var existingBankAccountsByIban = existingBankAccounts
            .Where(x => !string.IsNullOrWhiteSpace(x.Iban))
            .GroupBy(x => NormalizeIban(x.Iban), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation(
            "GoDutch bankrekening lookup gestart. TenantId: {TenantId}, bestaande bankaccounts: {ExistingCount}.",
            tenantId,
            existingBankAccounts.Count);

        var accountsJson = await SendGetWithRetryAsync(
            relativeUrl: "api/accounts",
            operationName: "GoDutch accounts lookup",
            tenantId: tenantId,
            cancellationToken: cancellationToken);

        using var accountsDoc = JsonDocument.Parse(accountsJson);

        if (!accountsDoc.RootElement.TryGetProperty("results", out var results) ||
            results.ValueKind != JsonValueKind.Array)
        {
            _logger.LogWarning(
                "GoDutch accounts lookup response bevat geen geldige results-array. TenantId: {TenantId}.",
                tenantId);

            return Array.Empty<GoDutchAccountLookupDto>();
        }

        var accounts = new List<GoDutchAccountLookupDto>();

        foreach (var account in results.EnumerateArray())
        {
            var accountId = GetString(account, "id") ?? string.Empty;
            var iban = NormalizeIban(GetString(account, "iban"));

            if (string.IsNullOrWhiteSpace(accountId) && string.IsNullOrWhiteSpace(iban))
            {
                continue;
            }

            var accountName = GetFirstString(
                account,
                "accountName",
                "name",
                "label",
                "description");

            var accountHolderName = GetFirstString(
                account,
                "accountHolderName",
                "holderName",
                "ownerName");

            existingBankAccountsByIban.TryGetValue(iban, out var existingBankAccount);

            var dto = new GoDutchAccountLookupDto
            {
                GoDutchAccountId = accountId,
                Iban = iban,
                AccountName = accountName,
                AccountHolderName = accountHolderName,
                AlreadyExists = existingBankAccount is not null,
                ExistingBankAccountId = existingBankAccount?.Id
            };

            accounts.Add(dto);

            _logger.LogDebug(
                "GoDutch account lookup gelezen. TenantId: {TenantId}, AccountId: {AccountId}, IBAN: {Iban}, AlreadyExists: {AlreadyExists}, ExistingBankAccountId: {ExistingBankAccountId}.",
                tenantId,
                dto.GoDutchAccountId,
                dto.Iban,
                dto.AlreadyExists,
                dto.ExistingBankAccountId);
        }

        var orderedAccounts = accounts
            .OrderBy(x => x.AlreadyExists)
            .ThenBy(x => x.Iban)
            .ThenBy(x => x.GoDutchAccountId)
            .ToList();

        _logger.LogInformation(
            "GoDutch bankrekening lookup afgerond. TenantId: {TenantId}, ontvangen: {ReceivedCount}, bestaande matches: {ExistingMatches}.",
            tenantId,
            orderedAccounts.Count,
            orderedAccounts.Count(x => x.AlreadyExists));

        return orderedAccounts;
    }

    private void ConfigureHttpClient(TenantGoDutchConnection? connection, Guid tenantId)
    {
        if (connection is null)
        {
            throw new InvalidOperationException("TenantGoDutchConnection not found.");
        }

        if (!connection.IsActive)
        {
            throw new InvalidOperationException("TenantGoDutchConnection is not active.");
        }

        if (string.IsNullOrWhiteSpace(connection.ApiBaseUrl))
        {
            throw new InvalidOperationException("GoDutch ApiBaseUrl missing for tenant connection.");
        }

        if (string.IsNullOrWhiteSpace(connection.Username))
        {
            throw new InvalidOperationException("GoDutch Username missing for tenant connection.");
        }

        if (string.IsNullOrWhiteSpace(connection.PasswordEncrypted))
        {
            throw new InvalidOperationException("GoDutch Password missing for tenant connection.");
        }

        var password = _encryptionService.Decrypt(connection.PasswordEncrypted);

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("GoDutch password could not be decrypted for tenant connection.");
        }

        var normalizedBaseUrl = connection.ApiBaseUrl.Trim().TrimEnd('/') + "/";
        _httpClient.BaseAddress = new Uri(normalizedBaseUrl);

        var authValue = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{connection.Username}:{password}"));

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", authValue);

        _logger.LogDebug(
            "GoDutch HttpClient geconfigureerd vanuit tenant connection. TenantId: {TenantId}, ApiBaseUrl: {ApiBaseUrl}.",
            tenantId,
            normalizedBaseUrl);
    }

    private async Task<string> SendGetWithRetryAsync(
        string relativeUrl,
        string operationName,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var maxAttempts = NormalizeMaxAttempts(_retryOptions.MaxAttempts);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var response = await _httpClient.GetAsync(relativeUrl, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug(
                "{OperationName} endpoint aangeroepen. TenantId: {TenantId}, Attempt: {Attempt}/{MaxAttempts}, StatusCode: {StatusCode}, ResponseLength: {ResponseLength}, Url: {Url}.",
                operationName,
                tenantId,
                attempt,
                maxAttempts,
                (int)response.StatusCode,
                body?.Length ?? 0,
                relativeUrl);

            if (response.IsSuccessStatusCode)
            {
                if (string.IsNullOrWhiteSpace(body))
                {
                    throw new InvalidOperationException($"{operationName} response was leeg.");
                }

                return body;
            }

            if (!ShouldRetry(response.StatusCode, attempt, maxAttempts))
            {
                throw new InvalidOperationException(
                    $"{operationName} call failed: {(int)response.StatusCode} - {body}");
            }

            var delay = CalculateDelay(response, attempt);

            _logger.LogWarning(
                "{OperationName} tijdelijk mislukt. Retry volgt. TenantId: {TenantId}, Attempt: {Attempt}/{MaxAttempts}, StatusCode: {StatusCode}, DelayMs: {DelayMs}.",
                operationName,
                tenantId,
                attempt,
                maxAttempts,
                (int)response.StatusCode,
                (int)delay.TotalMilliseconds);

            await Task.Delay(delay, cancellationToken);
        }

        throw new InvalidOperationException(
            $"{operationName} call failed after {maxAttempts} attempts.");
    }

    private bool ShouldRetry(HttpStatusCode statusCode, int attempt, int maxAttempts)
    {
        if (!_retryOptions.Enabled)
        {
            return false;
        }

        if (attempt >= maxAttempts)
        {
            return false;
        }

        return statusCode == HttpStatusCode.TooManyRequests
               || statusCode == HttpStatusCode.RequestTimeout
               || (int)statusCode >= 500;
    }

    private TimeSpan CalculateDelay(HttpResponseMessage response, int attempt)
    {
        var retryAfter = response.Headers.RetryAfter;

        if (retryAfter?.Delta is TimeSpan delta && delta > TimeSpan.Zero)
        {
            return delta;
        }

        if (retryAfter?.Date is DateTimeOffset retryDate)
        {
            var retryDelay = retryDate - DateTimeOffset.UtcNow;
            if (retryDelay > TimeSpan.Zero)
            {
                return retryDelay;
            }
        }

        var initialDelayMs = _retryOptions.InitialDelayMilliseconds <= 0
            ? 1500
            : _retryOptions.InitialDelayMilliseconds;

        var maxDelayMs = _retryOptions.MaxDelayMilliseconds <= 0
            ? 15000
            : _retryOptions.MaxDelayMilliseconds;

        var jitterMs = _retryOptions.JitterMilliseconds <= 0
            ? 0
            : _random.Next(0, _retryOptions.JitterMilliseconds + 1);

        var multiplier = Math.Pow(2, attempt - 1);
        var calculatedDelayMs = (int)Math.Min(initialDelayMs * multiplier, maxDelayMs);

        return TimeSpan.FromMilliseconds(calculatedDelayMs + jitterMs);
    }

    private static int NormalizeMaxAttempts(int maxAttempts)
    {
        return maxAttempts < 1 ? 1 : maxAttempts;
    }

    private static string NormalizeIban(string? iban)
    {
        return (iban ?? string.Empty)
            .Replace(" ", string.Empty)
            .Trim()
            .ToUpperInvariant();
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : property.ToString();
    }

    private static string? GetFirstString(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var value = GetString(element, propertyName);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
