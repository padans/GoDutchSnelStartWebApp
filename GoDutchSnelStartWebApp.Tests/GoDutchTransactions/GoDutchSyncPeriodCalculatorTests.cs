using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Helpers;
using GoDutchSnelStartWebApp.Domain.Entities;
using GoDutchSnelStartWebApp.Domain.Enums;
using Xunit;

namespace GoDutchSnelStartWebApp.Tests.GoDutchTransactions;

public class GoDutchSyncPeriodCalculatorTests
{
    [Fact]
    public void Calculate_NoPreviousRun_UsesLastDay()
    {
        var now = new DateTime(2025, 1, 10, 12, 0, 0);

        var (fromUtc, toUtc) = GoDutchSyncPeriodCalculator.Calculate(
            null,
            null,
            now,
            120);

        Assert.Equal(now.AddDays(-1), fromUtc);
        Assert.Equal(now, toUtc);
    }

    [Fact]
    public void Calculate_WithPreviousRun_UsesOverlap()
    {
        var now = new DateTime(2025, 1, 10, 12, 0, 0);

        var lastRun = RunWithPeriodTo(new DateTime(2025, 1, 10, 11, 0, 0));

        var (fromUtc, toUtc) = GoDutchSyncPeriodCalculator.Calculate(
            null,
            lastRun,
            now,
            120);

        Assert.Equal(new DateTime(2025, 1, 10, 10, 58, 0), fromUtc);
        Assert.Equal(now, toUtc);
    }

    [Fact]
    public void Calculate_WhenFromUtcEqualsNow_FallsBackToOneMinuteEarlier()
    {
        var now = new DateTime(2025, 1, 10, 12, 0, 0);

        var lastRun = RunWithPeriodTo(now);

        var (fromUtc, toUtc) = GoDutchSyncPeriodCalculator.Calculate(
            null,
            lastRun,
            now,
            0);

        Assert.Equal(now.AddMinutes(-1), fromUtc);
        Assert.Equal(now, toUtc);
    }

    [Fact]
    public void Calculate_WithLatestSnelStartDate_UsesThatDateWithOverlap()
    {
        var now = new DateTime(2025, 1, 10, 12, 0, 0);
        var latestSnelStartDate = new DateTime(2025, 1, 9, 0, 0, 0);

        var (fromUtc, toUtc) = GoDutchSyncPeriodCalculator.Calculate(
            latestSnelStartDate,
            null,
            now,
            120);

        Assert.Equal(new DateTime(2025, 1, 8, 23, 58, 0), fromUtc);
        Assert.Equal(now, toUtc);
    }

    [Fact]
    public void Calculate_WithLatestSnelStartDate_TakesPrecedenceOverLastSuccessfulRun()
    {
        var now = new DateTime(2025, 1, 10, 12, 0, 0);
        var latestSnelStartDate = new DateTime(2025, 1, 9, 0, 0, 0);

        var lastRun = RunWithPeriodTo(new DateTime(2025, 1, 10, 11, 0, 0));

        var (fromUtc, toUtc) = GoDutchSyncPeriodCalculator.Calculate(
            latestSnelStartDate,
            lastRun,
            now,
            120);

        Assert.Equal(new DateTime(2025, 1, 8, 23, 58, 0), fromUtc);
        Assert.Equal(now, toUtc);
    }

    private static GoDutchImportRun RunWithPeriodTo(DateTime periodTo) =>
        GoDutchImportRun.Reconstitute(
            id: Guid.NewGuid(),
            tenantId: Guid.Empty,
            bankAccountId: Guid.Empty,
            iban: string.Empty,
            periodFrom: default,
            periodTo: periodTo,
            triggerSource: ImportRunTriggerSource.BackgroundWorker,
            status: ImportRunStatus.Succeeded,
            transactionCount: 0,
            retryCount: 0,
            message: null,
            startedUtc: default,
            completedUtc: null);
}
