using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Dtos;
using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Services;
using Xunit;

namespace GoDutchSnelStartWebApp.Tests.GoDutchTransactions;

public class BalanceCalculationServiceTests
{
    private readonly BalanceCalculationService _service = new();

    [Fact]
    public void Calculate_NoTransactions_ReturnsZeroBalances()
    {
        // Arrange
        var transactions = new List<BankTransactionDto>();

        // Act
        var result = _service.Calculate(transactions);

        // Assert
        Assert.False(result.HasTransactions);
        Assert.False(result.HasPeriodTransactions);
        Assert.Equal(0m, result.OpeningBalance);
        Assert.Equal(0m, result.ClosingBalance);
        Assert.Equal(0m, result.TotalAmount);
    }

    [Fact]
    public void Calculate_WithoutBalanceAfter_UsesTotalAsClosing()
    {
        // Arrange
        var transactions = new List<BankTransactionDto>
        {
            new()
            {
                Id = "1",
                BookingDate = new DateTime(2025, 1, 1),
                Amount = 100m,
                IsInRequestedPeriod = true
            },
            new()
            {
                Id = "2",
                BookingDate = new DateTime(2025, 1, 2),
                Amount = -40m,
                IsInRequestedPeriod = true
            }
        };

        // Act
        var result = _service.Calculate(transactions);

        // Assert
        Assert.True(result.HasTransactions);
        Assert.True(result.HasPeriodTransactions);
        Assert.Equal(60m, result.TotalAmount);
        Assert.Equal(0m, result.OpeningBalance);
        Assert.Equal(60m, result.ClosingBalance);
    }

    [Fact]
    public void Calculate_WithBalanceAfter_UsesLastBalance()
    {
        // Arrange
        var transactions = new List<BankTransactionDto>
        {
            new()
            {
                Id = "1",
                BookingDate = new DateTime(2025, 1, 1),
                Amount = 100m,
                BalanceAfter = 100m,
                IsInRequestedPeriod = true
            },
            new()
            {
                Id = "2",
                BookingDate = new DateTime(2025, 1, 2),
                Amount = -20m,
                BalanceAfter = 80m,
                IsInRequestedPeriod = true
            }
        };

        // Act
        var result = _service.Calculate(transactions);

        // Assert
        Assert.True(result.HasTransactions);
        Assert.True(result.HasPeriodTransactions);
        Assert.Equal(80m, result.TotalAmount);
        Assert.Equal(0m, result.OpeningBalance);
        Assert.Equal(80m, result.ClosingBalance);
    }

    [Fact]
    public void Calculate_UsesLatestBalanceAfter_WhenMultipleExist()
    {
        // Arrange
        var transactions = new List<BankTransactionDto>
        {
            new()
            {
                Id = "1",
                BookingDate = new DateTime(2025, 1, 1),
                Amount = 100m,
                BalanceAfter = 100m,
                IsInRequestedPeriod = true
            },
            new()
            {
                Id = "2",
                BookingDate = new DateTime(2025, 1, 2),
                Amount = 30m,
                BalanceAfter = 130m,
                IsInRequestedPeriod = true
            }
        };

        // Act
        var result = _service.Calculate(transactions);

        // Assert
        Assert.True(result.HasTransactions);
        Assert.True(result.HasPeriodTransactions);
        Assert.Equal(130m, result.TotalAmount);
        Assert.Equal(0m, result.OpeningBalance);
        Assert.Equal(130m, result.ClosingBalance);
    }

    [Fact]
    public void Calculate_IgnoresTransactionsWithoutBookingDate()
    {
        // Arrange
        var transactions = new List<BankTransactionDto>
        {
            new()
            {
                Id = "1",
                BookingDate = null,
                Amount = 100m,
                IsInRequestedPeriod = true
            },
            new()
            {
                Id = "2",
                BookingDate = new DateTime(2025, 1, 2),
                Amount = 50m,
                IsInRequestedPeriod = true
            }
        };

        // Act
        var result = _service.Calculate(transactions);

        // Assert
        Assert.True(result.HasTransactions);
        Assert.True(result.HasPeriodTransactions);
        Assert.Equal(50m, result.TotalAmount);
        Assert.Equal(0m, result.OpeningBalance);
        Assert.Equal(50m, result.ClosingBalance);
    }

    [Fact]
    public void Calculate_WithBalanceAnchor_UsesAnchorForOpening()
    {
        // Arrange
        var transactions = new List<BankTransactionDto>
        {
            new()
            {
                Id = "anchor",
                BookingDate = new DateTime(2024, 12, 31),
                Amount = 0m,
                BalanceAfter = 50m,
                IsBalanceAnchor = true,
                IsInRequestedPeriod = false
            },
            new()
            {
                Id = "1",
                BookingDate = new DateTime(2025, 1, 1),
                Amount = 20m,
                IsInRequestedPeriod = true
            }
        };

        // Act
        var result = _service.Calculate(transactions);

        // Assert
        Assert.True(result.HasTransactions);
        Assert.True(result.HasPeriodTransactions);
        Assert.Equal(20m, result.TotalAmount);
        Assert.Equal(50m, result.OpeningBalance);
        Assert.Equal(70m, result.ClosingBalance);
    }
}