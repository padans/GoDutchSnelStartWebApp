using GoDutchSnelStartWebApp.Application.Configuration;
using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Dtos;
using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GoDutchSnelStartWebApp.Application.GoDutchTransactions.Services;

public sealed class GoDutchSnelStartImportService : IGoDutchSnelStartImportService
{
    private readonly IGoDutchSnelStartExportService _exportService;
    private readonly ISnelStartBankStatementImporter _importer;
    private readonly ILogger<GoDutchSnelStartImportService> _logger;
    private readonly SnelStartImportRetryOptions _retryOptions;

    public GoDutchSnelStartImportService(
        IGoDutchSnelStartExportService exportService,
        ISnelStartBankStatementImporter importer,
        ILogger<GoDutchSnelStartImportService> logger,
        IOptions<SnelStartImportRetryOptions> retryOptions)
    {
        _exportService = exportService;
        _importer = importer;
        _logger = logger;
        _retryOptions = retryOptions.Value;
    }

    public async Task<SnelStartImportResult> ImportAsync(
        Guid tenantId,
        Guid bankAccountId,
        string iban,
        DateTime from,
        DateTime to,
        SnelStartExportFormat format,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "SnelStart import gestart. TenantId: {TenantId}, BankAccountId: {BankAccountId}, Iban: {Iban}, periode: {FromDate:yyyy-MM-dd} t/m {ToDate:yyyy-MM-dd}, formaat: {Format}.",
            tenantId,
            bankAccountId,
            iban,
            from.Date,
            to.Date,
            format);

        var content = await _exportService.GenerateForSnelStartAsync(
            tenantId,
            bankAccountId,
            iban,
            from,
            to,
            format,
            cancellationToken);

        var request = new SnelStartImportRequest
        {
            TenantId = tenantId,
            BankAccountId = bankAccountId,
            Iban = iban,
            From = from.Date,
            To = to.Date,
            Format = format,
            FileName = BuildFileName(format),
            ContentType = GetContentType(format),
            Content = content
        };

        _logger.LogInformation(
            "SnelStart import request opgebouwd. FileName: {FileName}, ContentType: {ContentType}, ContentLength: {ContentLength}.",
            request.FileName,
            request.ContentType,
            request.Content?.Length ?? 0);

        return await ImportWithRetryAsync(request, cancellationToken);
    }

    private async Task<SnelStartImportResult> ImportWithRetryAsync(
        SnelStartImportRequest request,
        CancellationToken cancellationToken)
    {
        var maxAttempts = NormalizeMaxAttempts(_retryOptions.MaxAttempts);
        var retryCount = 0;

        if (!_retryOptions.Enabled || maxAttempts == 1)
        {
            var result = await _importer.ImportAsync(request, cancellationToken);
            result.RetryCount = retryCount;
            return result;
        }

        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                _logger.LogInformation(
                    "SnelStart upload poging {Attempt} van {MaxAttempts}. BankAccountId: {BankAccountId}, Iban: {Iban}, Bestand: {FileName}.",
                    attempt,
                    maxAttempts,
                    request.BankAccountId,
                    request.Iban,
                    request.FileName);

                var result = await _importer.ImportAsync(request, cancellationToken);

                if (result.Success && result.UploadSucceeded)
                {
                    result.RetryCount = retryCount;

                    if (retryCount > 0)
                    {
                        _logger.LogInformation(
                            "SnelStart upload geslaagd na retry. BankAccountId: {BankAccountId}, RetryCount: {RetryCount}.",
                            request.BankAccountId,
                            retryCount);
                    }

                    return result;
                }

                if (!ShouldRetry(result, attempt, maxAttempts))
                {
                    result.RetryCount = retryCount;
                    return result;
                }

                var delay = CalculateDelay(attempt);

                _logger.LogWarning(
                    "SnelStart upload niet geslaagd, retry volgt. Poging: {Attempt}, MaxAttempts: {MaxAttempts}, RetryCount: {RetryCount}, DelayMs: {DelayMs}, Success: {Success}, UploadSucceeded: {UploadSucceeded}, Message: {Message}.",
                    attempt,
                    maxAttempts,
                    retryCount + 1,
                    (int)delay.TotalMilliseconds,
                    result.Success,
                    result.UploadSucceeded,
                    result.Message);

                retryCount++;
                await Task.Delay(delay, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                lastException = ex;

                var delay = CalculateDelay(attempt);

                _logger.LogWarning(
                    ex,
                    "SnelStart upload poging {Attempt} mislukt. Retry volgt over {DelayMs} ms. BankAccountId: {BankAccountId}, Iban: {Iban}, RetryCount: {RetryCount}.",
                    attempt,
                    (int)delay.TotalMilliseconds,
                    request.BankAccountId,
                    request.Iban,
                    retryCount + 1);

                retryCount++;
                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "SnelStart upload definitief mislukt na {MaxAttempts} pogingen. BankAccountId: {BankAccountId}, Iban: {Iban}, RetryCount: {RetryCount}.",
                    maxAttempts,
                    request.BankAccountId,
                    request.Iban,
                    retryCount);

                throw;
            }
        }

        throw new InvalidOperationException(
            $"SnelStart upload is mislukt na {maxAttempts} pogingen.",
            lastException);
    }

    private static bool ShouldRetry(
        SnelStartImportResult result,
        int attempt,
        int maxAttempts)
    {
        if (attempt >= maxAttempts)
        {
            return false;
        }

        if (result.Success && result.UploadSucceeded)
        {
            return false;
        }

        if (result.IsDuplicateImport)
        {
            return false;
        }

        if (result.TransactionCount == 0)
        {
            return false;
        }

        return true;
    }

    private TimeSpan CalculateDelay(int attempt)
    {
        var initialDelayMs = _retryOptions.InitialDelayMilliseconds <= 0
            ? 1000
            : _retryOptions.InitialDelayMilliseconds;

        var multiplier = Math.Pow(2, attempt - 1);
        var delayMs = (int)(initialDelayMs * multiplier);

        return TimeSpan.FromMilliseconds(delayMs);
    }

    private static int NormalizeMaxAttempts(int maxAttempts)
    {
        return maxAttempts < 1 ? 1 : maxAttempts;
    }

    private static string BuildFileName(SnelStartExportFormat format)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

        return format switch
        {
            SnelStartExportFormat.Mt940 => $"mt940_{timestamp}.txt",
            SnelStartExportFormat.Camt053 => $"camt053_{timestamp}.xml",
            _ => $"export_{timestamp}.dat"
        };
    }

    private static string GetContentType(SnelStartExportFormat format)
    {
        return format switch
        {
            SnelStartExportFormat.Mt940 => "text/plain",
            SnelStartExportFormat.Camt053 => "application/xml",
            _ => "application/octet-stream"
        };
    }
}