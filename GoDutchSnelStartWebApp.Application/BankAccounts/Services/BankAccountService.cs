using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Application.BankAccounts.Dtos;
using GoDutchSnelStartWebApp.Application.BankAccounts.Helpers;
using GoDutchSnelStartWebApp.Application.BankAccounts.Interfaces;
using GoDutchSnelStartWebApp.Domain.Entities;
using GoDutchSnelStartWebApp.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace GoDutchSnelStartWebApp.Application.BankAccounts.Services;

public sealed class BankAccountService : IBankAccountService
{
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IGoDutchImportRunRepository _importRunRepository;
    private readonly ILogger<BankAccountService> _logger;

    public BankAccountService(
        IBankAccountRepository bankAccountRepository,
        ITenantRepository tenantRepository,
        IGoDutchImportRunRepository importRunRepository,
        ILogger<BankAccountService> logger)
    {
        _bankAccountRepository = bankAccountRepository;
        _tenantRepository = tenantRepository;
        _importRunRepository = importRunRepository;
        _logger = logger;
    }

    public async Task<BankAccountDto?> GetByIdAsync(
    Guid tenantId,
    Guid bankAccountId,
    CancellationToken cancellationToken = default)
    {
        var bankAccount = await _bankAccountRepository.GetByIdAsync(
            bankAccountId,
            cancellationToken);

        if (bankAccount is null || bankAccount.TenantId != tenantId)
        {
            return null;
        }

        return new BankAccountDto
        {
            Id = bankAccount.Id,
            TenantId = bankAccount.TenantId,
            Iban = bankAccount.Iban,
            AccountName = bankAccount.AccountName,
            IsActive = bankAccount.IsActive,
            SnelStartGrootboekId = bankAccount.SnelStartGrootboek?.Id,
            SnelStartGrootboekNummer = bankAccount.SnelStartGrootboek?.Nummer,
            SnelStartGrootboekNaam = bankAccount.SnelStartGrootboek?.Naam,
            SnelStartDagboekId = bankAccount.SnelStartDagboek?.Id,
            SnelStartDagboekCode = bankAccount.SnelStartDagboek?.Code,
            SnelStartDagboekNaam = bankAccount.SnelStartDagboek?.Naam,
            CreatedUtc = bankAccount.CreatedUtc,
            ModifiedUtc = bankAccount.ModifiedUtc
        };
    }

