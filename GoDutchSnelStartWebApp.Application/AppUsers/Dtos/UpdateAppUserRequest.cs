using GoDutchSnelStartWebApp.Domain.Enums;

namespace GoDutchSnelStartWebApp.Application.AppUsers.Dtos;

public sealed class UpdateAppUserRequest
{
    public string Username { get; set; } = string.Empty;
    public AppModule Module { get; set; }
    public bool IsActive { get; set; }
}
