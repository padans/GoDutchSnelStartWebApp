using GoDutchSnelStartWebApp.Application.Abstractions.Repositories.MyPos;
using GoDutchSnelStartWebApp.Application.Abstractions.Repositories.SnelStart;
using GoDutchSnelStartWebApp.Application.Abstractions.Security;
using GoDutchSnelStartWebApp.Application.MyPos.Dtos;
using GoDutchSnelStartWebApp.Application.MyPos.Interfaces;
using GoDutchSnelStartWebApp.Domain.Entities.MyPos;
using GoDutchSnelStartWebApp.Domain.Entities.SnelStart;
using GoDutchSnelStartWebApp.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;


namespace GoDutchSnelStartWebApp.Infrastructure.ExternalServices.MyPos;

public sealed class MyPosExportBatchExportService : IMyPosExportBatchExportService
{

    private readonly IMyPosExportBatchRepository _exportBatchRepository;
    private readonly IMyPosRawTransactionRepository _rawTransactionRepository;
    private readonly ITenantSnelStartConnectionRepository _tenantSnelStartConnectionRepository;
    private readonly ISecretEncryptionService _secretEncryptionService;
    private readonly ILogger<MyPosExportBatchExportService> _logger;
    private readonly HttpClient _httpClient;

    public MyPosExportBatchExportService(
     IMyPosExportBatchRepository exportBatchRepository,
     IMyPosRawTransactionRepository rawTransactionRepository,
     ITenantSnelStartConnectionRepository tenantSnelStartConnectionRepository,
     ISecretEncryptionService secretEncryptionService,
     ILogger<MyPosExportBatchExportService> logger,
     HttpClient httpClient)
    {
        _exportBatchRepository = exportBatchRepository;
        _rawTransactionRepository = rawTransactionRepository;
        _tenantSnelStartConnectionRepository = tenantSnelStartConnectionRepository;
        _secretEncryptionService = secretEncryptionService;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<MyPosExportBatchExportResultDto> ExportToSnelStartBankboekAsync(
        Guid tenantId,
        Guid batchId,
        CancellationToken cancellationToken = default)
    {
        var batch = await _exportBatchRepository.GetByIdAsync(batchId, cancellationToken);

        if (batch is null || batch.TenantId != tenantId)
        {
            throw new KeyNotFoundException("myPOS export batch not found.");
        }

        if (!batch.IsReadyForExport || batch.Status != MyPosExportBatchStatus.Ready)
        {
            throw new InvalidOperationException(
                $"myPOS export batch is not ready for export. Status: {batch.Status}. Message: {batch.ValidationMessage}");
        }

        if (batch.SnelStartBankDagboek is null)
        {
            throw new InvalidOperationException("SnelStart bank journal is missing on the export batch.");
        }

        if (batch.BookYear is null)
        {
            throw new InvalidOperationException("Book year is missing on the export batch.");
        }

        var readyLines = batch.Lines
            .OrderBy(x => x.TransactionType)
            .ToList();

        if (readyLines.Count == 0)
        {
            throw new InvalidOperationException("Export batch contains no lines.");
        }

        var invalidLine = readyLines.FirstOrDefault(x => !x.IsReadyForExport || x.SnelStartGrootboek is null);
        if (invalidLine is not null)
        {
            throw new InvalidOperationException(
                $"Export batch contains a line that is not ready for export. Type: {invalidLine.TransactionType}. Message: {invalidLine.MappingWarning}");
        }

        var snelStartConnection = await GetValidatedTenantConnectionAsync(tenantId, cancellationToken);
        var subscriptionKey = DecryptRequired(
            snelStartConnection.SubscriptionKeyEncrypted,
            "SnelStart subscription key could not be decrypted.");
        var clientKey = DecryptRequired(
            snelStartConnection.ClientKeyEncrypted,
            "SnelStart client key could not be decrypted.");

        var accessToken = await GetAccessTokenAsync(
            snelStartConnection.AuthUrl,
            clientKey,
            cancellationToken);

        var baseUrl = NormalizeApiBaseUrl(snelStartConnection.ApiBaseUrl);
        var endpoint = $"{baseUrl}/bankboekingen";

        var exportDate = batch.PeriodToLocalDate?.Date
            ?? batch.PeriodToUtc.Date;

        var exportedLineCount = 0;
        var failedLineCount = 0;
        var snelStartReferences = new List<string>();

        try
        {
            var exportLines = batch.Lines
                 .OrderBy(x => x.TransactionType)
                 .ToList();

            foreach (var line in exportLines)
            {
                if (!line.IsReadyForExport)
                {
                    failedLineCount++;

                    _logger.LogWarning(
                        "myPOS exportregel is niet klaar voor export. BatchId: {BatchId}, LineId: {LineId}, TransactionType: {TransactionType}, Warning: {Warning}.",
                        batch.Id,
                        line.Id,
                        line.TransactionType,
                        line.MappingWarning);
                }

                if (line.SnelStartGrootboek is null)
                {
                    failedLineCount++;

                    _logger.LogWarning(
                        "myPOS exportregel heeft geen SnelStart grootboek. BatchId: {BatchId}, LineId: {LineId}, TransactionType: {TransactionType}.",
                        batch.Id,
                        line.Id,
                        line.TransactionType);
                }
            }

            if (failedLineCount > 0)
            {
                var message =
                    $"Export niet uitgevoerd. {failedLineCount} regel(s) zijn niet klaar voor export. Er is niets naar SnelStart verzonden.";

                await _exportBatchRepository.MarkExportFailedAsync(
                    batch.Id,
                    message,
                    DateTime.UtcNow,
                    cancellationToken);

                return new MyPosExportBatchExportResultDto
                {
                    BatchId = batch.Id,
                    TenantId = tenantId,
                    Success = false,
                    LineCount = batch.Lines.Count,
                    ExportedLineCount = 0,
                    FailedLineCount = failedLineCount,
                    Message = message
                };
            }

            var payload = BuildBankboekingPayload(
                batch,
                exportLines,
                exportDate);

            var snelStartReference = await PostBankboekingAsync(
                endpoint,
                accessToken,
                subscriptionKey,
                payload,
                batch,
                cancellationToken);

            exportedLineCount = exportLines.Count;
            snelStartReferences.Add(snelStartReference);

            var reference = string.Join("; ", snelStartReferences);
            var exportedUtc = DateTime.UtcNow;

            var markedRawTransactionCount = await _rawTransactionRepository.MarkExportedForBatchAsync(
                tenantId,
                batch.Id,
                batch.PeriodFromUtc,
                batch.PeriodToUtc,
                exportedUtc,
                cancellationToken);

            _logger.LogInformation(
                "myPOS raw transacties gemarkeerd als geëxporteerd. TenantId: {TenantId}, BatchId: {BatchId}, FromUtc: {FromUtc}, ToUtc: {ToUtc}, MarkedCount: {MarkedCount}.",
                tenantId,
                batch.Id,
                batch.PeriodFromUtc,
                batch.PeriodToUtc,
                markedRawTransactionCount);

            await _exportBatchRepository.MarkExportedAsync(
                batch.Id,
                reference,
                exportedUtc,
                cancellationToken);

            _logger.LogInformation(
                "myPOS exportbatch succesvol geëxporteerd naar SnelStart-bankboek. BatchId: {BatchId}, TenantId: {TenantId}, ExportedLineCount: {ExportedLineCount}, Reference: {Reference}.",
                batch.Id,
                tenantId,
                exportedLineCount,
                reference);

            return new MyPosExportBatchExportResultDto
            {
                BatchId = batch.Id,
                TenantId = tenantId,
                Success = true,
                LineCount = batch.Lines.Count,
                ExportedLineCount = exportedLineCount,
                FailedLineCount = failedLineCount,
                SnelStartReference = reference,
                Message = "myPOS exportbatch is geëxporteerd naar SnelStart-bankboek."
            };
        }
        catch (Exception ex)
        {
            failedLineCount = batch.Lines.Count - exportedLineCount;

            _logger.LogError(
                ex,
                "myPOS exportbatch exporteren naar SnelStart-bankboek mislukt. BatchId: {BatchId}, TenantId: {TenantId}, ExportedLineCount: {ExportedLineCount}, FailedLineCount: {FailedLineCount}.",
                batch.Id,
                tenantId,
                exportedLineCount,
                failedLineCount);

            await _exportBatchRepository.MarkExportFailedAsync(
                batch.Id,
                ex.Message,
                DateTime.UtcNow,
                cancellationToken);

            throw;
        }
    }

    private async Task<string> PostBankboekingAsync(
     string endpoint,
     string accessToken,
     string subscriptionKey,
     object payload,
     MyPosExportBatch batch,
     CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "myPOS SnelStart bankboeking POST gestart voor volledige batch. BatchId: {BatchId}, LineCount: {LineCount}, Endpoint: {Endpoint}.",
            batch.Id,
            batch.Lines.Count,
            endpoint);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

        request.Content = JsonContent.Create(payload);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "SnelStart bankboeking POST mislukt voor volledige myPOS batch. BatchId: {BatchId}, StatusCode: {StatusCode}, Response: {Response}, Payload: {@Payload}.",
                batch.Id,
                (int)response.StatusCode,
                body,
                payload);

