using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Application.BankAccounts.Dtos;
using GoDutchSnelStartWebApp.Application.BankAccounts.Interfaces;
using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Dtos;
using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Interfaces;
using GoDutchSnelStartWebApp.Domain.Entities;
using GoDutchSnelStartWebApp.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace GoDutchSnelStartWebApp.Application.BankAccounts.Services;

public sealed class BankAccountResyncService : IBankAccountResyncService
{
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly IGoDutchImportRunRepository _importRunRepository;
    private readonly IGoDutchTransactionService _goDutchTransactionService;
    private readonly IGoDutchSnelStartAutoImportService _autoImportService;
    private readonly ILogger<BankAccountResyncService> _logger;

    public BankAccountResyncService(
        IBankAccountRepository bankAccountRepository,
        IGoDutchImportRunRepository importRunRepository,
        IGoDutchTransactionService goDutchTransactionService,
        IGoDutchSnelStartAutoImportService autoImportService,
        ILogger<BankAccountResyncService> logger)
    {
        _bankAccountRepository = bankAccountRepository;
        _importRunRepository = importRunRepository;
        _goDutchTransactionService = goDutchTransactionService;
        _autoImportService = autoImportService;
        _logger = logger;
    }

    public async Task<BankAccountResyncResultDto> ForceResyncFromDateAsync(
        Guid tenantId,
        Guid bankAccountId,
        DateTime fromUtc,
        CancellationToken cancellationToken = default)
    {
        var bankAccount = await _bankAccountRepository.GetByIdAsync(
            bankAccountId,
            cancellationToken);

        if (bankAccount is null || bankAccount.TenantId != tenantId)
        {
            throw new InvalidOperationException("Bank account not found.");
        }

        if (string.IsNullOrWhiteSpace(bankAccount.Iban))
        {
            throw new InvalidOperationException("Bank account has no IBAN configured.");
        }

        var nowUtc = DateTime.UtcNow;
        var normalizedFromUtc = EnsureUtc(fromUtc);

        if (normalizedFromUtc >= nowUtc)
        {
            normalizedFromUtc = nowUtc.AddMinutes(-5);
        }

        var importRun = GoDutchImportRun.Start(
            tenantId,
            bankAccountId,
            bankAccount.Iban,
            normalizedFromUtc,
            nowUtc,
            ImportRunTriggerSource.ManualResync);

        await _importRunRepository.CreateAsync(importRun, cancellationToken);

        var result = new BankAccountResyncResultDto
        {
            BankAccountId = bankAccountId,
            Iban = bankAccount.Iban,
            PeriodFromUtc = normalizedFromUtc,
            PeriodToUtc = nowUtc,
            ImportRunId = importRun.Id,
            Status = nameof(ImportRunStatus.Started),
            Message = "Handmatige resync gestart."
        };

        IReadOnlyList<BankTransactionDto> transactions;

        try
        {
            transactions = await _goDutchTransactionService.GetTransactionsAsync(
                tenantId,
                bankAccountId,
                bankAccount.Iban,
                normalizedFromUtc,
                nowUtc,
                cancellationToken);

            FillGoDutchSummary(result, transactions, nowUtc);
            result.DownloadSucceeded = true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            result.Status = nameof(ImportRunStatus.Failed);
            result.Message = $"GoDutch download mislukt: {ex.Message}";
            result.Details = ex.ToString();
            result.DownloadSucceeded = false;
            result.SnelStartUploadAttempted = false;
            result.UploadSucceeded = false;

            await CompleteImportRunAsync(importRun, result, cancellationToken);

            _logger.LogError(
                ex,
                "Handmatige resync mislukt tijdens GoDutch download. ImportRunId: {ImportRunId}, TenantId: {TenantId}, BankAccountId: {BankAccountId}.",
                importRun.Id,
                tenantId,
                bankAccountId);

            return result;
        }

        if (result.PeriodTransactionCount == 0)
        {
            result.Status = nameof(ImportRunStatus.Skipped);
            result.Message = "GoDutch download geslaagd, maar er zijn geen transacties binnen de gevraagde periode gevonden.";
            result.SnelStartUploadAttempted = false;
            result.UploadSucceeded = false;
            result.IsDuplicateImport = false;
            result.NotExportedTransactionCount = 0;

            await CompleteImportRunAsync(importRun, result, cancellationToken);

            _logger.LogInformation(
                "Handmatige resync overgeslagen: geen transacties binnen periode. ImportRunId: {ImportRunId}, TenantId: {TenantId}, BankAccountId: {BankAccountId}, OpeningBalance: {OpeningBalance}, OpeningBalanceDate: {OpeningBalanceDate}, ClosingBalance: {ClosingBalance}, ClosingBalanceDate: {ClosingBalanceDate}.",
                importRun.Id,
                tenantId,
                bankAccountId,
                result.OpeningBalance,
                result.OpeningBalanceDate,
                result.ClosingBalance,
                result.ClosingBalanceDate);

            return result;
        }

        try
        {
            result.SnelStartUploadAttempted = true;

            var importResult = await _autoImportService.ImportAsync(
                tenantId,
                bankAccountId,
                bankAccount.Iban,
                normalizedFromUtc,
                nowUtc,
                cancellationToken);

            result.ExportTransactionCount = importResult.TransactionCount;
            result.NotExportedTransactionCount = Math.Max(0, result.PeriodTransactionCount - importResult.TransactionCount);
            result.DownloadSucceeded = importResult.DownloadSucceeded || result.DownloadSucceeded;
            result.UploadSucceeded = importResult.UploadSucceeded;
            result.IsDuplicateImport = importResult.IsDuplicateImport;
            result.RetryCount = importResult.RetryCount;
            result.Details = importResult.Details;
            result.Status = DetermineStatus(importResult);
            result.Message = BuildMessage(importResult, result);

            await CompleteImportRunAsync(importRun, result, cancellationToken);

            _logger.LogInformation(
                "Handmatige resync afgerond. ImportRunId: {ImportRunId}, TenantId: {TenantId}, BankAccountId: {BankAccountId}, Status: {Status}, GoDutchTransactions: {GoDutchTransactionCount}, PeriodTransactions: {PeriodTransactionCount}, ExportTransactions: {ExportTransactionCount}, OpeningBalance: {OpeningBalance}, OpeningBalanceDate: {OpeningBalanceDate}, ClosingBalance: {ClosingBalance}, ClosingBalanceDate: {ClosingBalanceDate}.",
                importRun.Id,
                tenantId,
                bankAccountId,
                result.Status,
                result.GoDutchTransactionCount,
                result.PeriodTransactionCount,
                result.ExportTransactionCount,
                result.OpeningBalance,
                result.OpeningBalanceDate,
                result.ClosingBalance,
                result.ClosingBalanceDate);

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            result.Status = nameof(ImportRunStatus.Failed);
            result.Message = $"GoDutch download gelukt, maar SnelStart upload is mislukt: {ex.Message}";
            result.Details = ex.ToString();
            result.DownloadSucceeded = true;
            result.SnelStartUploadAttempted = true;
            result.UploadSucceeded = false;
            result.IsDuplicateImport = false;
            result.RetryCount = 0;
            result.NotExportedTransactionCount = result.PeriodTransactionCount;

            await CompleteImportRunAsync(importRun, result, cancellationToken);

            _logger.LogError(
                ex,
                "Handmatige resync: GoDutch download gelukt maar SnelStart upload mislukt. ImportRunId: {ImportRunId}, TenantId: {TenantId}, BankAccountId: {BankAccountId}, GoDutchTransactions: {GoDutchTransactionCount}, PeriodTransactions: {PeriodTransactionCount}, OpeningBalance: {OpeningBalance}, OpeningBalanceDate: {OpeningBalanceDate}, ClosingBalance: {ClosingBalance}, ClosingBalanceDate: {ClosingBalanceDate}.",
                importRun.Id,
                tenantId,
                bankAccountId,
                result.GoDutchTransactionCount,
                result.PeriodTransactionCount,
                result.OpeningBalance,
                result.OpeningBalanceDate,
                result.ClosingBalance,
                result.ClosingBalanceDate);

            return result;
        }
    }

