using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Application.Abstractions.Repositories.MyPos;
using GoDutchSnelStartWebApp.Application.MyPos.Dtos;
using GoDutchSnelStartWebApp.Application.MyPos.Interfaces;
using GoDutchSnelStartWebApp.Domain.Entities.MyPos;
using GoDutchSnelStartWebApp.Domain.ValueObjects;

namespace GoDutchSnelStartWebApp.Application.MyPos.Services;


public sealed class MyPosTransactionTypeMappingService : IMyPosTransactionTypeMappingService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IMyPosTransactionTypeMappingRepository _mappingRepository;

    public MyPosTransactionTypeMappingService(
        ITenantRepository tenantRepository,
        IMyPosTransactionTypeMappingRepository mappingRepository)
    {
        _tenantRepository = tenantRepository;
        _mappingRepository = mappingRepository;
    }

    public async Task<IReadOnlyList<MyPosTransactionTypeMappingDto>> GetByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await EnsureTenantExistsAsync(tenantId, cancellationToken);

        var mappings = await _mappingRepository.GetByTenantAsync(tenantId, cancellationToken);

        return mappings
            .OrderBy(x => x.TransactionCode)
            .Select(Map)
            .ToList();
    }

    public async Task<MyPosTransactionTypeMappingDto> UpsertAsync(
        Guid tenantId,
        UpsertMyPosTransactionTypeMappingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await EnsureTenantExistsAsync(tenantId, cancellationToken);

        var transactionCode = NormalizeTransactionCode(request.TransactionCode);
        if (string.IsNullOrWhiteSpace(transactionCode))
        {
            throw new ArgumentException("TransactionCode is required.", nameof(request));
        }

        var now = DateTime.UtcNow;
        var existing = await _mappingRepository.GetByCodeAsync(tenantId, transactionCode, cancellationToken);

        var mapping = existing ?? new MyPosTransactionTypeMapping
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TransactionCode = transactionCode,
            CreatedUtc = now
        };

        mapping.Description = request.Description?.Trim() ?? string.Empty;
        mapping.SnelStartGrootboek = request.SnelStartGrootboekId is not null
            ? new SnelStartGrootboekRef(request.SnelStartGrootboekId.Value, request.SnelStartGrootboekNummer?.Trim() ?? string.Empty, request.SnelStartGrootboekNaam?.Trim() ?? string.Empty)
            : null;

        mapping.BtwBerekening = NormalizeBtwBerekening(request.BtwBerekening);
        mapping.BtwSoort = NormalizeNullable(request.BtwSoort);
        mapping.BtwPercentage = request.BtwPercentage;

        if (string.Equals(mapping.BtwBerekening, "Geen", StringComparison.OrdinalIgnoreCase))
        {
            mapping.BtwSoort = "Geen";
            mapping.BtwPercentage = 0m;
        }

        mapping.IsActive = request.IsActive;
        mapping.ModifiedUtc = existing is null ? null : now;

        await _mappingRepository.UpsertAsync(mapping, cancellationToken);

        return Map(mapping);
    }

    public async Task DeleteAsync(
        Guid tenantId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await EnsureTenantExistsAsync(tenantId, cancellationToken);

        var existing = await _mappingRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null || existing.TenantId != tenantId)
        {
            throw new KeyNotFoundException("myPOS transaction type mapping not found.");
        }

        await _mappingRepository.DeleteAsync(id, DateTime.UtcNow, cancellationToken);
    }

    private async Task EnsureTenantExistsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            throw new KeyNotFoundException("Tenant not found.");
        }
    }

    private static MyPosTransactionTypeMappingDto Map(MyPosTransactionTypeMapping mapping)
    {
        return new MyPosTransactionTypeMappingDto
        {
            Id = mapping.Id,
            TenantId = mapping.TenantId,
            TransactionCode = mapping.TransactionCode,
            Description = mapping.Description,
            SnelStartGrootboekId = mapping.SnelStartGrootboek?.Id,
            SnelStartGrootboekNummer = mapping.SnelStartGrootboek?.Nummer,
            SnelStartGrootboekNaam = mapping.SnelStartGrootboek?.Naam,
            BtwBerekening = mapping.BtwBerekening,
            BtwSoort = mapping.BtwSoort,
            BtwPercentage = mapping.BtwPercentage,
            IsActive = mapping.IsActive,
            CreatedUtc = mapping.CreatedUtc,
            ModifiedUtc = mapping.ModifiedUtc
        };
    }

    private static string NormalizeTransactionCode(string? transactionCode)
    {
        return string.IsNullOrWhiteSpace(transactionCode)
            ? string.Empty
            : transactionCode.Trim();
    }

    private static string NormalizeBtwBerekening(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Geen";
        }

        var normalized = value.Trim();

        return normalized switch
        {
            "Geen" => "Geen",
            "InclusiefBtw" => "InclusiefBtw",
            "ExclusiefBtw" => "ExclusiefBtw",
            _ => "Geen"
        };
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
