using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Dtos;
using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Interfaces;

namespace GoDutchSnelStartWebApp.Application.GoDutchTransactions.Services;

public sealed class BalanceCalculationService : IBalanceCalculationService
{
    public CalculatedBalancesDto Calculate(
        IReadOnlyCollection<BankTransactionDto> transactions)
    {
        if (transactions is null || transactions.Count == 0)
        {
            return new CalculatedBalancesDto
            {
                OpeningBalance = 0m,
                ClosingBalance = 0m,
                TotalAmount = 0m,
                HasTransactions = false,
                HasPeriodTransactions = false
            };
        }

        var ordered = transactions
            .Where(t => t.BookingDate.HasValue)
            .OrderBy(t => t.BookingDate!.Value)
            .ThenBy(t => t.Id)
            .ToList();

        if (ordered.Count == 0)
        {
            return new CalculatedBalancesDto
            {
                OpeningBalance = 0m,
                ClosingBalance = 0m,
                TotalAmount = 0m,
                HasTransactions = false,
                HasPeriodTransactions = false
            };
        }

        var anchor = ordered
            .Where(t => t.IsBalanceAnchor && t.BalanceAfter.HasValue)
            .OrderBy(t => t.BookingDate)
            .ThenBy(t => t.Id)
            .LastOrDefault();

        var periodTransactions = ordered
            .Where(t => t.IsInRequestedPeriod)
            .ToList();

        var totalAmount = periodTransactions.Sum(t => t.Amount);

        if (periodTransactions.Count == 0)
        {
            if (anchor?.BalanceAfter is decimal anchorBalance)
            {
                return new CalculatedBalancesDto
                {
                    OpeningBalance = anchorBalance,
                    ClosingBalance = anchorBalance,
                    TotalAmount = 0m,
                    HasTransactions = true,
                    HasPeriodTransactions = false
                };
            }

            return new CalculatedBalancesDto
            {
                OpeningBalance = 0m,
                ClosingBalance = 0m,
                TotalAmount = 0m,
                HasTransactions = false,
                HasPeriodTransactions = false
            };
        }

        decimal openingBalance;
        decimal closingBalance;

        if (anchor?.BalanceAfter is decimal openingFromAnchor)
        {
            openingBalance = openingFromAnchor;
            closingBalance = openingBalance + totalAmount;
        }
        else
        {
            var lastWithBalanceInPeriod = periodTransactions
                .Where(t => t.BalanceAfter.HasValue)
                .OrderBy(t => t.BookingDate)
                .ThenBy(t => t.Id)
                .LastOrDefault();

            if (lastWithBalanceInPeriod?.BalanceAfter is decimal closingFromPeriod)
            {
                closingBalance = closingFromPeriod;
                openingBalance = closingBalance - totalAmount;
            }
            else
            {
                openingBalance = 0m;
                closingBalance = totalAmount;
            }
        }

        return new CalculatedBalancesDto
        {
            OpeningBalance = openingBalance,
            ClosingBalance = closingBalance,
            TotalAmount = totalAmount,
            HasTransactions = true,
            HasPeriodTransactions = true
        };
    }
}