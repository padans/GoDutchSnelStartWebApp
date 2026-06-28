using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Application.Abstractions.Repositories.MyPos;
using GoDutchSnelStartWebApp.Application.Abstractions.Security;
using GoDutchSnelStartWebApp.Application.MyPos.Dtos;
using GoDutchSnelStartWebApp.Application.MyPos.Interfaces;
using GoDutchSnelStartWebApp.Domain.Entities.MyPos;
using GoDutchSnelStartWebApp.Domain.ValueObjects;

namespace GoDutchSnelStartWebApp.Application.MyPos.Services;

public sealed class TenantMyPosConnectionService : ITenantMyPosConnectionService
{
    private const string DefaultAuthUrl = "https://auth-api.mypos.com/oauth/token";
    private const string DefaultTransactionsApiBaseUrl = "https://transactions-api.mypos.com/v1.1";

    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantMyPosConnectionRepository _connectionRepository;
    private readonly ISecretEncryptionService _secretEncryptionService;

    public TenantMyPosConnectionService(
        ITenantRepository tenantRepository,
        ITenantMyPosConnectionRepository connectionRepository,
        ISecretEncryptionService secretEncryptionService)
    {
        _tenantRepository = tenantRepository;
        _connectionRepository = connectionRepository;
        _secretEncryptionService = secretEncryptionService;
    }

    public async Task<TenantMyPosConnectionDto?> GetByTenantIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await EnsureTenantExistsAsync(tenantId, cancellationToken);

        var connection = await _connectionRepository.GetByTenantIdAsync(tenantId, cancellationToken);

        return connection is null ? null : Map(connection);
    }

    public async Task<TenantMyPosConnectionDto> CreateAsync(
        Guid tenantId,
        CreateTenantMyPosConnectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await EnsureTenantExistsAsync(tenantId, cancellationToken);

        var existing = await _connectionRepository.GetByTenantIdAsync(tenantId, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException("A myPOS connection already exists for this tenant.");
        }

        if (string.IsNullOrWhiteSpace(request.ClientId))
        {
            throw new ArgumentException("ClientId is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.ClientSecret))
        {
            throw new ArgumentException("ClientSecret is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            throw new ArgumentException("ApiKey is required.", nameof(request));
        }

        var now = DateTime.UtcNow;

        var connection = new TenantMyPosConnection
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AuthUrl = NormalizeAuthUrl(request.AuthUrl),
            TransactionsApiBaseUrl = NormalizeBaseUrl(request.TransactionsApiBaseUrl, DefaultTransactionsApiBaseUrl),
            ClientId = request.ClientId.Trim(),
            ClientSecretEncrypted = _secretEncryptionService.Encrypt(request.ClientSecret.Trim()),
            ApiKeyEncrypted = _secretEncryptionService.Encrypt(request.ApiKey.Trim()),
            IsActive = request.IsActive,
            CreatedUtc = now,
            ModifiedUtc = null,
            SnelStartBankDagboek = request.SnelStartBankDagboekId is not null
                ? new SnelStartDagboekRef(request.SnelStartBankDagboekId.Value, NormalizeNullable(request.SnelStartBankDagboekNummer) ?? string.Empty, NormalizeNullable(request.SnelStartBankDagboekNaam) ?? string.Empty)
                : null,
            SnelStartBankIban = NormalizeNullable(request.SnelStartBankIban)
        };

        await _connectionRepository.CreateAsync(connection, cancellationToken);

        return Map(connection);
    }

    public async Task<TenantMyPosConnectionDto> UpdateAsync(
        Guid tenantId,
        Guid id,
        UpdateTenantMyPosConnectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await EnsureTenantExistsAsync(tenantId, cancellationToken);

        var existing = await _connectionRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null || existing.TenantId != tenantId)
        {
            throw new KeyNotFoundException("myPOS connection not found.");
        }

        if (string.IsNullOrWhiteSpace(request.ClientId))
        {
            throw new ArgumentException("ClientId is required.", nameof(request));
        }

        existing.AuthUrl = NormalizeAuthUrl(request.AuthUrl);
        existing.TransactionsApiBaseUrl = NormalizeBaseUrl(request.TransactionsApiBaseUrl, DefaultTransactionsApiBaseUrl);
        existing.ClientId = request.ClientId.Trim();
        existing.SnelStartBankDagboek = request.SnelStartBankDagboekId is not null
            ? new SnelStartDagboekRef(request.SnelStartBankDagboekId.Value, NormalizeNullable(request.SnelStartBankDagboekNummer) ?? string.Empty, NormalizeNullable(request.SnelStartBankDagboekNaam) ?? string.Empty)
            : null;
        existing.SnelStartBankIban = NormalizeNullable(request.SnelStartBankIban);
        existing.IsActive = request.IsActive;
        existing.ModifiedUtc = DateTime.UtcNow;


        if (!string.IsNullOrWhiteSpace(request.ClientSecret))
        {
            existing.ClientSecretEncrypted = _secretEncryptionService.Encrypt(request.ClientSecret.Trim());
        }

        if (!string.IsNullOrWhiteSpace(request.ApiKey))
        {
            existing.ApiKeyEncrypted = _secretEncryptionService.Encrypt(request.ApiKey.Trim());
        }

        await _connectionRepository.UpdateAsync(existing, cancellationToken);

        return Map(existing);
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
            throw new KeyNotFoundException("myPOS connection not found.");
        }

        await _connectionRepository.DeleteAsync(id, DateTime.UtcNow, cancellationToken);
    }

    private async Task EnsureTenantExistsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            throw new KeyNotFoundException("Tenant not found.");
        }
    }

    private static TenantMyPosConnectionDto Map(TenantMyPosConnection connection)
    {
        return new TenantMyPosConnectionDto
        {
            Id = connection.Id,
            TenantId = connection.TenantId,
            AuthUrl = connection.AuthUrl,
            TransactionsApiBaseUrl = connection.TransactionsApiBaseUrl,
            ClientId = connection.ClientId,
            HasClientSecret = !string.IsNullOrWhiteSpace(connection.ClientSecretEncrypted),
            HasApiKey = !string.IsNullOrWhiteSpace(connection.ApiKeyEncrypted),
            SnelStartBankDagboekId = connection.SnelStartBankDagboek?.Id,
            SnelStartBankDagboekNummer = connection.SnelStartBankDagboek?.Code,
            SnelStartBankDagboekNaam = connection.SnelStartBankDagboek?.Naam,
            SnelStartBankIban = connection.SnelStartBankIban,
            IsActive = connection.IsActive,
            CreatedUtc = connection.CreatedUtc,
            ModifiedUtc = connection.ModifiedUtc
        };
    }
    private static string NormalizeAuthUrl(string? value)
    {
        return NormalizeBaseUrl(value, DefaultAuthUrl);
    }

    private static string NormalizeBaseUrl(string? value, string fallback)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim();

        return normalized.TrimEnd('/');
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
