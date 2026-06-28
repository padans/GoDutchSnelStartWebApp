using System.Text.Json.Serialization;

namespace GoDutchSnelStartWebApp.Infrastructure.Models.GoDutchApi;

public sealed class TransactionsResponse
{
    [JsonPropertyName("results")]
    public List<TransactionApiModel> Results { get; set; } = new();
}