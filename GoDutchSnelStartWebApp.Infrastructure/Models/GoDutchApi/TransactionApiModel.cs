using System.Text.Json.Serialization;

namespace GoDutchSnelStartWebApp.Infrastructure.Models.GoDutchApi;

public sealed class TransactionApiModel
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("amount")]
    public AmountApiModel? Amount { get; set; }

    [JsonPropertyName("side")]
    public string? Side { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("booking_date")]
    public string? BookingDate { get; set; }

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    [JsonPropertyName("counterparty")]
    public string? Counterparty { get; set; }

    [JsonPropertyName("booked_balance_after")]
    public AmountApiModel? BookedBalanceAfter { get; set; }
}