    private async Task CompleteImportRunAsync(
        GoDutchImportRun importRun,
        BankAccountResyncResultDto result,
        CancellationToken cancellationToken)
    {
        var status = Enum.Parse<ImportRunStatus>(result.Status);

        if (status == ImportRunStatus.Succeeded)
            importRun.MarkSucceeded(result.ExportTransactionCount, result.RetryCount, result.Message);
        else if (status == ImportRunStatus.Skipped)
            importRun.MarkSkipped(result.RetryCount, result.Message);
        else
            importRun.MarkFailed(result.RetryCount, result.Message);

        await _importRunRepository.UpdateAsync(importRun, cancellationToken);
    }

    private static void FillGoDutchSummary(
        BankAccountResyncResultDto result,
        IReadOnlyList<BankTransactionDto> transactions,
        DateTime periodToUtc)
    {
        var orderedTransactions = transactions
            .OrderBy(x => x.BookingDate)
            .ThenBy(x => x.Id)
            .ToList();

        var periodTransactions = orderedTransactions
            .Where(x => x.IsInRequestedPeriod && !x.IsBalanceAnchor)
            .ToList();

        var balanceAnchors = orderedTransactions
            .Where(x => x.IsBalanceAnchor)
            .ToList();

        result.GoDutchTransactionCount = orderedTransactions.Count;
        result.PeriodTransactionCount = periodTransactions.Count;
        result.BalanceAnchorCount = balanceAnchors.Count;
        result.HasBalanceAnchor = balanceAnchors.Count > 0;
        result.TotalAmount = periodTransactions.Sum(x => x.Amount);
        result.ExportTransactionCount = periodTransactions.Count;
        result.NotExportedTransactionCount = 0;

        var anchor = balanceAnchors
            .Where(x => x.BalanceAfter.HasValue)
            .OrderByDescending(x => x.BookingDate)
            .ThenByDescending(x => x.Id)
            .FirstOrDefault();

        if (anchor is not null)
        {
            result.OpeningBalance = anchor.BalanceAfter;
            result.OpeningBalanceDate = anchor.BookingDate;
        }
        else
        {
            var firstPeriodWithBalance = periodTransactions.FirstOrDefault(x => x.BalanceAfter.HasValue);
            if (firstPeriodWithBalance is not null)
            {
                result.OpeningBalance = firstPeriodWithBalance.BalanceAfter!.Value - firstPeriodWithBalance.Amount;
                result.OpeningBalanceDate = firstPeriodWithBalance.BookingDate;
            }
        }

        var lastPeriodWithBalance = periodTransactions
            .Where(x => x.BalanceAfter.HasValue)
            .LastOrDefault();

        if (lastPeriodWithBalance is not null)
        {
            result.ClosingBalance = lastPeriodWithBalance.BalanceAfter;
            result.ClosingBalanceDate = lastPeriodWithBalance.BookingDate;
        }
        else if (result.OpeningBalance.HasValue)
        {
            result.ClosingBalance = result.OpeningBalance.Value + result.TotalAmount;
            result.ClosingBalanceDate = periodTransactions.LastOrDefault()?.BookingDate
                ?? anchor?.BookingDate
                ?? periodToUtc;
        }
    }

