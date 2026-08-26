namespace GoDutchSnelStartWebApp.Application.AppUsers.Dtos;

public sealed class SelfChangePasswordRequest
{
    public string Username { get; set; } = string.Empty;
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string? NotificationEmail { get; set; }
}
