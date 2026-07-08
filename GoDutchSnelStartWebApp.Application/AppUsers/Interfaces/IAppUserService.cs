using GoDutchSnelStartWebApp.Application.AppUsers.Dtos;
using GoDutchSnelStartWebApp.Domain.Enums;

namespace GoDutchSnelStartWebApp.Application.AppUsers.Interfaces;

public interface IAppUserService
{
    Task<IReadOnlyList<AppUserDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AppUserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(string username, string password, AppModule module, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, string username, AppModule module, bool isActive, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(Guid id, string newPassword, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AppUserDto?> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken = default);
}
