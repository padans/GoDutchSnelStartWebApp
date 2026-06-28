using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Application.Abstractions.Security;
using GoDutchSnelStartWebApp.Application.Configuration;
using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Dtos;
using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Interfaces;
using GoDutchSnelStartWebApp.Domain.Entities;
using GoDutchSnelStartWebApp.Infrastructure.Models.GoDutchApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GoDutchSnelStartWebApp.Infrastructure.ExternalServices.GoDutch;

public sealed class GoDutchTransactionService : IGoDutchTransactionService
{
    private readonly HttpClient _httpClient;
    private readonly ITenantGoDutchConnectionRepository _connectionRepository;
    private readonly ISecretEncryptionService _encryptionService;
    private readonly ILogger<GoDutchTransactionService> _logger;
    private readonly GoDutchApiRetryOptions _retryOptions;
    private readonly Random _random = new();

    public GoDutchTransactionService(
        HttpClient httpClient,
        ITenantGoDutchConnectionRepository connectionRepository,
        ISecretEncryptionService encryptionService,
        ILogger<GoDutchTransactionService> logger,
        IOptions<GoDutchApiRetryOptions> retryOptions)
    {
        _httpClient = httpClient;
        _connectionRepository = connectionRepository;
        _encryptionService = encryptionService;
        _logger = logger;
        _retryOptions = retryOptions.Value;
    }

