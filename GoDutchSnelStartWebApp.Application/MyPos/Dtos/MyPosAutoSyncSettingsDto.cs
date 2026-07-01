namespace GoDutchSnelStartWebApp.Application.MyPos.Dtos;

public sealed class MyPosAutoSyncSettingsDto
{
    public bool Enabled { get; init; }
    public int IntervalMinutes { get; init; }

    /// <summary>"Lookback" = rollend venster in uren; "Period" = afgesloten kalenderperiode.</summary>
    public string SyncMode { get; init; } = "Lookback";

    public int LookbackHours { get; init; }

    /// <summary>"Day", "Week", "Month", "Quarter" of "Year" — alleen relevant bij SyncMode=Period.</summary>
    public string PeriodType { get; init; } = "Day";
}
