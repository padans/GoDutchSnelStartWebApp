using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Application.Configuration;
using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Interfaces;
using GoDutchSnelStartWebApp.Domain.Entities;
using GoDutchSnelStartWebApp.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GoDutchSnelStartWebApp.Application.GoDutchTransactions.Services;

public sealed class GoDutchAutoSyncService : IGoDutchAutoSyncService
{
    private const int DefaultSyncIntervalMinutes = 15;
    private const int MinimumSyncIntervalMinutes = 15;
    private const int MaximumSyncIntervalMinutes = 24 * 60;

    private readonly IBankAccountSnelStartLinkRepository _linkRepository;
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly IGoDutchImportRunRepository _importRunRepository;
    private readonly IGoDutchSnelStartAutoImportService _autoImportService;
    private readonly ILogger<GoDutchAutoSyncService> _logger;
    private readonly GoDutchAutoSyncOptions _options;

    public GoDutchAutoSyncService(
        IBankAccountSnelStartLinkRepository linkRepository,
        IBankAccountRepository bankAccountRepository,
        IGoDutchImportRunRepository importRunRepository,
        IGoDutchSnelStartAutoImportService autoImportService,
        ILogger<GoDutchAutoSyncService> logger,
        IOptions<GoDutchAutoSyncOptions> options)
    {
        _linkRepository = linkRepository;
        _bankAccountRepository = bankAccountRepository;
        _importRunRepository = importRunRepository;
        _autoImportService = autoImportService;
        _logger = logger;
        _options = options.Value;
    }

