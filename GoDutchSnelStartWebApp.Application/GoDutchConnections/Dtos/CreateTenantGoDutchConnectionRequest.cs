namespace GoDutchSnelStartWebApp.Application.GoDutchConnections.Dtos;

public sealed class CreateTenantGoDutchConnectionRequest
{
    public string ApiBaseUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;

    public string? Password { get; set; }

    public bool IsActive { get; set; } = true;
}