    public async Task<IReadOnlyList<BankTransactionDto>> GetTransactionsAsync(
        Guid tenantId,
        Guid bankAccountId,
        string iban,
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
    {
        var connection = await _connectionRepository.GetByTenantIdAsync(tenantId, ct);
        var requestContext = CreateRequestContext(connection, tenantId);

        if (string.IsNullOrWhiteSpace(iban))
        {
            throw new InvalidOperationException("IBAN is required.");
        }

        var normalizedIban = NormalizeIban(iban);
        var fromDate = from.Date;
        var toDate = to.Date;

        if (toDate < fromDate)
        {
            throw new InvalidOperationException("'to' must be greater than or equal to 'from'.");
        }

        _logger.LogInformation(
            "GoDutch transacties ophalen gestart met tenant connection. TenantId: {TenantId}, BankAccountId: {BankAccountId}, IBAN: {Iban}, periode: {FromDate:yyyy-MM-dd} t/m {ToDate:yyyy-MM-dd}.",
            tenantId,
            bankAccountId,
            normalizedIban,
            fromDate,
            toDate);

        var matchedAccountId = await ResolveAccountIdByIbanAsync(requestContext, normalizedIban, ct);

        if (string.IsNullOrWhiteSpace(matchedAccountId))
        {
            _logger.LogWarning("Geen GoDutch account gevonden voor IBAN {Iban}.", normalizedIban);
            return Array.Empty<BankTransactionDto>();
        }

        _logger.LogInformation(
            "GoDutch account gevonden voor IBAN {Iban}. AccountId: {AccountId}.",
            normalizedIban,
            matchedAccountId);

        var txJson = await SendGetWithRetryAsync(
            context: requestContext,
            relativeUrl: $"api/accounts/{matchedAccountId}/transactions",
            operationName: "GoDutch transactions",
            bankAccountId: bankAccountId,
            iban: normalizedIban,
            ct: ct);

        var txData = JsonSerializer.Deserialize<TransactionsResponse>(
            txJson,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (txData?.Results is null || txData.Results.Count == 0)
        {
            _logger.LogWarning(
                "GoDutch transactions response bevat geen transacties voor account {AccountId}.",
                matchedAccountId);

            return Array.Empty<BankTransactionDto>();
        }

        _logger.LogInformation(
            "GoDutch raw transacties ontvangen voor account {AccountId}: {Count}.",
            matchedAccountId,
            txData.Results.Count);

        var inPeriodTransactions = new List<BankTransactionDto>();
        BankTransactionDto? lastBalanceAnchor = null;

        var skippedStatus = 0;
        var skippedAmount = 0;
        var skippedDate = 0;
        var skippedAfterRange = 0;

        foreach (var t in txData.Results)
        {
            if (!IsBookedOrEmpty(t.Status))
            {
                skippedStatus++;
                continue;
            }

            if (!TryParseAmount(t.Amount?.Value, out var amount))
            {
                skippedAmount++;
                continue;
            }

            if (string.Equals(t.Side, "Debit", StringComparison.OrdinalIgnoreCase))
            {
                amount = -Math.Abs(amount);
            }
            else
            {
                amount = Math.Abs(amount);
            }

            if (!TryParseTransactionDate(t.BookingDate, out var bookingDate))
            {
                skippedDate++;
                continue;
            }

            var bookingDay = bookingDate.Date;

            decimal? balanceAfter = null;
            if (TryParseAmount(t.BookedBalanceAfter?.Value, out var parsedBalanceAfter))
            {
                balanceAfter = parsedBalanceAfter;
            }

            var mapped = new BankTransactionDto
            {
                Id = string.IsNullOrWhiteSpace(t.Id) ? Guid.NewGuid().ToString("N") : t.Id,
                BookingDate = bookingDay,
                Amount = amount,
                Description = BuildDescription(t),
                BalanceAfter = balanceAfter,
                Currency = t.Amount?.Currency ?? "EUR",
                Status = t.Status,
                IsInRequestedPeriod = bookingDay >= fromDate && bookingDay <= toDate,
                IsBalanceAnchor = false
            };

            if (bookingDay < fromDate)
            {
                if (mapped.BalanceAfter.HasValue)
                {
                    if (lastBalanceAnchor is null ||
                        mapped.BookingDate > lastBalanceAnchor.BookingDate ||
                        (mapped.BookingDate == lastBalanceAnchor.BookingDate &&
                         string.CompareOrdinal(mapped.Id, lastBalanceAnchor.Id) > 0))
                    {
                        mapped.IsBalanceAnchor = true;
                        mapped.IsInRequestedPeriod = false;
                        lastBalanceAnchor = mapped;
                    }
                }

                continue;
            }

            if (bookingDay > toDate)
            {
                skippedAfterRange++;
                continue;
            }

            inPeriodTransactions.Add(mapped);
        }

        var result = new List<BankTransactionDto>();

        if (lastBalanceAnchor is not null)
        {
            result.Add(lastBalanceAnchor);
        }

        result.AddRange(inPeriodTransactions);

        _logger.LogInformation(
            "GoDutch transacties verwerkt. Raw: {RawCount}, resultaat: {ResultCount}, inPeriod: {InPeriodCount}, hasBalanceAnchor: {HasBalanceAnchor}, skipped status: {SkippedStatus}, skipped amount: {SkippedAmount}, skipped date: {SkippedDate}, skipped after range: {SkippedAfterRange}.",
            txData.Results.Count,
            result.Count,
            inPeriodTransactions.Count,
            lastBalanceAnchor is not null,
            skippedStatus,
            skippedAmount,
            skippedDate,
            skippedAfterRange);

        foreach (var tx in result.Take(10))
        {
            _logger.LogDebug(
                "Mapped transactie. Id: {Id}, BookingDate: {BookingDate:yyyy-MM-dd}, Amount: {Amount}, BalanceAfter: {BalanceAfter}, InRequestedPeriod: {IsInRequestedPeriod}, IsBalanceAnchor: {IsBalanceAnchor}, Description: {Description}.",
                tx.Id,
                tx.BookingDate,
                tx.Amount,
                tx.BalanceAfter,
                tx.IsInRequestedPeriod,
                tx.IsBalanceAnchor,
                tx.Description);
        }

        return result
            .OrderBy(x => x.BookingDate)
            .ThenBy(x => x.Id)
            .ToList();
    }

    private GoDutchRequestContext CreateRequestContext(TenantGoDutchConnection? connection, Guid tenantId)
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
        var baseUri = new Uri(normalizedBaseUrl, UriKind.Absolute);

        var authValue = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{connection.Username}:{password}"));

        var authorizationHeader = new AuthenticationHeaderValue("Basic", authValue);

        _logger.LogDebug(
            "GoDutch request context aangemaakt vanuit tenant connection. TenantId: {TenantId}, ApiBaseUrl: {ApiBaseUrl}.",
            tenantId,
            normalizedBaseUrl);

        return new GoDutchRequestContext(baseUri, authorizationHeader);
    }

    private async Task<string?> ResolveAccountIdByIbanAsync(
        GoDutchRequestContext context,
        string normalizedIban,
        CancellationToken ct)
    {
        var accountsJson = await SendGetWithRetryAsync(
            context: context,
            relativeUrl: "api/accounts",
            operationName: "GoDutch accounts",
            bankAccountId: null,
            iban: normalizedIban,
            ct: ct);

        using var accountsDoc = JsonDocument.Parse(accountsJson);

        if (!accountsDoc.RootElement.TryGetProperty("results", out var results) ||
            results.ValueKind != JsonValueKind.Array)
        {
            _logger.LogWarning("GoDutch accounts response bevat geen geldige results-array.");
            return null;
        }

        foreach (var account in results.EnumerateArray())
        {
            var accountIban = NormalizeIban(GetString(account, "iban"));
            var accountId = GetString(account, "id");

            _logger.LogDebug(
                "GoDutch account gelezen. AccountId: {AccountId}, IBAN: {Iban}.",
                accountId,
                accountIban);

            if (string.Equals(accountIban, normalizedIban, StringComparison.OrdinalIgnoreCase))
            {
                return accountId;
            }
        }

        return null;
    }

    private async Task<string> SendGetWithRetryAsync(
        GoDutchRequestContext context,
        string relativeUrl,
        string operationName,
        Guid? bankAccountId,
        string? iban,
        CancellationToken ct)
    {
        var maxAttempts = NormalizeMaxAttempts(_retryOptions.MaxAttempts);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var requestUri = new Uri(context.BaseUri, relativeUrl);

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Authorization = context.AuthorizationHeader;

            using var response = await _httpClient.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            _logger.LogDebug(
                "{OperationName} endpoint aangeroepen. Attempt: {Attempt}/{MaxAttempts}, StatusCode: {StatusCode}, ResponseLength: {ResponseLength}, Url: {Url}.",
                operationName,
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
                "{OperationName} tijdelijk mislukt. Retry volgt. Attempt: {Attempt}/{MaxAttempts}, BankAccountId: {BankAccountId}, Iban: {Iban}, StatusCode: {StatusCode}, DelayMs: {DelayMs}.",
                operationName,
                attempt,
                maxAttempts,
                bankAccountId,
                iban,
                (int)response.StatusCode,
                (int)delay.TotalMilliseconds);

            await Task.Delay(delay, ct);
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

    private sealed record GoDutchRequestContext(
        Uri BaseUri,
        AuthenticationHeaderValue AuthorizationHeader);

    private static int NormalizeMaxAttempts(int maxAttempts)
    {
        return maxAttempts < 1 ? 1 : maxAttempts;
    }

    private static string BuildDescription(TransactionApiModel transaction)
    {
        var description =
            transaction.Label
            ?? transaction.Counterparty
            ?? transaction.Reference
            ?? "GoDutch transactie";

        return string.IsNullOrWhiteSpace(description)
            ? "GoDutch transactie"
            : description.Trim();
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

    private static bool TryParseAmount(string? rawValue, out decimal amount)
    {
        amount = 0m;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        if (decimal.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out amount))
        {
            return true;
        }

        if (decimal.TryParse(rawValue, NumberStyles.Any, new CultureInfo("nl-NL"), out amount))
        {
            return true;
        }

        return false;
    }

    private static bool TryParseTransactionDate(string? rawValue, out DateTime bookingDate)
    {
        bookingDate = default;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        if (DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out bookingDate))
        {
            return true;
        }

        if (DateTime.TryParse(rawValue, new CultureInfo("nl-NL"), DateTimeStyles.AssumeLocal, out bookingDate))
        {
            return true;
        }

        return false;
    }

    private static bool IsBookedOrEmpty(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return true;
        }

        var normalized = status.Trim().ToUpperInvariant();
        return normalized is "BOOKED" or "BOOK" or "PROCESSED";
    }
}
