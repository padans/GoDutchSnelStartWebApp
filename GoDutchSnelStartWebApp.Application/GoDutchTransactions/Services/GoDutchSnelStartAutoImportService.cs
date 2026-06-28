using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Dtos;
using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Interfaces;
using Microsoft.Extensions.Logging;

namespace GoDutchSnelStartWebApp.Application.GoDutchTransactions.Services;

public sealed class GoDutchSnelStartAutoImportService : IGoDutchSnelStartAutoImportService
{
    private readonly IBankAccountSnelStartLinkRepository _linkRepository;
    private readonly IGoDutchSnelStartImportService _importService;
    private readonly ILogger<GoDutchSnelStartAutoImportService> _logger;

    public GoDutchSnelStartAutoImportService(
        IBankAccountSnelStartLinkRepository linkRepository,
        IGoDutchSnelStartImportService importService,
        ILogger<GoDutchSnelStartAutoImportService> logger)
    {
        _linkRepository = linkRepository;
        _importService = importService;
        _logger = logger;
    }

    public async Task<SnelStartImportResult> ImportAsync(
        Guid tenantId,
        Guid bankAccountId,
        string iban,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        var link = await _linkRepository.GetByBankAccountIdAsync(
            bankAccountId,
            cancellationToken);

        if (link is null)
        {
            throw new InvalidOperationException("No SnelStart link found for this bank account.");
        }

        if (!link.IsActive)
        {
            throw new InvalidOperationException("The SnelStart link for this bank account is inactive.");
        }

        _logger.LogInformation(
            "Automatische SnelStart import gestart. TenantId: {TenantId}, BankAccountId: {BankAccountId}, Iban: {Iban}, periode: {FromDate:yyyy-MM-dd} t/m {ToDate:yyyy-MM-dd}, format uit link: {Format}.",
            tenantId,
            bankAccountId,
            iban,
            from.Date,
            to.Date,
            link.ExportFormat);

        return await _importService.ImportAsync(
            tenantId,
            bankAccountId,
            iban,
            from,
            to,
            link.ExportFormat,
            cancellationToken);
    }
}