            throw new InvalidOperationException(
                $"SnelStart bankboeking POST mislukt voor myPOS batch met HTTP {(int)response.StatusCode}. Response: {body}");
        }

        var reference = TryReadSnelStartReference(body);

        _logger.LogInformation(
            "SnelStart bankboeking POST geslaagd voor volledige myPOS batch. BatchId: {BatchId}, StatusCode: {StatusCode}, Reference: {Reference}.",
            batch.Id,
            (int)response.StatusCode,
            reference);

        return reference;
    }


    private static object BuildBankboekingPayload(
     MyPosExportBatch batch,
     IReadOnlyList<MyPosExportBatchLine> lines,
     DateTime exportDate)
    {
        var description =
            $"myPOS {batch.PeriodFromLocalDate:dd-MM-yyyy} t/m {batch.PeriodToLocalDate:dd-MM-yyyy}";

        var grootboekBoekingsRegels = new List<object>();
        var btwBoekingsregels = new List<object>();

        decimal netBankAmount = 0m;

        foreach (var line in lines.OrderBy(x => x.TransactionType))
        {
            var originalSignedAmount = line.TotalAmount;
            var isReceived = originalSignedAmount >= 0;

            var grossAmount = Math.Abs(originalSignedAmount);

            var btwSoort = ResolveBtwSoort(line);

            var hasVat =
                !string.Equals(btwSoort, "Geen", StringComparison.OrdinalIgnoreCase) &&
                line.BtwPercentage.HasValue &&
                line.BtwPercentage.Value > 0;

            var netAmount = grossAmount;
            var vatAmount = 0m;
            var grossAmountForBank = grossAmount;

            if (hasVat)
            {
                if (string.Equals(line.BtwBerekening, "InclusiefBtw", StringComparison.OrdinalIgnoreCase))
                {
                    var factor = 1m + (line.BtwPercentage!.Value / 100m);

                    netAmount = RoundMoney(grossAmount / factor);
                    vatAmount = RoundMoney(grossAmount - netAmount);
                    grossAmountForBank = grossAmount;
                }
                else if (string.Equals(line.BtwBerekening, "ExclusiefBtw", StringComparison.OrdinalIgnoreCase))
                {
                    netAmount = grossAmount;
                    vatAmount = RoundMoney(netAmount * (line.BtwPercentage!.Value / 100m));
                    grossAmountForBank = RoundMoney(netAmount + vatAmount);
                }
            }

            netBankAmount += isReceived
                ? grossAmountForBank
                : -grossAmountForBank;

            var lineDescription =
                string.IsNullOrWhiteSpace(line.Description)
                    ? $"myPOS {line.TransactionType}"
                    : $"myPOS {line.TransactionType} {line.Description}";

            grootboekBoekingsRegels.Add(new
            {
                omschrijving = lineDescription,
                grootboek = new
                {
                    id = line.SnelStartGrootboek?.Id
                },
                debet = isReceived ? 0m : netAmount,
                credit = isReceived ? netAmount : 0m,
                btwSoort = btwSoort
            });

            if (hasVat && vatAmount != 0m)
            {
                btwBoekingsregels.Add(new
                {
                    debet = isReceived ? 0m : vatAmount,
                    credit = isReceived ? vatAmount : 0m,
                    type = isReceived ? "AfTeDragenBtwType" : "TeVorderenBtwType",
                    tarief = btwSoort
                });
            }
        }

        netBankAmount = RoundMoney(netBankAmount);

        return new
        {
            datum = exportDate.ToString("yyyy-MM-dd"),
            omschrijving = description,
            boekstuk = $"MYPOS-{batch.BookYear}-{batch.Id.ToString("N")[..8]}",
            bedragOntvangen = netBankAmount > 0m ? netBankAmount : 0m,
            bedragUitgegeven = netBankAmount < 0m ? Math.Abs(netBankAmount) : 0m,
            dagboek = new
            {
                id = batch.SnelStartBankDagboek?.Id
            },
            grootboekBoekingsRegels,
            btwBoekingsregels
        };
    }

    private static decimal RoundMoney(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    private static string ResolveBtwSoort(MyPosExportBatchLine line)
    {
        if (string.Equals(line.BtwBerekening, "Geen", StringComparison.OrdinalIgnoreCase))
        {
            return "Geen";
        }

        if (string.IsNullOrWhiteSpace(line.BtwSoort))
        {
            return "Geen";
        }

        return line.BtwSoort.Trim();
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

    private string DecryptRequired(
        string? encryptedValue,
        string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(encryptedValue))
        {
            throw new InvalidOperationException(errorMessage);
        }

        var decryptedValue = _secretEncryptionService.Decrypt(encryptedValue);

        if (string.IsNullOrWhiteSpace(decryptedValue))
        {
            throw new InvalidOperationException(errorMessage);
        }

        return decryptedValue.Trim();
    }

    private async Task<string> GetAccessTokenAsync(
        string authUrl,
        string clientKey,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "SnelStart access token ophalen gestart voor myPOS export. AuthUrl: {AuthUrl}, ClientKeyLength: {ClientKeyLength}.",
            authUrl,
            clientKey.Trim().Length);

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, authUrl)
        {
            Content = new StringContent(
                $"grant_type=clientkey&clientkey={Uri.EscapeDataString(clientKey.Trim())}",
                Encoding.UTF8,
                "application/x-www-form-urlencoded")
        };

        using var tokenResponse = await _httpClient.SendAsync(tokenRequest, cancellationToken);
        var tokenBody = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "SnelStart token ophalen mislukt voor myPOS export. StatusCode: {StatusCode}, ResponseLength: {ResponseLength}.",
                (int)tokenResponse.StatusCode,
                tokenBody.Length);

            throw new InvalidOperationException(
                $"SnelStart token request failed with HTTP {(int)tokenResponse.StatusCode}.");
        }

        using var tokenDoc = JsonDocument.Parse(tokenBody);

        if (!tokenDoc.RootElement.TryGetProperty("access_token", out var accessTokenElement))
        {
            throw new InvalidOperationException("SnelStart token response does not contain an access_token.");
        }

        var accessToken = accessTokenElement.GetString();

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("SnelStart returned an empty access_token.");
        }

        _logger.LogDebug("SnelStart access token succesvol opgehaald voor myPOS export.");

        return accessToken;
    }

    private static string NormalizeApiBaseUrl(string value)
    {
        var baseUrl = value.Trim().TrimEnd('/');

        return baseUrl.EndsWith("/v2", StringComparison.OrdinalIgnoreCase)
            ? baseUrl
            : baseUrl + "/v2";
    }

    private static string TryReadSnelStartReference(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return Guid.NewGuid().ToString();
        }

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(body);
            var root = document.RootElement;

            if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                if (root.TryGetProperty("id", out var idElement))
                {
                    return idElement.ToString();
                }

                if (root.TryGetProperty("uri", out var uriElement))
                {
                    return uriElement.ToString();
                }
            }
        }
        catch
        {
            // Response hoeft niet altijd JSON te zijn.
        }

        return Guid.NewGuid().ToString();
    }

    private static string Truncate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Length <= 1000
            ? value
            : value[..1000] + "...";
    }
}
