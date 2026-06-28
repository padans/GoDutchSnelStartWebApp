namespace GoDutchSnelStartWebApp.Application.Configuration;

public sealed class GoDutchApiRetryOptions
{
    public const string SectionName = "GoDutchApiRetry";

    /// <summary>
    /// Schakelt retry/backoff voor GoDutch API calls in of uit.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Totaal aantal pogingen inclusief de eerste poging.
    /// </summary>
    public int MaxAttempts { get; set; } = 4;

    /// <summary>
    /// Wachttijd in milliseconden voor de eerste retry.
    /// Volgende retries gebruiken exponentiële backoff.
    /// </summary>
    public int InitialDelayMilliseconds { get; set; } = 1500;

    /// <summary>
    /// Bovengrens voor de backoff per poging.
    /// </summary>
    public int MaxDelayMilliseconds { get; set; } = 15000;

    /// <summary>
    /// Kleine willekeurige extra wachttijd om bursts te spreiden.
    /// </summary>
    public int JitterMilliseconds { get; set; } = 500;
}