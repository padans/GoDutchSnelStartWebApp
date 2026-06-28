using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Application.Abstractions.Security;
using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Dtos;
using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Interfaces;
using GoDutchSnelStartWebApp.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace GoDutchSnelStartWebApp.Infrastructure.ExternalServices.SnelStart;

public sealed class SnelStartBankStatementImporter : ISnelStartBankStatementImporter
{
    private readonly HttpClient _httpClient;
    private readonly IBankAccountSettingsRepository _bankAccountSettingsRepository;
    private readonly IBankAccountSnelStartLinkRepository _bankAccountSnelStartLinkRepository;
    private readonly ISnelStartAdministrationRepository _snelStartAdministrationRepository;
    private readonly ISecretEncryptionService _encryptionService;
    private readonly ILogger<SnelStartBankStatementImporter> _logger;

    public SnelStartBankStatementImporter(
        HttpClient httpClient,
        IBankAccountSettingsRepository bankAccountSettingsRepository,
        IBankAccountSnelStartLinkRepository bankAccountSnelStartLinkRepository,
        ISnelStartAdministrationRepository snelStartAdministrationRepository,
        ISecretEncryptionService encryptionService,
        ILogger<SnelStartBankStatementImporter> logger)
    {
        _httpClient = httpClient;
        _bankAccountSettingsRepository = bankAccountSettingsRepository;
        _bankAccountSnelStartLinkRepository = bankAccountSnelStartLinkRepository;
        _snelStartAdministrationRepository = snelStartAdministrationRepository;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<SnelStartImportResult> ImportAsync(
        SnelStartImportRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var normalizedIban = request.Iban?.Trim() ?? string.Empty;
        var transactionCount = CountTransactions(request.Content, request.Format);

        var link = await _bankAccountSnelStartLinkRepository.GetByBankAccountIdAsync(
            request.BankAccountId,
            cancellationToken);

        if (link is null)
        {
            throw new InvalidOperationException("No SnelStart link found for this bank account.");
        }

        if (!link.IsActive)
        {
            throw new InvalidOperationException("The SnelStart link for this bank account is inactive.");
        }

        var administration = await _snelStartAdministrationRepository.GetByIdAsync(
            link.SnelStartAdministrationId,
            cancellationToken);

        if (administration is null)
        {
            throw new InvalidOperationException("SnelStartAdministration not found.");
        }

        if (!administration.IsActive)
        {
            throw new InvalidOperationException("The selected SnelStart administration is inactive.");
        }

        if (administration.TenantId != request.TenantId)
        {
            throw new InvalidOperationException("The selected SnelStart administration does not belong to the current tenant.");
        }

        if (transactionCount == 0)
        {
            _logger.LogInformation(
                "SnelStart import overgeslagen: geen transacties gevonden. TenantId: {TenantId}, BankAccountId: {BankAccountId}, Iban: {Iban}, AdministrationId: {AdministrationId}, AdministrationName: {AdministrationName}.",
                request.TenantId,
                request.BankAccountId,
                normalizedIban,
                administration.Id,
                administration.Name);

            return new SnelStartImportResult
            {
                Success = true,
                Message = "GoDutch download geslaagd. SnelStart upload niet uitgevoerd.",
                Details = $"Er zijn geen transacties gevonden voor IBAN {normalizedIban} over periode {request.From:yyyy-MM-dd} t/m {request.To:yyyy-MM-dd}. SnelStart upload is niet uitgevoerd voor administratie '{administration.Name}'.",
                TransactionCount = 0,
                Iban = normalizedIban,
                AdministrationName = administration.Name,
                DownloadSucceeded = true,
                UploadSucceeded = false,
                IsDuplicateImport = false,
                RawResponse = null
            };
        }

        var bankAccountSettings = await _bankAccountSettingsRepository.GetByBankAccountIdAsync(
            request.BankAccountId,
            cancellationToken);

        if (bankAccountSettings is null)
        {
            throw new InvalidOperationException("BankAccountSettings not found.");
        }

        if (string.IsNullOrWhiteSpace(bankAccountSettings.SnelStartAuthUrl))
        {
            throw new InvalidOperationException("SnelStartAuthUrl missing.");
        }

        if (string.IsNullOrWhiteSpace(bankAccountSettings.SnelStartApiBaseUrl))
        {
            throw new InvalidOperationException("SnelStartApiBaseUrl missing.");
        }

        if (string.IsNullOrWhiteSpace(bankAccountSettings.SnelStartSubscriptionKeyEncrypted))
        {
            throw new InvalidOperationException("SnelStartSubscriptionKey missing.");
        }

        if (string.IsNullOrWhiteSpace(bankAccountSettings.SnelStartClientKey))
        {
            throw new InvalidOperationException("SnelStartClientKey missing.");
        }

        var subscriptionKey = _encryptionService.Decrypt(
            bankAccountSettings.SnelStartSubscriptionKeyEncrypted);

        if (string.IsNullOrWhiteSpace(subscriptionKey))
        {
            throw new InvalidOperationException("SnelStart subscription key could not be decrypted.");
        }

        var clientKey = bankAccountSettings.SnelStartClientKey.Trim();

        if (string.IsNullOrWhiteSpace(clientKey))
        {
            throw new InvalidOperationException("SnelStartClientKey is empty.");
        }

        _logger.LogInformation(
            "SnelStart import gestart. TenantId: {TenantId}, BankAccountId: {BankAccountId}, Iban: {Iban}, Formaat: {Format}, FileName: {FileName}, AdministrationId: {AdministrationId}, AdministrationName: {AdministrationName}, TransactionCount: {TransactionCount}.",
            request.TenantId,
            request.BankAccountId,
            normalizedIban,
            request.Format,
            request.FileName,
            administration.Id,
            administration.Name,
            transactionCount);

        var token = await GetAccessTokenAsync(
            bankAccountSettings.SnelStartAuthUrl,
            clientKey,
            cancellationToken);

        var content = request.Content ?? string.Empty;

        if (!string.IsNullOrEmpty(content) && content[0] == '\uFEFF')
        {
            content = content[1..];
        }

        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));

