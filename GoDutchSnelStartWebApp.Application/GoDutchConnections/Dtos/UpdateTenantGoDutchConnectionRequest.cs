namespace GoDutchSnelStartWebApp.Application.GoDutchConnections.Dtos;

public sealed class UpdateTenantGoDutchConnectionRequest
{
    public string ApiBaseUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Laat leeg om het bestaande versleutelde wachtwoord te behouden.
    /// </summary>
    public string? Password { get; set; }

    public bool IsActive { get; set; } = true;
}