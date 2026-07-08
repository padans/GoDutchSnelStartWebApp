using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Application.Abstractions.Security;
using GoDutchSnelStartWebApp.Application.AppUsers.Dtos;
using GoDutchSnelStartWebApp.Application.AppUsers.Interfaces;
using GoDutchSnelStartWebApp.Domain.Entities;
using GoDutchSnelStartWebApp.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace GoDutchSnelStartWebApp.Application.AppUsers.Services;

public sealed class AppUserService : IAppUserService
{
    private readonly IAppUserRepository _repository;
    private readonly IPasswordHashingService _passwordHasher;
    private readonly ILogger<AppUserService> _logger;

    public AppUserService(
        IAppUserRepository repository,
        IPasswordHashingService passwordHasher,
        ILogger<AppUserService> logger)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AppUserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _repository.GetAllAsync(cancellationToken);
        return users.Select(MapToDto).ToList();
    }

    public async Task<AppUserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        return user is null ? null : MapToDto(user);
    }

    public async Task<Guid> CreateAsync(string username, string password, AppModule module, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.", nameof(username));
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password is required.", nameof(password));

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Username = username.Trim().ToLowerInvariant(),
            PasswordHash = _passwordHasher.Hash(password),
            Module = module,
            IsActive = true,
            CreatedUtc = DateTime.UtcNow
        };

        await _repository.CreateAsync(user, cancellationToken);
        _logger.LogInformation("AppUser {Username} created with module {Module}", user.Username, user.Module);
        return user.Id;
    }

    public async Task UpdateAsync(Guid id, string username, AppModule module, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"AppUser {id} not found.");

        user.Username = username.Trim().ToLowerInvariant();
        user.Module = module;
        user.IsActive = isActive;

        await _repository.UpdateAsync(user, cancellationToken);
        _logger.LogInformation("AppUser {UserId} updated", id);
    }

    public async Task ChangePasswordAsync(Guid id, string newPassword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
            throw new ArgumentException("Password is required.", nameof(newPassword));

        var user = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"AppUser {id} not found.");

        user.PasswordHash = _passwordHasher.Hash(newPassword);
        await _repository.UpdateAsync(user, cancellationToken);
        _logger.LogInformation("AppUser {UserId} password changed", id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"AppUser {id} not found.");

        await _repository.DeleteAsync(id, cancellationToken);
        _logger.LogInformation("AppUser {UserId} deleted", id);
    }

    public async Task<AppUserDto?> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;

        var user = await _repository.GetByUsernameAsync(username.Trim().ToLowerInvariant(), cancellationToken);

        if (user is null || !user.IsActive)
            return null;

        if (!_passwordHasher.Verify(password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for username {Username}", username);
            return null;
        }

        _logger.LogInformation("Successful login for username {Username}", username);
        return MapToDto(user);
    }

    private static AppUserDto MapToDto(AppUser user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Module = user.Module,
        IsActive = user.IsActive,
        CreatedUtc = user.CreatedUtc
    };
}
