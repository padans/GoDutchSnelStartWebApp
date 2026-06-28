using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Application.Abstractions.Security;
using GoDutchSnelStartWebApp.Application.GoDutchConnections.Dtos;
using GoDutchSnelStartWebApp.Application.GoDutchConnections.Interfaces;
using GoDutchSnelStartWebApp.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace GoDutchSnelStartWebApp.Application.GoDutchConnections.Services;

public sealed class TenantGoDutchConnectionService : ITenantGoDutchConnectionService
{
    private readonly ITenantGoDutchConnectionRepository _connectionRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ISecretEncryptionService _secretEncryptionService;
    private readonly ILogger<TenantGoDutchConnectionService> _logger;

    public TenantGoDutchConnectionService(
        ITenantGoDutchConnectionRepository connectionRepository,
        ITenantRepository tenantRepository,
        ISecretEncryptionService secretEncryptionService,
        ILogger<TenantGoDutchConnectionService> logger)
    {
        _connectionRepository = connectionRepository;
        _tenantRepository = tenantRepository;
        _secretEncryptionService = secretEncryptionService;
        _logger = logger;
    }

    public async Task<TenantGoDutchConnectionDto?> GetByTenantIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await EnsureTenantExistsAsync(tenantId, cancellationToken);

        var connection = await _connectionRepository.GetByTenantIdAsync(tenantId, cancellationToken);
        if (connection is null)
        {
            _logger.LogInformation("No GoDutch connection found for tenant {TenantId}", tenantId);
            return null;
        }

        _logger.LogInformation(
            "GoDutch connection {ConnectionId} retrieved for tenant {TenantId}",
            connection.Id,
            tenantId);

        return MapToDto(connection);
    }

    public async Task<Guid> CreateAsync(
        Guid tenantId,
        CreateTenantGoDutchConnectionRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request.ApiBaseUrl, request.Username);

        await EnsureTenantExistsAsync(tenantId, cancellationToken);

        var existing = await _connectionRepository.GetByTenantIdAsync(tenantId, cancellationToken);
        if (existing is not null)
        {
            _logger.LogWarning(
                "GoDutch connection creation failed for tenant {TenantId}: connection already exists",
                tenantId);

            throw new InvalidOperationException("GoDutch connection already exists for this tenant.");
        }

        var nowUtc = DateTime.UtcNow;
        var connection = new TenantGoDutchConnection
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ApiBaseUrl = NormalizeRequired(request.ApiBaseUrl),
            Username = NormalizeRequired(request.Username),
            PasswordEncrypted = string.IsNullOrWhiteSpace(request.Password)
                ? null
                : _secretEncryptionService.Encrypt(request.Password),
            IsActive = request.IsActive,
            CreatedUtc = nowUtc,
            ModifiedUtc = null
        };

        _logger.LogInformation(
            "Creating GoDutch connection {ConnectionId} for tenant {TenantId}",
            connection.Id,
            tenantId);

        await _connectionRepository.CreateAsync(connection, cancellationToken);

        _logger.LogInformation(
            "GoDutch connection {ConnectionId} created successfully for tenant {TenantId}",
            connection.Id,
            tenantId);

        return connection.Id;
    }

    public async Task UpdateAsync(
        Guid tenantId,
        Guid id,
        UpdateTenantGoDutchConnectionRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request.ApiBaseUrl, request.Username);

        await EnsureTenantExistsAsync(tenantId, cancellationToken);

        var existing = await _connectionRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null || existing.TenantId != tenantId)
        {
            _logger.LogWarning(
                "GoDutch connection update failed: connection {ConnectionId} not found for tenant {TenantId}",
                id,
                tenantId);

            throw new KeyNotFoundException("GoDutch connection not found.");
        }

        existing.ApiBaseUrl = NormalizeRequired(request.ApiBaseUrl);
        existing.Username = NormalizeRequired(request.Username);
        existing.IsActive = request.IsActive;
        existing.ModifiedUtc = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            existing.PasswordEncrypted = _secretEncryptionService.Encrypt(request.Password);
        }

        await _connectionRepository.UpdateAsync(existing, cancellationToken);

        _logger.LogInformation(
            "GoDutch connection {ConnectionId} updated successfully for tenant {TenantId}",
            id,
            tenantId);
    }

    public async Task DeleteAsync(
        Guid tenantId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await EnsureTenantExistsAsync(tenantId, cancellationToken);

        var existing = await _connectionRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null || existing.TenantId != tenantId)
        {
            _logger.LogWarning(
                "GoDutch connection delete failed: connection {ConnectionId} not found for tenant {TenantId}",
                id,
                tenantId);

            throw new KeyNotFoundException("GoDutch connection not found.");
        }

        await _connectionRepository.DeleteAsync(id, DateTime.UtcNow, cancellationToken);

        _logger.LogInformation(
            "GoDutch connection {ConnectionId} deleted successfully for tenant {TenantId}",
            id,
            tenantId);
    }

    private async Task EnsureTenantExistsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            _logger.LogWarning("Tenant {TenantId} not found", tenantId);
            throw new KeyNotFoundException("Tenant not found.");
        }
    }

    private static void Validate(string apiBaseUrl, string username)
    {
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            throw new ArgumentException("GoDutch API base URL is required.", nameof(apiBaseUrl));
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("GoDutch username is required.", nameof(username));
        }
    }

    private static string NormalizeRequired(string value)
    {
        return value.Trim().TrimEnd('/');
    }

    private static TenantGoDutchConnectionDto MapToDto(TenantGoDutchConnection connection)
    {
        return new TenantGoDutchConnectionDto
        {
            Id = connection.Id,
            TenantId = connection.TenantId,
            ApiBaseUrl = connection.ApiBaseUrl,
            Username = connection.Username,
            HasPassword = !string.IsNullOrWhiteSpace(connection.PasswordEncrypted),
            IsActive = connection.IsActive,
            CreatedUtc = connection.CreatedUtc,
            ModifiedUtc = connection.ModifiedUtc
        };
    }
}
