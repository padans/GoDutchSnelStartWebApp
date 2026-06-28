using System.Text.Json.Serialization;

namespace GoDutchSnelStartWebApp.Infrastructure.Models.GoDutchApi;

public sealed class AmountApiModel
{
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}