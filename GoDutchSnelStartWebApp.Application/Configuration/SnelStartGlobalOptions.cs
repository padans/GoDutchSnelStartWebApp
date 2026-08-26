namespace GoDutchSnelStartWebApp.Application.Configuration;

public sealed class SnelStartGlobalOptions
{
    public const string SectionName = "SnelStartGlobal";

    /// <summary>
    /// Application-wide B2B developer portal key (Padans). Same for every tenant.
    /// Set in appsettings.Production.json on the VPS — never committed to source control.
    /// </summary>
    public string SubscriptionKey { get; set; } = string.Empty;
}
