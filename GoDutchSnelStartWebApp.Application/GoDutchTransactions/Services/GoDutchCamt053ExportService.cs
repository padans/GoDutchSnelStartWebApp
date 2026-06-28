using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Dtos;
using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Interfaces;
using Microsoft.Extensions.Logging;

namespace GoDutchSnelStartWebApp.Application.GoDutchTransactions.Services;

public sealed class GoDutchCamt053ExportService : IGoDutchCamt053ExportService
{
    private readonly IGoDutchTransactionService _goDutchTransactionService;
    private readonly ICamt053Generator _camt053Generator;
    private readonly IBalanceCalculationService _balanceCalculationService;
    private readonly ILogger<GoDutchCamt053ExportService> _logger;

    public GoDutchCamt053ExportService(
        IGoDutchTransactionService goDutchTransactionService,
        ICamt053Generator camt053Generator,
        IBalanceCalculationService balanceCalculationService,
        ILogger<GoDutchCamt053ExportService> logger)
    {
        _goDutchTransactionService = goDutchTransactionService;
        _camt053Generator = camt053Generator;
        _balanceCalculationService = balanceCalculationService;
        _logger = logger;
    }

    public async Task<string> GenerateCamt053Async(
        Guid tenantId,
        Guid bankAccountId,
        string iban,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(iban))
        {
            throw new InvalidOperationException("IBAN is required.");
        }

        var fromDate = from.Date;
        var toDate = to.Date;

        if (toDate < fromDate)
        {
            throw new InvalidOperationException("'to' must be greater than or equal to 'from'.");
        }

        var transactions = await _goDutchTransactionService.GetTransactionsAsync(
            tenantId,
            bankAccountId,
            iban,
            fromDate,
            toDate,
            cancellationToken);

        var orderedTransactions = transactions
            .Where(x => x.BookingDate.HasValue)
            .OrderBy(x => x.BookingDate!.Value)
            .ThenBy(x => x.Id)
            .ToList();

        var balances = _balanceCalculationService.Calculate(orderedTransactions);

        var exportTransactions = orderedTransactions
            .Where(x => x.IsInRequestedPeriod)
            .ToList();

        var openingBalance = balances.OpeningBalance;
        var closingBalance = balances.ClosingBalance;
        var totalAmount = balances.TotalAmount;

        _logger.LogInformation(
            "CAMT053 request opgebouwd. Iban: {Iban}, transacties export: {ExportCount}, transacties totaal incl. anchor: {TotalCount}, totalAmount: {TotalAmount}, openingBalance: {OpeningBalance}, closingBalance: {ClosingBalance}.",
            iban,
            exportTransactions.Count,
            orderedTransactions.Count,
            totalAmount,
            openingBalance,
            closingBalance);

        var request = new Mt940Request
        {
            Iban = NormalizeIban(iban),
            Currency = orderedTransactions
                .Select(x => x.Currency)
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "EUR",
            StatementReference = $"GODUTCH{DateTime.UtcNow:yyyyMMddHHmmss}",
            StatementNumber = $"{fromDate:yyyyMMdd}",
            StatementDate = toDate,
            PeriodFrom = fromDate,
            PeriodTo = toDate,
            OpeningBalance = openingBalance,
            ClosingBalance = closingBalance,
            Transactions = exportTransactions
        };

        return _camt053Generator.Generate(request);
    }

    private static string NormalizeIban(string iban)
    {
        return iban.Replace(" ", string.Empty).Trim().ToUpperInvariant();
    }
}