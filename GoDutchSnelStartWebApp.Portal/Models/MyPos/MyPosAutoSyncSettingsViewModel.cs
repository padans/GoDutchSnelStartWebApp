namespace GoDutchSnelStartWebApp.Portal.Models.MyPos;

public sealed class MyPosAutoSyncSettingsViewModel
{
    public bool Enabled { get; set; }
    public int IntervalMinutes { get; set; }

    /// <summary>"Lookback" of "Period"</summary>
    public string SyncMode { get; set; } = "Lookback";

    public int LookbackHours { get; set; }

    /// <summary>"Day", "Week", "Month", "Quarter" of "Year"</summary>
    public string PeriodType { get; set; } = "Day";
}
