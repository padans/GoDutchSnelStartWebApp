namespace GoDutchSnelStartWebApp.Application.ConnectivityTests.Dtos;

public sealed class ConnectionTestResultDto
{
    public bool Success { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? TestedUrl { get; set; }
    public int? StatusCode { get; set; }
}