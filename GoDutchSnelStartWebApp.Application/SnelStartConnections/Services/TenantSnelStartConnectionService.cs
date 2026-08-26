using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Application.Abstractions.Repositories.SnelStart;
using GoDutchSnelStartWebApp.Application.Abstractions.Security;
using GoDutchSnelStartWebApp.Application.Configuration;
using GoDutchSnelStartWebApp.Application.SnelStartConnections.Dtos;
using GoDutchSnelStartWebApp.Application.SnelStartConnections.Interfaces;
using GoDutchSnelStartWebApp.Domain.Entities.SnelStart;
using GoDutchSnelStartWebApp.Domain.Enums;
using Microsoft.Extensions.Options;

namespace GoDutchSnelStartWebApp.Application.SnelStartConnections.Services;

public sealed class TenantSnelStartConnectionService : ITenantSnelStartConnectionService
{
    private const string DefaultAuthUrl = "https://auth.snelstart.nl/b2b/token";
    private const string DefaultApiBaseUrl = "https://b2bapi.snelstart.nl/v2";

    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantSnelStartConnectionRepository _connectionRepository;
    private readonly ISecretEncryptionService _secretEncryptionService;
    private readonly SnelStartGlobalOptions _snelStartGlobal;

    public TenantSnelStartConnectionService(
        ITenantRepository tenantRepository,
        ITenantSnelStartConnectionRepository connectionRepository,
        ISecretEncryptionService secretEncryptionService,
        IOptions<SnelStartGlobalOptions> snelStartGlobalOptions)
    {
        _tenantRepository = tenantRepository;
        _connectionRepository = connectionRepository;
        _secretEncryptionService = secretEncryptionService;
        _snelStartGlobal = snelStartGlobalOptions.Value;
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

        // Use the request key first; fall back to the globally configured application key.
        var effectiveSubscriptionKey = !string.IsNullOrWhiteSpace(request.SubscriptionKey)
            ? request.SubscriptionKey.Trim()
            : _snelStartGlobal.SubscriptionKey;

        if (string.IsNullOrWhiteSpace(effectiveSubscriptionKey))
        {
            throw new ArgumentException(
                "SubscriptionKey is required. Provide it in the request or configure SnelStartGlobal:SubscriptionKey.",
                nameof(request));
        }

        var now = DateTime.UtcNow;

        var connection = new TenantSnelStartConnection
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConnectionType = ParseConnectionType(request.ConnectionType),
            AuthUrl = NormalizeUrl(request.AuthUrl, DefaultAuthUrl),
            ApiBaseUrl = NormalizeApiBaseUrl(request.ApiBaseUrl),
            SubscriptionKeyEncrypted = _secretEncryptionService.Encrypt(effectiveSubscriptionKey),
            ClientKeyEncrypted = string.IsNullOrWhiteSpace(request.ClientKey)
                ? null
                : _secretEncryptionService.Encrypt(request.ClientKey.Trim()),
            OAuthAccessTokenEncrypted = null,
            OAuthRefreshTokenEncrypted = null,
            OAuthExpiresUtc = null,
            IsActive = request.IsActive,
            CreatedUtc = now,
            ModifiedUtc = null,
            KeyExpiresUtc = now.AddDays(90),
            ExpiryWarningSentUtc = null
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

        var keysChanged = false;

        if (!string.IsNullOrWhiteSpace(request.SubscriptionKey))
        {
            existing.SubscriptionKeyEncrypted = _secretEncryptionService.Encrypt(request.SubscriptionKey.Trim());
            keysChanged = true;
        }

        if (!string.IsNullOrWhiteSpace(request.ClientKey))
        {
            existing.ClientKeyEncrypted = _secretEncryptionService.Encrypt(request.ClientKey.Trim());
            keysChanged = true;
        }

        // Nieuwe sleutels ingevoerd → vervaldatum resetten naar 90 dagen vanaf nu
        if (keysChanged && existing.ConnectionType == SnelStartConnectionType.CustomKey)
        {
            existing.KeyExpiresUtc = existing.ModifiedUtc!.Value.AddDays(90);
            existing.ExpiryWarningSentUtc = null;
        }

        if (existing.ConnectionType == SnelStartConnectionType.CustomKey)
        {
            // If the DB row has no encrypted key yet, auto-apply the global application key.
            if (string.IsNullOrWhiteSpace(existing.SubscriptionKeyEncrypted))
            {
                if (string.IsNullOrWhiteSpace(_snelStartGlobal.SubscriptionKey))
                    throw new InvalidOperationException("SubscriptionKey is required. Configure SnelStartGlobal:SubscriptionKey.");

                existing.SubscriptionKeyEncrypted = _secretEncryptionService.Encrypt(_snelStartGlobal.SubscriptionKey);
            }

            // ClientKey may be set later (tenant adds their Maatwerksleutel separately).
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
            ModifiedUtc = connection.ModifiedUtc,
            KeyExpiresUtc = connection.KeyExpiresUtc
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
