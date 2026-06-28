using GoDutchSnelStartWebApp.Domain.Entities;

namespace GoDutchSnelStartWebApp.Application.GoDutchTransactions.Helpers;

public static class GoDutchSyncPeriodCalculator
{
    public static (DateTime fromUtc, DateTime toUtc) Calculate(
        DateTime? latestSnelStartDate,
        GoDutchImportRun? lastSuccessfulRun,
        DateTime nowUtc,
        int overlapSeconds)
    {
        var overlap = TimeSpan.FromSeconds(overlapSeconds);

        DateTime fromUtc;

        // 🔥 NIEUWE LOGICA (voorrang)
        if (latestSnelStartDate.HasValue)
        {
            fromUtc = latestSnelStartDate.Value.Date.Subtract(overlap);

            if (fromUtc >= nowUtc)
            {
                fromUtc = nowUtc.AddMinutes(-1);
            }
        }
        else if (lastSuccessfulRun is not null)
        {
            fromUtc = lastSuccessfulRun.PeriodToUtc.Subtract(overlap);

            if (fromUtc >= nowUtc)
            {
                fromUtc = nowUtc.AddMinutes(-1);
            }
        }
        else
        {
            fromUtc = nowUtc.AddDays(-1);
        }

        var toUtc = nowUtc;

        if (fromUtc >= toUtc)
        {
            fromUtc = toUtc.AddMinutes(-5);
        }

        return (fromUtc, toUtc);
    }
}