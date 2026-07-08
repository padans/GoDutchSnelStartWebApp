namespace GoDutchSnelStartWebApp.Application.AppUsers.Dtos;

public sealed class ChangePasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}
