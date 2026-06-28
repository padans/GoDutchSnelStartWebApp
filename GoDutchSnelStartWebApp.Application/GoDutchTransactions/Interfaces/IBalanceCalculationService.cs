using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Dtos;

namespace GoDutchSnelStartWebApp.Application.GoDutchTransactions.Interfaces;

public interface IBalanceCalculationService
{
    CalculatedBalancesDto Calculate(
        IReadOnlyCollection<BankTransactionDto> transactions);
}