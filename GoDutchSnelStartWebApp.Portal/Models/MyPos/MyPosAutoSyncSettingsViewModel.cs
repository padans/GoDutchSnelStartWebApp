namespace GoDutchSnelStartWebApp.Portal.Models.MyPos;

public sealed class MyPosAutoSyncSettingsViewModel
{
    public bool Enabled { get; set; }
    public int IntervalMinutes { get; set; }
    public int LookbackHours { get; set; }
}
