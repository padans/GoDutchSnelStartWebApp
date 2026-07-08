namespace GoDutchSnelStartWebApp.Portal.Models;

public sealed class AppUserViewModel
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public sealed class LoginRequestViewModel
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
}
