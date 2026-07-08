using GoDutchSnelStartWebApp.Domain.Enums;

namespace GoDutchSnelStartWebApp.Application.AppUsers.Dtos;

public sealed class AppUserDto
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public AppModule Module { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedUtc { get; init; }
}
