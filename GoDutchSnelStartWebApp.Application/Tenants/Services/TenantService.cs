using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Application.Tenants.Dtos;
using GoDutchSnelStartWebApp.Application.Tenants.Interfaces;
using GoDutchSnelStartWebApp.Domain.Entities;
using GoDutchSnelStartWebApp.Domain.Enums;
using GoDutchSnelStartWebApp.Web.Contracts.Tenants;
using Microsoft.Extensions.Logging;

namespace GoDutchSnelStartWebApp.Application.Tenants.Services;

public sealed class TenantService : ITenantService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ILogger<TenantService> _logger;

    public TenantService(ITenantRepository tenantRepository, ILogger<TenantService> logger)
    {
        _tenantRepository = tenantRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TenantDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all tenants");

        var tenants = await _tenantRepository.GetAllAsync(cancellationToken);

        _logger.LogInformation("Retrieved {TenantCount} tenants", tenants.Count);

        return tenants
            .Select(MapToDto)
            .ToList();
    }

    public async Task<TenantDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting tenant by id {TenantId}", id);

        var tenant = await _tenantRepository.GetByIdAsync(id, cancellationToken);

        if (tenant is null)
        {
            _logger.LogWarning("Tenant {TenantId} not found", id);
            return null;
        }

        _logger.LogInformation("Tenant {TenantId} retrieved", id);

        return MapToDto(tenant);
    }

    public async Task<Guid> CreateAsync(CreateTenantRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            _logger.LogWarning("Tenant creation failed: name is required");
            throw new ArgumentException("Tenant name is required.", nameof(request.Name));
        }

        var now = DateTime.UtcNow;
        DateTime? trialStartsUtc = null;
        DateTime? trialEndsUtc = null;
        var status = Enum.TryParse<TenantStatus>(request.Status?.Trim(), ignoreCase: true, out var cs) ? cs : TenantStatus.Draft;

        if (request.IsTrial)
        {
            status = TenantStatus.Trial;
            trialStartsUtc = now;
            trialEndsUtc = now.AddDays(request.TrialDurationDays > 0 ? request.TrialDurationDays : 7);
        }
        else
        {
            trialStartsUtc = request.TrialStartsUtc;
            trialEndsUtc = request.TrialEndsUtc;
        }

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            CustomerCode = Normalize(request.CustomerCode),
            CompanyName = Normalize(request.CompanyName),
            ContactName = Normalize(request.ContactName),
            Email = Normalize(request.Email),
            Phone = Normalize(request.Phone),
            Address = Normalize(request.Address),
            PostalCode = Normalize(request.PostalCode),
            City = Normalize(request.City),
            KvkNumber = Normalize(request.KvkNumber),
            GoDutchEnabled = request.GoDutchEnabled,
            MyPosEnabled = request.MyPosEnabled,
            DefaultIban = Normalize(request.DefaultIban),
            Status = status,
            IsActive = request.IsActive ?? true,
            TrialStartsUtc = trialStartsUtc,
            TrialEndsUtc = trialEndsUtc,
            OnboardingCompletedUtc = request.OnboardingCompletedUtc,
            CreatedUtc = now,
            ModifiedUtc = null
        };

        _logger.LogInformation(
            "Creating tenant {TenantId} with name {TenantName} and status {Status}",
            tenant.Id,
            tenant.Name,
            tenant.Status);

        await _tenantRepository.CreateAsync(tenant, cancellationToken);

        _logger.LogInformation("Tenant {TenantId} created successfully", tenant.Id);

        return tenant.Id;
    }

    public async Task UpdateAsync(Guid id, UpdateTenantRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            _logger.LogWarning("Tenant update failed for {TenantId}: name is required", id);
            throw new ArgumentException("Tenant name is required.", nameof(request.Name));
        }

        _logger.LogDebug("Updating tenant {TenantId}", id);

        var existingTenant = await _tenantRepository.GetByIdAsync(id, cancellationToken);
        if (existingTenant is null)
        {
            _logger.LogWarning("Tenant update failed: tenant {TenantId} not found", id);
            throw new KeyNotFoundException("Tenant not found.");
        }

        existingTenant.Name = request.Name.Trim();
        existingTenant.CustomerCode = Normalize(request.CustomerCode);
        existingTenant.CompanyName = Normalize(request.CompanyName);
        existingTenant.ContactName = Normalize(request.ContactName);
        existingTenant.Email = Normalize(request.Email);
        existingTenant.Phone = Normalize(request.Phone);
        existingTenant.Address = Normalize(request.Address);
        existingTenant.PostalCode = Normalize(request.PostalCode);
        existingTenant.City = Normalize(request.City);
        existingTenant.KvkNumber = Normalize(request.KvkNumber);
        existingTenant.GoDutchEnabled = request.GoDutchEnabled;
        existingTenant.MyPosEnabled = request.MyPosEnabled;
        existingTenant.DefaultIban = Normalize(request.DefaultIban);
        existingTenant.Status = Enum.TryParse<TenantStatus>(request.Status?.Trim(), ignoreCase: true, out var us) ? us : TenantStatus.Draft;
        existingTenant.IsActive = request.IsActive;
        existingTenant.TrialStartsUtc = request.TrialStartsUtc;
        existingTenant.TrialEndsUtc = request.TrialEndsUtc;
        existingTenant.OnboardingCompletedUtc = request.OnboardingCompletedUtc;
        existingTenant.ModifiedUtc = DateTime.UtcNow;

        await _tenantRepository.UpdateAsync(existingTenant, cancellationToken);

        _logger.LogInformation("Tenant {TenantId} updated successfully", id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting tenant {TenantId}", id);

        var existingTenant = await _tenantRepository.GetByIdAsync(id, cancellationToken);
        if (existingTenant is null)
        {
            _logger.LogWarning("Tenant delete failed: tenant {TenantId} not found", id);
            throw new KeyNotFoundException("Tenant not found.");
        }

        await _tenantRepository.DeleteAsync(id, cancellationToken);

        _logger.LogInformation("Tenant {TenantId} deleted successfully", id);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static TenantDto MapToDto(Tenant tenant)
    {
        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            CustomerCode = tenant.CustomerCode,
            CompanyName = tenant.CompanyName,
            ContactName = tenant.ContactName,
            Email = tenant.Email,
            Phone = tenant.Phone,
            Address = tenant.Address,
            PostalCode = tenant.PostalCode,
            City = tenant.City,
            KvkNumber = tenant.KvkNumber,
            GoDutchEnabled = tenant.GoDutchEnabled,
            MyPosEnabled = tenant.MyPosEnabled,
            DefaultIban = tenant.DefaultIban,
            Status = tenant.Status.ToString(),
            IsActive = tenant.IsActive,
            TrialStartsUtc = tenant.TrialStartsUtc,
            TrialEndsUtc = tenant.TrialEndsUtc,
            OnboardingCompletedUtc = tenant.OnboardingCompletedUtc,
            CreatedUtc = tenant.CreatedUtc,
            ModifiedUtc = tenant.ModifiedUtc
        };
    }
}