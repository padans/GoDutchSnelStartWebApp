namespace GoDutchSnelStartWebApp.Application.Configuration;

public sealed class SnelStartGlobalOptions
{
    public const string SectionName = "SnelStartGlobal";

    /// <summary>
    /// Application-wide B2B developer portal key (Padans). Same for every tenant.
    /// Set in appsettings.Production.json on the VPS — never committed to source control.
    /// This is the single source of truth for the SnelStart <c>Ocp-Apim-Subscription-Key</c>
    /// header; it is never persisted per tenant or per bank account.
    /// </summary>
    public string SubscriptionKey { get; set; } = string.Empty;

    /// <summary>
    /// Returns the trimmed subscription key, or throws when it has not been configured.
    /// </summary>
    public string RequireSubscriptionKey()
    {
        if (string.IsNullOrWhiteSpace(SubscriptionKey))
        {
            throw new InvalidOperationException(
                "SnelStart subscription key is not configured. Set 'SnelStartGlobal:SubscriptionKey' " +
                "in appsettings.Production.json on the server.");
        }

        return SubscriptionKey.Trim();
    }
}