        var payload = new[]
        {
            new
            {
                name = request.FileName,
                base64EncodedContent = base64
            }
        };

        var json = JsonSerializer.Serialize(payload);
        var endpoint = $"{bankAccountSettings.SnelStartApiBaseUrl.TrimEnd('/')}/bankafschriftbestanden";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        httpRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        httpRequest.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        _logger.LogInformation(
            "SnelStart import response ontvangen. StatusCode: {StatusCode}, BodyLength: {BodyLength}.",
            (int)response.StatusCode,
            responseBody?.Length ?? 0);

        var responseInfo = ParseSnelStartResponse(responseBody);

        var uploadSucceeded = response.IsSuccessStatusCode && responseInfo.AllItemsSuccessful;
        var duplicateImport = responseInfo.HasDuplicateMessage;

        var message = BuildMessage(
            downloadSucceeded: true,
            uploadSucceeded: uploadSucceeded,
            duplicateImport: duplicateImport);

        var details = BuildDetails(
            iban: normalizedIban,
            from: request.From,
            to: request.To,
            administrationName: administration.Name,
            transactionCount: transactionCount,
            uploadSucceeded: uploadSucceeded,
            duplicateImport: duplicateImport,
            responseInfo: responseInfo);

        return new SnelStartImportResult
        {
            Success = uploadSucceeded,
            Message = message,
            Details = details,
            TransactionCount = transactionCount,
            Iban = normalizedIban,
            AdministrationName = administration.Name,
            DownloadSucceeded = true,
            UploadSucceeded = uploadSucceeded,
            IsDuplicateImport = duplicateImport,
            RawResponse = responseBody
        };
    }

    private async Task<string> GetAccessTokenAsync(
        string authUrl,
        string clientKey,
        CancellationToken cancellationToken)
    {
        var normalizedAuthUrl = authUrl.Trim();
        var trimmedClientKey = clientKey.Trim();

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, normalizedAuthUrl)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "clientkey",
                ["clientkey"] = trimmedClientKey
            })
        };

        using var response = await _httpClient.SendAsync(tokenRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var tokenInfo = ReadAccessTokenInfo(body);

        // Deliberately do not log the token response body. It contains the bearer access token.
        _logger.LogInformation(
            "SnelStart token response ontvangen. StatusCode: {StatusCode}, BodyLength: {BodyLength}, HasAccessToken: {HasAccessToken}, AccessTokenLength: {AccessTokenLength}.",
            (int)response.StatusCode,
            body?.Length ?? 0,
            tokenInfo.HasAccessToken,
            tokenInfo.AccessTokenLength);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"SnelStart token request failed. HTTP {(int)response.StatusCode} ({response.StatusCode}). Response: {RedactSensitiveTokenResponse(body)}");
        }

        if (!tokenInfo.HasAccessToken)
        {
            throw new InvalidOperationException("SnelStart auth response does not contain an access_token.");
        }

        var token = tokenInfo.AccessToken;

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Empty access_token received from SnelStart.");
        }

        return token;
    }

    private static SnelStartTokenInfo ReadAccessTokenInfo(string? responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return new SnelStartTokenInfo(false, 0, null);
        }

        try
        {
            var json = responseBody.Trim();
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("access_token", out var tokenElement) ||
                tokenElement.ValueKind != JsonValueKind.String)
            {
                return new SnelStartTokenInfo(false, 0, null);
            }

            var token = tokenElement.GetString();

            return string.IsNullOrWhiteSpace(token)
                ? new SnelStartTokenInfo(false, 0, null)
                : new SnelStartTokenInfo(true, token.Length, token);
        }
        catch
        {
            return new SnelStartTokenInfo(false, 0, null);
        }
    }

    private static string RedactSensitiveTokenResponse(string? responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return string.Empty;
        }

        if (responseBody.Contains("access_token", StringComparison.OrdinalIgnoreCase))
        {
            return "SnelStart token response bevat access_token; body is bewust niet gelogd.";
        }

        return responseBody.Length <= 500
            ? responseBody
            : responseBody[..500] + "...";
    }

    private static int CountTransactions(string? content, SnelStartExportFormat format)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return 0;
        }

        return format switch
        {
            SnelStartExportFormat.Mt940 => CountOccurrences(content, ":61:"),
            SnelStartExportFormat.Camt053 => CountOccurrences(content, "<Ntry>"),
            _ => 0
        };
    }

    private static int CountOccurrences(string input, string value)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(value))
        {
            return 0;
        }

        var count = 0;
        var index = 0;

        while ((index = input.IndexOf(value, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static string BuildMessage(
        bool downloadSucceeded,
        bool uploadSucceeded,
        bool duplicateImport)
    {
        if (!downloadSucceeded)
        {
            return "GoDutch download niet geslaagd.";
        }

        if (uploadSucceeded)
        {
            return "GoDutch download geslaagd. SnelStart upload geslaagd.";
        }

        if (duplicateImport)
        {
            return "GoDutch download geslaagd. SnelStart upload overgeslagen (transacties al eerder ingelezen).";
        }

        return "GoDutch download geslaagd. SnelStart upload niet geslaagd.";
    }

    private static string BuildDetails(
        string? iban,
        DateTime from,
        DateTime to,
        string administrationName,
        int transactionCount,
        bool uploadSucceeded,
        bool duplicateImport,
        SnelStartResponseInfo responseInfo)
    {
        var baseText =
            $"{transactionCount} transacties opgehaald voor IBAN {iban?.Trim()} over periode {from:yyyy-MM-dd} t/m {to:yyyy-MM-dd}.";

        if (uploadSucceeded)
        {
            return $"{baseText} SnelStart upload geslaagd voor administratie '{administrationName}'.";
        }

        if (duplicateImport)
        {
            var duplicateText = string.IsNullOrWhiteSpace(responseInfo.FirstRelevantError)
                ? "SnelStart meldt dat de afschriftregels al eerder zijn ingelezen."
                : CleanHtml(responseInfo.FirstRelevantError);

            return $"{baseText} SnelStart upload niet geslaagd voor administratie '{administrationName}': {duplicateText}";
        }

        if (!string.IsNullOrWhiteSpace(responseInfo.FirstRelevantError))
        {
            return $"{baseText} SnelStart upload niet geslaagd voor administratie '{administrationName}': {CleanHtml(responseInfo.FirstRelevantError)}";
        }

        return $"{baseText} SnelStart upload niet geslaagd voor administratie '{administrationName}'.";
    }

    private static SnelStartResponseInfo ParseSnelStartResponse(string? responseBody)
    {
        var result = new SnelStartResponseInfo();

        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return result;
        }

        try
        {
            var json = responseBody.Trim();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return result;
            }

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                result.ItemCount++;

                if (!item.TryGetProperty("isSuccess", out var isSuccessElement) ||
                    isSuccessElement.ValueKind != JsonValueKind.True)
                {
                    result.AllItemsSuccessful = false;
                }

                if (item.TryGetProperty("errors", out var errorsElement) &&
                    errorsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var error in errorsElement.EnumerateArray())
                    {
                        var description = ExtractErrorDescription(error);

                        if (string.IsNullOrWhiteSpace(description))
                        {
                            continue;
                        }

                        result.Errors.Add(description);

                        if (string.IsNullOrWhiteSpace(result.FirstRelevantError))
                        {
                            result.FirstRelevantError = description;
                        }

                        if (IsDuplicateMessage(description))
                        {
                            result.HasDuplicateMessage = true;
                        }
                    }
                }
            }

            if (result.ItemCount == 0)
            {
                result.AllItemsSuccessful = false;
            }
        }
        catch
        {
            result.AllItemsSuccessful = false;
        }

        return result;
    }

    private static string? ExtractErrorDescription(JsonElement errorElement)
    {
        if (errorElement.ValueKind == JsonValueKind.String)
        {
            return errorElement.GetString();
        }

        if (errorElement.ValueKind == JsonValueKind.Object &&
            errorElement.TryGetProperty("description", out var descriptionElement) &&
            descriptionElement.ValueKind == JsonValueKind.String)
        {
            return descriptionElement.GetString();
        }

        return null;
    }

    private static bool IsDuplicateMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        return message.Contains("al eerder zijn ingelezen", StringComparison.OrdinalIgnoreCase)
               || message.Contains("bestaat al", StringComparison.OrdinalIgnoreCase)
               || message.Contains("already exists", StringComparison.OrdinalIgnoreCase)
               || message.Contains("overlap", StringComparison.OrdinalIgnoreCase);
    }

    private static string CleanHtml(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input ?? string.Empty;
        }

        return input
            .Replace("<b>", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("</b>", string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record SnelStartTokenInfo(
        bool HasAccessToken,
        int AccessTokenLength,
        string? AccessToken);

    private sealed class SnelStartResponseInfo
    {
        public bool AllItemsSuccessful { get; set; } = true;
        public bool HasDuplicateMessage { get; set; }
        public int ItemCount { get; set; }
        public string? FirstRelevantError { get; set; }
        public List<string> Errors { get; } = new();
    }
}