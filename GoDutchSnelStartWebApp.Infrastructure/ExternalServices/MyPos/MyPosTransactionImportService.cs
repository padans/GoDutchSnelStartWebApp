using GoDutchSnelStartWebApp.Application.Abstractions.Repositories.MyPos;
using GoDutchSnelStartWebApp.Application.Abstractions.Security;
using GoDutchSnelStartWebApp.Application.MyPos.Dtos;
using GoDutchSnelStartWebApp.Application.MyPos.Interfaces;
using GoDutchSnelStartWebApp.Domain.Entities.MyPos;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GoDutchSnelStartWebApp.Infrastructure.ExternalServices.MyPos;

public sealed class MyPosTransactionImportService : IMyPosTransactionImportService
{
    private readonly HttpClient _httpClient;
    private readonly ITenantMyPosConnectionRepository _connectionRepository;
    private readonly IMyPosRawTransactionRepository _rawTransactionRepository;
    private readonly ISecretEncryptionService _secretEncryptionService;
    private readonly ILogger<MyPosTransactionImportService> _logger;

    public MyPosTransactionImportService(
        HttpClient httpClient,
        ITenantMyPosConnectionRepository connectionRepository,
        IMyPosRawTransactionRepository rawTransactionRepository,
        ISecretEncryptionService secretEncryptionService,
        ILogger<MyPosTransactionImportService> logger)
    {
        _httpClient = httpClient;
        _connectionRepository = connectionRepository;
        _rawTransactionRepository = rawTransactionRepository;
        _secretEncryptionService = secretEncryptionService;
        _logger = logger;
    }

    private async Task<IReadOnlyList<JsonElement>> FetchTransactionsAsync(
    string baseUrl,
    string accessToken,
    string apiKey,
    string requestId,
    DateTime fromUtc,
    DateTime toUtc,
    CancellationToken cancellationToken)
    {
        const int limit = 100;

        var transactions = new List<JsonElement>();

        var totalRecords = await FetchTransactionTotalCountAsync(
            baseUrl,
            accessToken,
            apiKey,
            requestId,
            fromUtc,
            toUtc,
            cancellationToken);

        _logger.LogInformation(
            "myPOS totaal aantal transacties opgehaald. TotalRecords: {TotalRecords}, FromUtc: {FromUtc}, ToUtc: {ToUtc}.",
            totalRecords,
            fromUtc,
            toUtc);

        if (totalRecords <= 0)
        {
            return transactions;
        }

        var totalPages = (int)Math.Ceiling((double)totalRecords / limit);
        var processedRecords = 0;

        for (var page = 1; page <= totalPages; page++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = BuildTransactionsEndpoint(
                baseUrl,
                fromUtc,
                toUtc,
                page,
                limit);

            _logger.LogInformation(
                "myPOS transacties ophalen. Page: {Page}/{TotalPages}, Limit: {Limit}.",
                page,
                totalPages,
                limit);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            AddMyPosHeaders(request, accessToken, apiKey, requestId);

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "myPOS transacties ophalen mislukt. Page: {Page}, StatusCode: {StatusCode}, ResponsePreview: {ResponsePreview}.",
                    page,
                    (int)response.StatusCode,
                    Truncate(json));

                response.EnsureSuccessStatusCode();
            }

            var pageTransactions = ParseTransactionItems(json);

            if (pageTransactions.Count > 0)
            {
                transactions.AddRange(pageTransactions);
                processedRecords += pageTransactions.Count;
            }

