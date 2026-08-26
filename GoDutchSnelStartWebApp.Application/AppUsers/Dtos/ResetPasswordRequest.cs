namespace GoDutchSnelStartWebApp.Application.AppUsers.Dtos;

public sealed record ResetPasswordRequest(string Token, string NewPassword);
