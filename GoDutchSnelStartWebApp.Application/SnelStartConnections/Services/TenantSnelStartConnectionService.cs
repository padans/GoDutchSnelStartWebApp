using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Application.Abstractions.Repositories.SnelStart;
using GoDutchSnelStartWebApp.Application.Abstractions.Security;
using GoDutchSnelStartWebApp.Application.SnelStartConnections.Dtos;
using GoDutchSnelStartWebApp.Application.SnelStartConnections.Interfaces;
using GoDutchSnelStartWebApp.Domain.Entities.SnelStart;
using GoDutchSnelStartWebApp.Domain.Enums;

namespace GoDutchSnelStartWebApp.Application.SnelStartConnections.Services;

public sealed class TenantSnelStartConnectionService : ITenantSnelStartConnectionService
{
    private const string DefaultAuthUrl = "https://auth.snelstart.nl/b2b/token";
    private const string DefaultApiBaseUrl = "https://b2bapi.snelstart.nl/v2";

    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantSnelStartConnectionRepository _connectionRepository;
    private readonly ISecretEncryptionService _secretEncryptionService;

    public TenantSnelStartConnectionService(
        ITenantRepository tenantRepository,
        ITenantSnelStartConnectionRepository connectionRepository,
        ISecretEncryptionService secretEncryptionService)
    {
        _tenantRepository = tenantRepository;
        _connectionRepository = connectionRepository;
        _secretEncryptionService = secretEncryptionService;
    }

    public async Task<TenantSnelStartConnectionDto?> GetByTenantIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await EnsureTenantExistsAsync(tenantId, cancellationToken);

        var connection = await _connectionRepository.GetByTenantIdAsync(tenantId, cancellationToken);

        return connection is null ? null : Map(connection);
    }

    public async Task<Guid> CreateAsync(
        Guid tenantId,
        CreateTenantSnelStartConnectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await EnsureTenantExistsAsync(tenantId, cancellationToken);

        var existing = await _connectionRepository.GetByTenantIdAsync(tenantId, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException("An active SnelStart connection already exists for this tenant.");
        }

        if (string.IsNullOrWhiteSpace(request.SubscriptionKey))
        {
            throw new ArgumentException("SubscriptionKey is required for CustomKey SnelStart connections.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.ClientKey))
        {
            throw new ArgumentException("ClientKey is required for CustomKey SnelStart connections.", nameof(request));
        }

        var now = DateTime.UtcNow;

        var connection = new TenantSnelStartConnection
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConnectionType = ParseConnectionType(request.ConnectionType),
            AuthUrl = NormalizeUrl(request.AuthUrl, DefaultAuthUrl),
            ApiBaseUrl = NormalizeApiBaseUrl(request.ApiBaseUrl),
            SubscriptionKeyEncrypted = _secretEncryptionService.Encrypt(request.SubscriptionKey.Trim()),
            ClientKeyEncrypted = _secretEncryptionService.Encrypt(request.ClientKey.Trim()),
            OAuthAccessTokenEncrypted = null,
            OAuthRefreshTokenEncrypted = null,
            OAuthExpiresUtc = null,
            IsActive = request.IsActive,
            CreatedUtc = now,
            ModifiedUtc = null
        };

        await _connectionRepository.CreateAsync(connection, cancellationToken);

        return connection.Id;
    }

    public async Task UpdateAsync(
        Guid tenantId,
        Guid id,
        UpdateTenantSnelStartConnectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await EnsureTenantExistsAsync(tenantId, cancellationToken);

        var existing = await _connectionRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null || existing.TenantId != tenantId)
        {
            throw new KeyNotFoundException("SnelStart connection not found.");
        }

        existing.ConnectionType = ParseConnectionType(request.ConnectionType);
        existing.AuthUrl = NormalizeUrl(request.AuthUrl, DefaultAuthUrl);
        existing.ApiBaseUrl = NormalizeApiBaseUrl(request.ApiBaseUrl);
        existing.IsActive = request.IsActive;
        existing.ModifiedUtc = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.SubscriptionKey))
        {
            existing.SubscriptionKeyEncrypted = _secretEncryptionService.Encrypt(request.SubscriptionKey.Trim());
        }

        if (!string.IsNullOrWhiteSpace(request.ClientKey))
        {
            existing.ClientKeyEncrypted = _secretEncryptionService.Encrypt(request.ClientKey.Trim());
        }

        if (existing.ConnectionType == SnelStartConnectionType.CustomKey)
        {
            if (string.IsNullOrWhiteSpace(existing.SubscriptionKeyEncrypted))
            {
                throw new InvalidOperationException("SubscriptionKey is required for CustomKey SnelStart connections.");
            }

            if (string.IsNullOrWhiteSpace(existing.ClientKeyEncrypted))
            {
                throw new InvalidOperationException("ClientKey is required for CustomKey SnelStart connections.");
            }
        }

        await _connectionRepository.UpdateAsync(existing, cancellationToken);
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
            throw new KeyNotFoundException("SnelStart connection not found.");
        }

        await _connectionRepository.DeleteAsync(id, DateTime.UtcNow, cancellationToken);
    }

    private async Task EnsureTenantExistsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            throw new KeyNotFoundException("Tenant not found.");
        }
    }

    private static TenantSnelStartConnectionDto Map(TenantSnelStartConnection connection)
    {
        return new TenantSnelStartConnectionDto
        {
            Id = connection.Id,
            TenantId = connection.TenantId,
            ConnectionType = connection.ConnectionType.ToString(),
            AuthUrl = connection.AuthUrl,
            ApiBaseUrl = connection.ApiBaseUrl,
            HasSubscriptionKey = !string.IsNullOrWhiteSpace(connection.SubscriptionKeyEncrypted),
            HasClientKey = !string.IsNullOrWhiteSpace(connection.ClientKeyEncrypted),
            HasOAuthAccessToken = !string.IsNullOrWhiteSpace(connection.OAuthAccessTokenEncrypted),
            HasOAuthRefreshToken = !string.IsNullOrWhiteSpace(connection.OAuthRefreshTokenEncrypted),
            OAuthExpiresUtc = connection.OAuthExpiresUtc,
            IsActive = connection.IsActive,
            CreatedUtc = connection.CreatedUtc,
            ModifiedUtc = connection.ModifiedUtc
        };
    }

    private static SnelStartConnectionType ParseConnectionType(string? connectionType)
    {
        if (string.IsNullOrWhiteSpace(connectionType) ||
            connectionType.Trim().Equals(nameof(SnelStartConnectionType.CustomKey), StringComparison.OrdinalIgnoreCase))
        {
            return SnelStartConnectionType.CustomKey;
        }

        throw new NotSupportedException("Only CustomKey SnelStart connections are supported right now. OAuth will be added later.");
    }

    private static string NormalizeApiBaseUrl(string? value)
    {
        var normalized = NormalizeUrl(value, DefaultApiBaseUrl);

        return normalized.EndsWith("/v2", StringComparison.OrdinalIgnoreCase)
            ? normalized
            : normalized + "/v2";
    }

    private static string NormalizeUrl(string? value, string fallback)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim();

        return normalized.TrimEnd('/');
    }
}
