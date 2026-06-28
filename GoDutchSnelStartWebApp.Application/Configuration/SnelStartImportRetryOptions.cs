namespace GoDutchSnelStartWebApp.Application.Configuration;

public sealed class SnelStartImportRetryOptions
{
    public const string SectionName = "SnelStartImportRetry";

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Totaal aantal pogingen inclusief de eerste poging.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Wachttijd in milliseconden voor de eerste retry.
    /// Bij volgende retries gebruiken we exponentiële backoff.
    /// </summary>
    public int InitialDelayMilliseconds { get; set; } = 1000;
}