namespace GoDutchSnelStartWebApp.Application.Configuration;

public sealed class GoDutchAutoSyncOptions
{
    public const string SectionName = "GoDutchAutoSync";

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Interval in minuten tussen twee synchronisatierondes.
    /// </summary>
    public int IntervalMinutes { get; set; } = 15;

    /// <summary>
    /// Overlap in seconden om gemiste transacties tussen runs te voorkomen.
    /// </summary>
    public int OverlapSeconds { get; set; } = 120;

    /// <summary>
    /// Als de laatste run recent is, sla startup-run over (in minuten).
    /// </summary>
    public int SkipStartupRunIfLastRunWithinMinutes { get; set; } = 10;
}