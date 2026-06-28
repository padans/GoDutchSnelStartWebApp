using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Application.Abstractions.Security;
using GoDutchSnelStartWebApp.Application.SnelStartAdministrations.Dtos;
using GoDutchSnelStartWebApp.Application.SnelStartAdministrations.Interfaces;
using GoDutchSnelStartWebApp.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace GoDutchSnelStartWebApp.Application.SnelStartAdministrations.Services;

public sealed class SnelStartAdministrationService : ISnelStartAdministrationService
{
    private readonly ISnelStartAdministrationRepository _repository;
    private readonly ISecretEncryptionService _encryptionService;
    private readonly ILogger<SnelStartAdministrationService> _logger;

    public SnelStartAdministrationService(
        ISnelStartAdministrationRepository repository,
        ISecretEncryptionService encryptionService,
        ILogger<SnelStartAdministrationService> logger)
    {
        _repository = repository;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<SnelStartAdministrationDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var administration = await _repository.GetByIdAsync(id, cancellationToken);

        if (administration is null)
        {
            return null;
        }

        return MapToDto(administration);
    }

    public async Task<IReadOnlyList<SnelStartAdministrationDto>> GetByTenantIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var administrations = await _repository.GetByTenantIdAsync(tenantId, cancellationToken);

        return administrations
            .Select(MapToDto)
            .ToList();
    }

    public async Task<Guid> CreateAsync(
        CreateSnelStartAdministrationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.TenantId == Guid.Empty)
        {
            throw new InvalidOperationException("TenantId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.AdministrationClientKey))
        {
            throw new InvalidOperationException("ClientKey is required.");
        }

        var encryptedClientKey = _encryptionService.Encrypt(request.AdministrationClientKey);

        if (string.IsNullOrWhiteSpace(encryptedClientKey))
        {
            throw new InvalidOperationException("ClientKey could not be encrypted.");
        }

        var administration = new SnelStartAdministration
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Name = request.Name.Trim(),
            AdministrationClientKeyEncrypted = encryptedClientKey,
            IsActive = request.IsActive
        };

        await _repository.CreateAsync(administration, cancellationToken);

        _logger.LogInformation(
            "SnelStartAdministration created. AdministrationId: {AdministrationId}, TenantId: {TenantId}, Name: {Name}.",
            administration.Id,
            administration.TenantId,
            administration.Name);

        return administration.Id;
    }

    public async Task UpdateAsync(
        UpdateSnelStartAdministrationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.Id == Guid.Empty)
        {
            throw new InvalidOperationException("Id is required.");
        }

        if (request.TenantId == Guid.Empty)
        {
            throw new InvalidOperationException("TenantId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Name is required.");
        }

        var existing = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (existing is null)
        {
            throw new InvalidOperationException("SnelStartAdministration not found.");
        }

        existing.TenantId = request.TenantId;
        existing.Name = request.Name.Trim();
        existing.IsActive = request.IsActive;

        if (!string.IsNullOrWhiteSpace(request.AdministrationClientKey))
        {
            var encryptedClientKey = _encryptionService.Encrypt(request.AdministrationClientKey);

            if (string.IsNullOrWhiteSpace(encryptedClientKey))
            {
                throw new InvalidOperationException("ClientKey could not be encrypted.");
            }

            existing.AdministrationClientKeyEncrypted = encryptedClientKey;
        }

        await _repository.UpdateAsync(existing, cancellationToken);

        _logger.LogInformation(
            "SnelStartAdministration updated. AdministrationId: {AdministrationId}, TenantId: {TenantId}, Name: {Name}.",
            existing.Id,
            existing.TenantId,
            existing.Name);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new InvalidOperationException("Id is required.");
        }

        await _repository.DeleteAsync(id, cancellationToken);

        _logger.LogInformation(
            "SnelStartAdministration deleted. AdministrationId: {AdministrationId}.",
            id);
    }

    private static SnelStartAdministrationDto MapToDto(SnelStartAdministration administration)
    {
        return new SnelStartAdministrationDto
        {
            Id = administration.Id,
            TenantId = administration.TenantId,
            Name = administration.Name,
            IsActive = administration.IsActive,
            HasClientKey = !string.IsNullOrWhiteSpace(administration.AdministrationClientKeyEncrypted),
            CreatedUtc = administration.CreatedUtc,
            ModifiedUtc = administration.ModifiedUtc
        };
    }
}