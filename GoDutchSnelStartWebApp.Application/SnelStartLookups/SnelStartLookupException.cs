namespace GoDutchSnelStartWebApp.Application.SnelStartLookups;

public sealed class SnelStartLookupException : Exception
{
    public int? ExternalStatusCode { get; }

    public SnelStartLookupException(string message, int? externalStatusCode = null)
        : base(message)
    {
        ExternalStatusCode = externalStatusCode;
    }
}