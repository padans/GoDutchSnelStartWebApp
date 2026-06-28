using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Dtos;
using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Interfaces;
using Microsoft.Extensions.Logging;

namespace GoDutchSnelStartWebApp.Application.GoDutchTransactions.Services;

public sealed class GoDutchWebhookImportService : IGoDutchWebhookImportService
{
    private readonly IGoDutchSnelStartAutoImportService _autoImportService;
    private readonly ILogger<GoDutchWebhookImportService> _logger;

    public GoDutchWebhookImportService(
        IGoDutchSnelStartAutoImportService autoImportService,
        ILogger<GoDutchWebhookImportService> logger)
    {
        _autoImportService = autoImportService;
        _logger = logger;
    }

    public async Task<SnelStartImportResult> ImportAsync(
        GoDutchWebhookImportRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.TenantId == Guid.Empty)
        {
            throw new InvalidOperationException("TenantId is required.");
        }

        if (request.BankAccountId == Guid.Empty)
        {
            throw new InvalidOperationException("BankAccountId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Iban))
        {
            throw new InvalidOperationException("Iban is required.");
        }

        _logger.LogInformation(
            "Webhook import gestart. TenantId: {TenantId}, BankAccountId: {BankAccountId}, Iban: {Iban}, Periode: {From:yyyy-MM-dd} t/m {To:yyyy-MM-dd}, Source: {Source}.",
            request.TenantId,
            request.BankAccountId,
            request.Iban,
            request.From,
            request.To,
            request.TriggerSource);

        try
        {
            var result = await _autoImportService.ImportAsync(
                request.TenantId,
                request.BankAccountId,
                request.Iban.Trim(),
                request.From,
                request.To,
                cancellationToken);

            _logger.LogInformation(
                "Webhook import afgerond. Success: {Success}, Message: {Message}.",
                result.Success,
                result.Message);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Webhook import fout. TenantId: {TenantId}, BankAccountId: {BankAccountId}.",
                request.TenantId,
                request.BankAccountId);

            return new SnelStartImportResult
            {
                Success = false,
                Message = "Webhook verwerking mislukt.",
                Details = ex.Message,
                DownloadSucceeded = false,
                UploadSucceeded = false
            };
        }
    }
}