    public async Task<IReadOnlyList<BankAccountDto>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting bank accounts for tenant {TenantId}", tenantId);

        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            _logger.LogWarning("Tenant {TenantId} not found while listing bank accounts", tenantId);
            throw new KeyNotFoundException("Tenant not found.");
        }

        var bankAccounts = await _bankAccountRepository.GetByTenantIdAsync(tenantId, cancellationToken);

        _logger.LogInformation("Retrieved {BankAccountCount} bank accounts for tenant {TenantId}", bankAccounts.Count, tenantId);

        return bankAccounts
            .Select(MapToDto)
            .ToList();
    }

    public async Task<BankAccountDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting bank account by id {BankAccountId}", id);

        var bankAccount = await _bankAccountRepository.GetByIdAsync(id, cancellationToken);
        if (bankAccount is null)
        {
            _logger.LogWarning("Bank account {BankAccountId} not found", id);
            return null;
        }

        return MapToDto(bankAccount);
    }

    public async Task<Guid> CreateAsync(Guid tenantId, CreateBankAccountRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Iban))
        {
            throw new ArgumentException("IBAN is required.", nameof(request.Iban));
        }

        if (!IbanValidator.IsValid(request.Iban.Trim()))
        {
            throw new ArgumentException("IBAN format is invalid.", nameof(request.Iban));
        }

        if (string.IsNullOrWhiteSpace(request.AccountName))
        {
            throw new ArgumentException("Account name is required.", nameof(request.AccountName));
        }

        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            throw new KeyNotFoundException("Tenant not found.");
        }

        var bankAccount = new BankAccount
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Iban = request.Iban.Trim(),
            AccountName = request.AccountName.Trim(),
            IsActive = request.IsActive,

            SnelStartGrootboek = request.SnelStartGrootboekId is not null
                ? new SnelStartGrootboekRef(request.SnelStartGrootboekId.Value, request.SnelStartGrootboekNummer ?? string.Empty, request.SnelStartGrootboekNaam ?? string.Empty)
                : null,

            SnelStartDagboek = request.SnelStartDagboekId is not null
                ? new SnelStartDagboekRef(request.SnelStartDagboekId.Value, request.SnelStartDagboekCode ?? string.Empty, request.SnelStartDagboekNaam ?? string.Empty)
                : null,

            CreatedUtc = DateTime.UtcNow
        };

        _logger.LogInformation("Creating bank account {BankAccountId} for tenant {TenantId}", bankAccount.Id, tenantId);

        await _bankAccountRepository.CreateAsync(bankAccount, cancellationToken);

        _logger.LogInformation("Bank account {BankAccountId} created successfully", bankAccount.Id);

        return bankAccount.Id;
    }

    public async Task UpdateAsync(Guid tenantId, Guid id, UpdateBankAccountRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Iban))
        {
            throw new ArgumentException("IBAN is required.", nameof(request.Iban));
        }

        if (!IbanValidator.IsValid(request.Iban.Trim()))
        {
            throw new ArgumentException("IBAN format is invalid.", nameof(request.Iban));
        }

        if (string.IsNullOrWhiteSpace(request.AccountName))
        {
            throw new ArgumentException("Account name is required.", nameof(request.AccountName));
        }

        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            throw new KeyNotFoundException("Tenant not found.");
        }

        var existingBankAccount = await _bankAccountRepository.GetByIdAsync(id, cancellationToken);
        if (existingBankAccount is null || existingBankAccount.TenantId != tenantId)
        {
            throw new KeyNotFoundException("Bank account not found.");
        }

        existingBankAccount.Iban = request.Iban.Trim();
        existingBankAccount.AccountName = request.AccountName.Trim();
        existingBankAccount.IsActive = request.IsActive;

        existingBankAccount.SnelStartGrootboek = request.SnelStartGrootboekId is not null
            ? new SnelStartGrootboekRef(request.SnelStartGrootboekId.Value, request.SnelStartGrootboekNummer ?? string.Empty, request.SnelStartGrootboekNaam ?? string.Empty)
            : null;

        existingBankAccount.SnelStartDagboek = request.SnelStartDagboekId is not null
            ? new SnelStartDagboekRef(request.SnelStartDagboekId.Value, request.SnelStartDagboekCode ?? string.Empty, request.SnelStartDagboekNaam ?? string.Empty)
            : null;

        existingBankAccount.ModifiedUtc = DateTime.UtcNow;

        await _bankAccountRepository.UpdateAsync(existingBankAccount, cancellationToken);

        _logger.LogInformation("Bank account {BankAccountId} updated successfully", id);
    }

    public async Task DeleteAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            throw new KeyNotFoundException("Tenant not found.");
        }

        var existingBankAccount = await _bankAccountRepository.GetByIdAsync(id, cancellationToken);
        if (existingBankAccount is null || existingBankAccount.TenantId != tenantId)
        {
            throw new KeyNotFoundException("Bank account not found.");
        }

        await _bankAccountRepository.DeleteAsync(id, cancellationToken);

        _logger.LogInformation("Bank account {BankAccountId} deleted successfully", id);
    }

    public async Task<BankAccountSyncStatusDto> GetSyncStatusAsync(
        Guid tenantId,
        Guid bankAccountId,
        CancellationToken cancellationToken = default)
    {
        var bankAccount = await _bankAccountRepository.GetByIdAsync(bankAccountId, cancellationToken);
        if (bankAccount is null || bankAccount.TenantId != tenantId)
        {
            throw new KeyNotFoundException("Bank account not found.");
        }

        var lastRun = await _importRunRepository.GetLastCompletedByBankAccountIdAsync(bankAccountId, cancellationToken);

        if (lastRun is null)
        {
            return new BankAccountSyncStatusDto
            {
                Status = "NeverRun",
                LastRunAt = null,
                TransactionCount = 0,
                RetryCount = 0,
                TriggerSource = null,
                Message = null
            };
        }

        return new BankAccountSyncStatusDto
        {
            Status = lastRun.Status.ToString(),
            LastRunAt = lastRun.CompletedUtc,
            TransactionCount = lastRun.TransactionCount,
            RetryCount = lastRun.RetryCount,
            TriggerSource = lastRun.TriggerSource.ToString(),
            Message = lastRun.Message
        };
    }

    private static BankAccountDto MapToDto(BankAccount bankAccount)
    {
        return new BankAccountDto
        {
            Id = bankAccount.Id,
            TenantId = bankAccount.TenantId,
            Iban = bankAccount.Iban,
            AccountName = bankAccount.AccountName,
            IsActive = bankAccount.IsActive,

            SnelStartGrootboekId = bankAccount.SnelStartGrootboek?.Id,
            SnelStartGrootboekNummer = bankAccount.SnelStartGrootboek?.Nummer,
            SnelStartGrootboekNaam = bankAccount.SnelStartGrootboek?.Naam,

            SnelStartDagboekId = bankAccount.SnelStartDagboek?.Id,
            SnelStartDagboekCode = bankAccount.SnelStartDagboek?.Code,
            SnelStartDagboekNaam = bankAccount.SnelStartDagboek?.Naam,

            CreatedUtc = bankAccount.CreatedUtc,
            ModifiedUtc = bankAccount.ModifiedUtc
        };
    }
}