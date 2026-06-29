namespace GoDutchSnelStartWebApp.Application.Configuration;

public sealed class MyPosAutoSyncOptions
{
    public const string SectionName = "MyPosAutoSync";

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Interval in minuten tussen twee importrondes.
    /// </summary>
    public int IntervalMinutes { get; set; } = 60;

    /// <summary>
    /// Hoeveel uur terug te kijken per importronde.
    /// </summary>
    public int LookbackHours { get; set; } = 48;
}