            _logger.LogInformation(
                "myPOS pagina verwerkt. Page: {Page}/{TotalPages}, PageCount: {PageCount}, Processed: {Processed}/{TotalRecords}.",
                page,
                totalPages,
                pageTransactions.Count,
                processedRecords,
                totalRecords);
        }

        _logger.LogInformation(
            "myPOS transacties ophalen afgerond. Fetched: {FetchedCount}.",
            transactions.Count);

        return transactions;
    }

    public async Task<MyPosTransactionImportResultDto> FetchAndStoreAsync(
        Guid tenantId,
        Guid tenantMyPosConnectionId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty) throw new InvalidOperationException("TenantId is required.");
        if (tenantMyPosConnectionId == Guid.Empty) throw new InvalidOperationException("TenantMyPosConnectionId is required.");
        if (toUtc <= fromUtc) throw new InvalidOperationException("ToUtc must be greater than FromUtc.");

        var connection = await _connectionRepository.GetByIdAsync(tenantMyPosConnectionId, cancellationToken);
        if (connection is null || connection.TenantId != tenantId) throw new KeyNotFoundException("myPOS connection not found.");
        if (!connection.IsActive) throw new InvalidOperationException("myPOS connection is not active.");

        var clientSecret = _secretEncryptionService.Decrypt(connection.ClientSecretEncrypted);
        var apiKey = _secretEncryptionService.Decrypt(connection.ApiKeyEncrypted);

        if (string.IsNullOrWhiteSpace(clientSecret)) throw new InvalidOperationException("myPOS client secret is missing.");
        if (string.IsNullOrWhiteSpace(apiKey)) throw new InvalidOperationException("myPOS API key is missing.");

        var accessToken = await GetAccessTokenAsync(connection.AuthUrl, connection.ClientId, clientSecret, cancellationToken);
        var requestId = Guid.NewGuid().ToString("N");
        var importedUtc = DateTime.UtcNow;

        var rawItems = await FetchTransactionsAsync(
            connection.TransactionsApiBaseUrl,
            accessToken,
            apiKey,
            requestId,
            fromUtc,
            toUtc,
            cancellationToken);

        var transactions = rawItems
            .Select(item => TryMapTransaction(tenantId, connection.Id, requestId, item, importedUtc))
            .Where(x => x is not null)
            .Select(x => x!)
            .ToList();

        var duplicateKeys = transactions
    .GroupBy(x => new
    {
        x.TenantId,
        x.TenantMyPosConnectionId,
        x.MyPosTransactionId
    })
    .Where(x => x.Count() > 1)
    .Select(x => x.Key)
    .ToHashSet();

        if (duplicateKeys.Count > 0)
        {
            var originalCount = transactions.Count;

            _logger.LogWarning(
                "myPOS import bevat dubbele transacties binnen dezelfde download. TenantId: {TenantId}, ConnectionId: {ConnectionId}, DuplicateGroups: {DuplicateGroups}.",
                tenantId,
                connection.Id,
                duplicateKeys.Count);

            transactions = transactions
                .GroupBy(x => new
                {
                    x.TenantId,
                    x.TenantMyPosConnectionId,
                    x.MyPosTransactionId
                })
                .Select(x => x.OrderByDescending(t => t.ImportedUtc).First())
                .ToList();

            _logger.LogWarning(
                "myPOS dubbele transacties uit importset verwijderd. TenantId: {TenantId}, ConnectionId: {ConnectionId}, OriginalCount: {OriginalCount}, DeduplicatedCount: {DeduplicatedCount}, RemovedCount: {RemovedCount}.",
                tenantId,
                connection.Id,
                originalCount,
                transactions.Count,
                originalCount - transactions.Count);
        }

          var upsertResult = transactions.Count == 0
             ? new MyPosRawTransactionUpsertResultDto()
             : await _rawTransactionRepository.UpsertRangeAsync(transactions, cancellationToken);

        _logger.LogInformation(
            "myPOS import afgerond. TenantId: {TenantId}, ConnectionId: {ConnectionId}, Fetched: {Fetched}, Mapped: {Mapped}, Inserted: {Inserted}, Updated: {Updated}, Skipped: {Skipped}, DuplicateInImport: {DuplicateInImport}.",
            tenantId,
            connection.Id,
            rawItems.Count,
            transactions.Count,
            upsertResult.InsertedCount,
            upsertResult.UpdatedCount,
            upsertResult.SkippedCount,
            upsertResult.DuplicateInImportCount);

        return new MyPosTransactionImportResultDto
        {
            TenantId = tenantId,
            TenantMyPosConnectionId = connection.Id,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            FetchedCount = rawItems.Count,
            InsertedOrUpdatedCount = upsertResult.DatabaseOperationCount,
            InsertedCount = upsertResult.InsertedCount,
            UpdatedCount = upsertResult.UpdatedCount,
            SkippedCount = upsertResult.SkippedCount,
            DuplicateInImportCount = upsertResult.DuplicateInImportCount,
            Message = rawItems.Count == 0
         ? "myPOS download geslaagd, geen transacties gevonden."
         : "myPOS download geslaagd en raw transacties verwerkt."
        };
    }

    public async Task<IReadOnlyList<MyPosRawTransactionDto>> GetRawTransactionsAsync(
        Guid tenantId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var transactions = await _rawTransactionRepository.GetByTenantAsync(tenantId, fromUtc, toUtc, cancellationToken);
        return transactions.Select(MapToDto).ToList();
    }

    private async Task<string> GetAccessTokenAsync(
     string authUrl,
     string clientId,
     string clientSecret,
     CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(authUrl))
        {
            throw new InvalidOperationException("myPOS AuthUrl is missing.");
        }

        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException("myPOS ClientId is missing.");
        }

        if (string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException("myPOS ClientSecret is missing.");
        }

        _logger.LogDebug(
            "myPOS access token ophalen gestart. AuthUrl: {AuthUrl}, ClientIdLength: {ClientIdLength}, ClientSecretLength: {ClientSecretLength}.",
            authUrl,
            clientId.Trim().Length,
            clientSecret.Trim().Length);

        var credentials = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{clientId.Trim()}:{clientSecret.Trim()}"));

        using var request = new HttpRequestMessage(HttpMethod.Post, authUrl)
        {
            Content = new StringContent(
                "grant_type=client_credentials",
                Encoding.UTF8,
                "application/x-www-form-urlencoded")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "myPOS token ophalen mislukt. StatusCode: {StatusCode}, ResponseLength: {ResponseLength}, ResponsePreview: {ResponsePreview}.",
                (int)response.StatusCode,
                body.Length,
                Truncate(body));

            throw new InvalidOperationException(
                $"myPOS token ophalen mislukt met HTTP {(int)response.StatusCode}.");
        }

        using var doc = JsonDocument.Parse(body);

        if (!doc.RootElement.TryGetProperty("access_token", out var accessTokenElement))
        {
            _logger.LogWarning(
                "myPOS token response bevat geen access_token. ResponseLength: {ResponseLength}, ResponsePreview: {ResponsePreview}.",
                body.Length,
                Truncate(body));

            throw new InvalidOperationException("myPOS token response does not contain an access_token.");
        }

        var accessToken = accessTokenElement.GetString();

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("myPOS returned an empty access_token.");
        }

        _logger.LogDebug("myPOS access token succesvol opgehaald.");

        return accessToken;
    }

    private async Task<int> FetchTransactionTotalCountAsync(
     string baseUrl,
     string accessToken,
     string apiKey,
     string requestId,
     DateTime fromUtc,
     DateTime toUtc,
     CancellationToken cancellationToken)
    {
        const int maxAttempts = 4;

        var url = BuildTransactionsEndpoint(
            baseUrl,
            fromUtc,
            toUtc,
            page: 1,
            limit: 1);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            AddMyPosHeaders(request, accessToken, apiKey, requestId);

            _logger.LogInformation(
                "myPOS totaal aantal transacties ophalen via limit=1. Attempt: {Attempt}/{MaxAttempts}, FromUtc: {FromUtc}, ToUtc: {ToUtc}.",
                attempt,
                maxAttempts,
                fromUtc,
                toUtc);

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var totalRecords = ReadTotalCountFromMyPosResponse(json);

                _logger.LogInformation(
                    "myPOS totaal aantal transacties opgehaald. TotalRecords: {TotalRecords}, FromUtc: {FromUtc}, ToUtc: {ToUtc}.",
                    totalRecords,
                    fromUtc,
                    toUtc);

                return totalRecords;
            }

            if (IsTransientStatusCode(response.StatusCode) && attempt < maxAttempts)
            {
                var delay = TimeSpan.FromSeconds(attempt * 2);

                _logger.LogWarning(
                    "myPOS totaal ophalen tijdelijk mislukt. Attempt: {Attempt}/{MaxAttempts}, StatusCode: {StatusCode}, Wacht: {DelaySeconds}s, ResponsePreview: {ResponsePreview}.",
                    attempt,
                    maxAttempts,
                    (int)response.StatusCode,
                    delay.TotalSeconds,
                    Truncate(json));

                await Task.Delay(delay, cancellationToken);
                continue;
            }

            _logger.LogWarning(
                "myPOS totaal ophalen mislukt. StatusCode: {StatusCode}, ResponsePreview: {ResponsePreview}.",
                (int)response.StatusCode,
                Truncate(json));

            throw new HttpRequestException(
                $"myPOS total retrieval failed with HTTP {(int)response.StatusCode}: {Truncate(json)}");
        }

        throw new HttpRequestException("myPOS total retrieval failed after multiple attempts.");
    }
    private static int ReadTotalCountFromMyPosResponse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return 0;
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        foreach (var propertyName in new[]
                 {
                 "total",
                 "total_count",
                 "totalCount",
                 "total_records",
                 "totalRecords",
                 "records_total",
                 "recordsTotal",
                 "count"
             })
        {
            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty(propertyName, out var totalElement) &&
                totalElement.ValueKind == JsonValueKind.Number &&
                totalElement.TryGetInt32(out var total))
            {
                return total;
            }
        }

        var itemsArray = GetItemsArray(root);

        if (itemsArray is not null &&
            itemsArray.Value.ValueKind == JsonValueKind.Array)
        {
            return itemsArray.Value.GetArrayLength();
        }

        return 0;
    }

    private static string BuildTransactionsEndpoint(
     string baseUrl,
     DateTime fromUtc,
     DateTime toUtc,
     int page,
     int limit)
    {
        var from = DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc)
            .ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

        var to = DateTime.SpecifyKind(toUtc, DateTimeKind.Utc)
            .ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

        return $"{baseUrl}/transactions" +
               $"?from_date={Uri.EscapeDataString(from)}" +
               $"&to_date={Uri.EscapeDataString(to)}" +
               $"&limit={limit}" +
               $"&page={page}";
    }

    private static JsonElement? GetItemsArray(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array) return root;
        foreach (var propertyName in new[] { "data", "items", "transactions", "value", "result" })
        {
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Array)
            {
                return property;
            }
        }
        return null;
    }

    private static MyPosRawTransaction? TryMapTransaction(Guid tenantId, Guid connectionId, string requestId, JsonElement item, DateTime importedUtc)
    {
        var myPosTransactionId = TryGetLong(item, "id");
        if (myPosTransactionId is null || myPosTransactionId.Value <= 0) return null;

        var transactionType = TryGetString(item, "transaction_type") ?? TryGetString(item, "transactionType");
        if (string.IsNullOrWhiteSpace(transactionType)) return null;

        var amount = TryGetDecimal(item, "transaction_amount") ?? TryGetDecimal(item, "transactionAmount") ?? TryGetDecimal(item, "amount") ?? 0m;
        var transactionUtc = TryGetDateTime(item, "date") ?? TryGetDateTime(item, "transaction_date") ?? TryGetDateTime(item, "created_at") ?? importedUtc;

        return new MyPosRawTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TenantMyPosConnectionId = connectionId,
            MyPosTransactionId = myPosTransactionId.Value,
            AccountNumber = TryGetString(item, "account_number"),
            Ruid = TryGetString(item, "ruid"),
            ReferenceNumberType = TryGetInt(item, "reference_number_type"),
            BillingDescriptor = TryGetString(item, "billing_descriptor"),
            PanMasked = TryGetString(item, "pan_masked"),
            Description = TryGetString(item, "description"),
            PaymentReference = TryGetString(item, "payment_reference"),
            TransactionType = transactionType.Trim(),
            TransactionCurrency = TryGetString(item, "transaction_currency") ?? TryGetString(item, "currency"),
            TransactionAmount = amount,
            OriginalCurrency = TryGetString(item, "original_currency"),
            OriginalAmount = TryGetDecimal(item, "original_amount"),
            TransactionUtc = DateTime.SpecifyKind(transactionUtc, DateTimeKind.Utc),
            Sign = TryGetString(item, "sign"),
            ReferenceNumber = TryGetString(item, "reference_number"),
            TerminalId = TryGetString(item, "terminal_id"),
            SerialNumber = TryGetString(item, "serial_number"),
            RequestId = requestId,
            RawJson = item.GetRawText(),
            ImportedUtc = importedUtc,
            IsExported = false,
            ExportBatchId = null
        };
    }

    private static MyPosRawTransactionDto MapToDto(MyPosRawTransaction transaction) => new()
    {
        Id = transaction.Id,
        TenantId = transaction.TenantId,
        TenantMyPosConnectionId = transaction.TenantMyPosConnectionId,
        MyPosTransactionId = transaction.MyPosTransactionId,
        AccountNumber = transaction.AccountNumber,
        Ruid = transaction.Ruid,
        BillingDescriptor = transaction.BillingDescriptor,
        PanMasked = transaction.PanMasked,
        Description = transaction.Description,
        PaymentReference = transaction.PaymentReference,
        TransactionType = transaction.TransactionType,
        TransactionCurrency = transaction.TransactionCurrency,
        TransactionAmount = transaction.TransactionAmount,
        OriginalCurrency = transaction.OriginalCurrency,
        OriginalAmount = transaction.OriginalAmount,
        TransactionUtc = transaction.TransactionUtc,
        Sign = transaction.Sign,
        ReferenceNumber = transaction.ReferenceNumber,
        TerminalId = transaction.TerminalId,
        SerialNumber = transaction.SerialNumber,
        RequestId = transaction.RequestId,
        ImportedUtc = transaction.ImportedUtc,
        IsExported = transaction.IsExported,
        ExportBatchId = transaction.ExportBatchId
    };

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property)) return null;
        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private static long? TryGetLong(JsonElement element, string propertyName)
    {
        var raw = TryGetString(element, propertyName);
        return long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;
    }

    private static int? TryGetInt(JsonElement element, string propertyName)
    {
        var raw = TryGetString(element, propertyName);
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;
    }

    private static decimal? TryGetDecimal(JsonElement element, string propertyName)
    {
        var raw = TryGetString(element, propertyName);
        return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : null;
    }

    private static DateTime? TryGetDateTime(JsonElement element, string propertyName)
    {
        var raw = TryGetString(element, propertyName);
        return DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto) ? dto.UtcDateTime : null;
    }

    private static string Truncate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return value.Length <= 500 ? value : value[..500] + "...";
    }


    private static void AddMyPosHeaders(
    HttpRequestMessage request,
    string accessToken,
    string apiKey,
    string requestId)
    {
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");
        request.Headers.TryAddWithoutValidation("API-Key", apiKey);
        request.Headers.TryAddWithoutValidation("X-Request-ID", requestId);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
    }

    private static IReadOnlyList<JsonElement> ParseTransactionItems(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var itemsArray = GetItemsArray(root);

        if (itemsArray is null || itemsArray.Value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var result = new List<JsonElement>();

        foreach (var item in itemsArray.Value.EnumerateArray())
        {
            result.Add(item.Clone());
        }

        return result;
    }

    private static bool IsTransientStatusCode(System.Net.HttpStatusCode statusCode)
    {
        return statusCode == System.Net.HttpStatusCode.TooManyRequests ||
               statusCode == System.Net.HttpStatusCode.BadGateway ||
               statusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
               statusCode == System.Net.HttpStatusCode.GatewayTimeout;
    }

    private static string CreatePreview(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        const int maxLength = 500;

        return value.Length <= maxLength
            ? value
            : value[..maxLength];
    }
}