    private static string DetermineStatus(SnelStartImportResult importResult)
    {
        if (importResult.TransactionCount == 0 || importResult.IsDuplicateImport)
        {
            return nameof(ImportRunStatus.Skipped);
        }

        return importResult.Success && importResult.UploadSucceeded
            ? nameof(ImportRunStatus.Succeeded)
            : nameof(ImportRunStatus.Failed);
    }

    private static string BuildMessage(
        SnelStartImportResult importResult,
        BankAccountResyncResultDto result)
    {
        if (importResult.IsDuplicateImport)
        {
            return string.IsNullOrWhiteSpace(importResult.Message)
                ? "SnelStart import overgeslagen: de transacties zijn al aanwezig."
                : importResult.Message;
        }

        if (importResult.TransactionCount == 0)
        {
            return "GoDutch download geslaagd, maar er zijn geen nieuwe transacties voor SnelStart-export gevonden.";
        }

        if (!string.IsNullOrWhiteSpace(importResult.Message))
        {
            return importResult.Message;
        }

        return result.Status switch
        {
            nameof(ImportRunStatus.Succeeded) => "Handmatige resync is succesvol geïmporteerd in SnelStart.",
            nameof(ImportRunStatus.Skipped) => "Handmatige resync is overgeslagen.",
            nameof(ImportRunStatus.Failed) => "Handmatige resync is mislukt tijdens SnelStart upload.",
            _ => "Handmatige resync afgerond."
        };
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
