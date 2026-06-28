using System;
using System.Collections.Generic;
using System.Text;

namespace GoDutchSnelStartWebApp.Infrastructure.ExternalServices.SnelStart
{
    public sealed class SnelStartUploadSettings
    {
        public string AuthUrl { get; init; } = "https://auth.snelstart.nl/b2b/token";
        public string ApiBaseUrl { get; init; } = "https://b2bapi.snelstart.nl/v2";
        public string ClientKey { get; init; } = string.Empty;
        public string SubscriptionKey { get; init; } = string.Empty;
    }
}