    public async Task RunOnceAsync(CancellationToken cancellationToken = default)
    {
        var cycleStartedUtc = DateTime.UtcNow;
        var dueLinks = await _linkRepository.GetDueForAutoSyncAsync(cycleStartedUtc, cancellationToken);

        _logger.LogInformation(
            "GoDutch auto sync service gestart. Aantal koppelingen die nu aan de beurt zijn: {Count}. DueUtc: {DueUtc}.",
            dueLinks.Count,
            cycleStartedUtc);

        foreach (var link in dueLinks)
        {
            GoDutchImportRun? importRun = null;
            var attemptedAtUtc = DateTime.UtcNow;
            var nextRunUtc = CalculateNextRunUtc(attemptedAtUtc, link.SyncIntervalMinutes);

            try
            {
                var bankAccount = await _bankAccountRepository.GetByIdAsync(
                    link.BankAccountId,
                    cancellationToken);

                if (bankAccount is null)
                {
                    _logger.LogWarning(
                        "GoDutch auto sync overgeslagen. BankAccount niet gevonden voor LinkId: {LinkId}, BankAccountId: {BankAccountId}.",
                        link.Id,
                        link.BankAccountId);

                    await UpdateScheduleAsync(link, attemptedAtUtc, nextRunUtc, cancellationToken);
                    continue;
                }

                if (!bankAccount.IsActive)
                {
                    _logger.LogInformation(
                        "GoDutch auto sync overgeslagen. BankAccount is inactief. LinkId: {LinkId}, BankAccountId: {BankAccountId}, AccountName: {AccountName}.",
                        link.Id,
                        bankAccount.Id,
                        bankAccount.AccountName);

                    await UpdateScheduleAsync(link, attemptedAtUtc, nextRunUtc, cancellationToken);
                    continue;
                }

                if (!link.AutoSyncEnabled)
                {
                    _logger.LogInformation(
                        "GoDutch auto sync overgeslagen. Auto-sync is uitgeschakeld voor LinkId: {LinkId}, BankAccountId: {BankAccountId}.",
                        link.Id,
                        bankAccount.Id);

                    await UpdateScheduleAsync(link, attemptedAtUtc, null, cancellationToken);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(bankAccount.Iban))
                {
                    _logger.LogWarning(
                        "GoDutch auto sync overgeslagen. IBAN ontbreekt. LinkId: {LinkId}, BankAccountId: {BankAccountId}, AccountName: {AccountName}.",
                        link.Id,
                        bankAccount.Id,
                        bankAccount.AccountName);

                    await UpdateScheduleAsync(link, attemptedAtUtc, nextRunUtc, cancellationToken);
                    continue;
                }

                var nowUtc = DateTime.UtcNow;

                var lastCompletedRun =
                    await _importRunRepository.GetLastCompletedByBankAccountIdAsync(
                        bankAccount.Id,
                        cancellationToken);

                DateTime fromUtc;

                if (lastCompletedRun is not null)
                {
                    var overlap = TimeSpan.FromSeconds(_options.OverlapSeconds);

                    fromUtc = lastCompletedRun.PeriodToUtc.Subtract(overlap);

                    if (fromUtc >= nowUtc)
                    {
                        fromUtc = nowUtc.AddMinutes(-1);
                    }

                    _logger.LogInformation(
                        "AutoSync gebruikt laatste afgeronde run. BankAccountId: {BankAccountId}, LastPeriodToUtc: {LastPeriodToUtc}, FromUtc: {FromUtc}.",
                        bankAccount.Id,
                        lastCompletedRun.PeriodToUtc,
                        fromUtc);
                }
                else
                {
                    fromUtc = nowUtc.AddDays(-1);

                    _logger.LogInformation(
                        "AutoSync eerste run. BankAccountId: {BankAccountId}, FromUtc: {FromUtc}.",
                        bankAccount.Id,
                        fromUtc);
                }

                var toUtc = nowUtc;

                if (fromUtc >= toUtc)
                {
                    fromUtc = toUtc.AddMinutes(-5);
                }

                importRun = GoDutchImportRun.Start(
                    bankAccount.TenantId,
                    bankAccount.Id,
                    bankAccount.Iban,
                    fromUtc,
                    toUtc,
                    ImportRunTriggerSource.BackgroundWorker);

                await _importRunRepository.CreateAsync(importRun, cancellationToken);

                _logger.LogInformation(
                    "GoDutch auto sync import gestart. LinkId: {LinkId}, ImportRunId: {ImportRunId}, TenantId: {TenantId}, BankAccountId: {BankAccountId}, AccountName: {AccountName}, Iban: {Iban}, Periode: {FromUtc:yyyy-MM-dd HH:mm:ss} t/m {ToUtc:yyyy-MM-dd HH:mm:ss}, SyncIntervalMinutes: {SyncIntervalMinutes}.",
                    link.Id,
                    importRun.Id,
                    bankAccount.TenantId,
                    bankAccount.Id,
                    bankAccount.AccountName,
                    bankAccount.Iban,
                    fromUtc,
                    toUtc,
                    link.SyncIntervalMinutes);

                var result = await _autoImportService.ImportAsync(
                    bankAccount.TenantId,
                    bankAccount.Id,
                    bankAccount.Iban,
                    fromUtc,
                    toUtc,
                    cancellationToken);

                if (result.TransactionCount == 0 || result.IsDuplicateImport)
                    importRun.MarkSkipped(result.RetryCount, result.Message);
                else if (result.Success && result.UploadSucceeded)
                    importRun.MarkSucceeded(result.TransactionCount, result.RetryCount, result.Message);
                else
                    importRun.MarkFailed(result.RetryCount, result.Message);

                await _importRunRepository.UpdateAsync(importRun, cancellationToken);

                await UpdateScheduleAsync(
                    link,
                    importRun.CompletedUtc ?? DateTime.UtcNow,
                    CalculateNextRunUtc(importRun.CompletedUtc ?? DateTime.UtcNow, link.SyncIntervalMinutes),
                    cancellationToken);

                _logger.LogInformation(
                    "GoDutch auto sync import afgerond. LinkId: {LinkId}, ImportRunId: {ImportRunId}, Status: {Status}, Success: {Success}, UploadSucceeded: {UploadSucceeded}, TransactionCount: {TransactionCount}, RetryCount: {RetryCount}, Message: {Message}, NextRunUtc: {NextRunUtc}.",
                    link.Id,
                    importRun.Id,
                    importRun.Status,
                    result.Success,
                    result.UploadSucceeded,
                    result.TransactionCount,
                    result.RetryCount,
                    result.Message,
                    CalculateNextRunUtc(importRun.CompletedUtc ?? DateTime.UtcNow, link.SyncIntervalMinutes));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "GoDutch auto sync fout voor LinkId: {LinkId}, BankAccountId: {BankAccountId}.",
                    link.Id,
                    link.BankAccountId);

                if (importRun is not null)
                {
                    try
                    {
                        importRun.MarkFailed(message: ex.Message);

                        await _importRunRepository.UpdateAsync(importRun, cancellationToken);
                    }
                    catch (Exception updateEx)
                    {
                        _logger.LogError(
                            updateEx,
                            "Opslaan van failed GoDutch import run is mislukt voor LinkId: {LinkId}, ImportRunId: {ImportRunId}.",
                            link.Id,
                            importRun.Id);
                    }
                }
                else
                {
                    try
                    {
                        var bankAccount = await _bankAccountRepository.GetByIdAsync(
                            link.BankAccountId,
                            cancellationToken);

                        if (bankAccount is not null)
                        {
                            var now = DateTime.UtcNow;
                            var failedRun = GoDutchImportRun.Start(
                                bankAccount.TenantId,
                                bankAccount.Id,
                                bankAccount.Iban ?? string.Empty,
                                now.AddMinutes(-5),
                                now,
                                ImportRunTriggerSource.BackgroundWorker);
                            failedRun.MarkFailed(message: ex.Message);

                            await _importRunRepository.CreateAsync(failedRun, cancellationToken);
                        }
                    }
                    catch (Exception createEx)
                    {
                        _logger.LogError(
                            createEx,
                            "Aanmaken van failed GoDutch import run is mislukt voor LinkId: {LinkId}.",
                            link.Id);
                    }
                }

                try
                {
                    await UpdateScheduleAsync(link, DateTime.UtcNow, nextRunUtc, cancellationToken);
                }
                catch (Exception scheduleEx)
                {
                    _logger.LogError(
                        scheduleEx,
                        "Bijwerken van auto-sync planning is mislukt voor LinkId: {LinkId}.",
                        link.Id);
                }
            }
        }

        _logger.LogInformation("GoDutch auto sync service afgerond.");
    }

    private async Task UpdateScheduleAsync(
        BankAccountSnelStartLink link,
        DateTime? lastRunUtc,
        DateTime? nextRunUtc,
        CancellationToken cancellationToken)
    {
        await _linkRepository.UpdateAutoSyncScheduleAsync(
            link.Id,
            lastRunUtc,
            nextRunUtc,
            DateTime.UtcNow,
            cancellationToken);
    }

    private static DateTime CalculateNextRunUtc(DateTime fromUtc, int syncIntervalMinutes)
    {
        return fromUtc.AddMinutes(NormalizeSyncIntervalMinutes(syncIntervalMinutes));
    }

    private static int NormalizeSyncIntervalMinutes(int syncIntervalMinutes)
    {
        if (syncIntervalMinutes <= 0)
        {
            return DefaultSyncIntervalMinutes;
        }

        return Math.Clamp(
            syncIntervalMinutes,
            MinimumSyncIntervalMinutes,
            MaximumSyncIntervalMinutes);
    }
}
