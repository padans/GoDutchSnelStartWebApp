namespace GoDutchSnelStartWebApp.Application.MyPos.Dtos;

public sealed class MyPosAutoSyncSettingsDto
{
    public bool Enabled { get; init; }
    public int IntervalMinutes { get; init; }
    public int LookbackHours { get; init; }
}
