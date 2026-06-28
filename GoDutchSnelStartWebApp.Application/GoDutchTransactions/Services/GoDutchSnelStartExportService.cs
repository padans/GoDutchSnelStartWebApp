using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Dtos;
using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Interfaces;
using Microsoft.Extensions.Logging;

namespace GoDutchSnelStartWebApp.Application.GoDutchTransactions.Services;

public sealed class GoDutchSnelStartExportService : IGoDutchSnelStartExportService
{
    private readonly IGoDutchMt940ExportService _mt940ExportService;
    private readonly IGoDutchCamt053ExportService _camt053ExportService;
    private readonly ILogger<GoDutchSnelStartExportService> _logger;

    public GoDutchSnelStartExportService(
        IGoDutchMt940ExportService mt940ExportService,
        IGoDutchCamt053ExportService camt053ExportService,
        ILogger<GoDutchSnelStartExportService> logger)
    {
        _mt940ExportService = mt940ExportService;
        _camt053ExportService = camt053ExportService;
        _logger = logger;
    }

    public async Task<string> GenerateForSnelStartAsync(
        Guid tenantId,
        Guid bankAccountId,
        string iban,
        DateTime from,
        DateTime to,
        SnelStartExportFormat format,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "SnelStart export generatie gestart. TenantId: {TenantId}, BankAccountId: {BankAccountId}, Iban: {Iban}, periode: {FromDate:yyyy-MM-dd} t/m {ToDate:yyyy-MM-dd}, formaat: {Format}.",
            tenantId,
            bankAccountId,
            iban,
            from.Date,
            to.Date,
            format);

        return format switch
        {
            SnelStartExportFormat.Mt940 => await _mt940ExportService.GenerateMt940Async(
                tenantId,
                bankAccountId,
                iban,
                from,
                to,
                cancellationToken),

            SnelStartExportFormat.Camt053 => await _camt053ExportService.GenerateCamt053Async(
                tenantId,
                bankAccountId,
                iban,
                from,
                to,
                cancellationToken),

            _ => throw new InvalidOperationException($"Unsupported SnelStart export format: {format}")
        };
    }
}