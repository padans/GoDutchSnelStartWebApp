using GoDutchSnelStartWebApp.Domain.Enums;

namespace GoDutchSnelStartWebApp.Domain.Entities;

public sealed class AppUser
{
    public Guid Id { get; init; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public AppModule Module { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedUtc { get; init; }
}
