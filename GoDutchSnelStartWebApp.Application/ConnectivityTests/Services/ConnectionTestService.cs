using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Application.Abstractions.Security;
using GoDutchSnelStartWebApp.Application.ConnectivityTests.Dtos;
using GoDutchSnelStartWebApp.Application.ConnectivityTests.Interfaces;
using GoDutchSnelStartWebApp.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace GoDutchSnelStartWebApp.Application.ConnectivityTests.Services;

public sealed class ConnectionTestService : IConnectionTestService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly IBankAccountSettingsRepository _bankAccountSettingsRepository;
    private readonly ISecretEncryptionService _secretEncryptionService;
    private readonly ISnelStartConnectionTestClient _snelStartConnectionTestClient;
    private readonly ILogger<ConnectionTestService> _logger;

    public ConnectionTestService(
        ITenantRepository tenantRepository,
        IBankAccountRepository bankAccountRepository,
        IBankAccountSettingsRepository bankAccountSettingsRepository,
        ISecretEncryptionService secretEncryptionService,
        ISnelStartConnectionTestClient snelStartConnectionTestClient,
        ILogger<ConnectionTestService> logger)
    {
        _tenantRepository = tenantRepository;
        _bankAccountRepository = bankAccountRepository;
        _bankAccountSettingsRepository = bankAccountSettingsRepository;
        _secretEncryptionService = secretEncryptionService;
        _snelStartConnectionTestClient = snelStartConnectionTestClient;
        _logger = logger;
    }

    public async Task<ConnectionTestResultDto> TestSnelStartAsync(Guid tenantId, Guid bankAccountId, CancellationToken cancellationToken = default)
    {
        var settings = await GetValidatedSettingsAsync(tenantId, bankAccountId, cancellationToken);

        if (string.IsNullOrWhiteSpace(settings.SnelStartAuthUrl))
        {
            throw new ArgumentException("SnelStartAuthUrl is required.");
        }

        if (string.IsNullOrWhiteSpace(settings.SnelStartApiBaseUrl))
        {
            throw new ArgumentException("SnelStartApiBaseUrl is required.");
        }

        if (string.IsNullOrWhiteSpace(settings.SnelStartClientKey))
        {
            throw new ArgumentException("SnelStartClientKey is required.");
        }

        if (string.IsNullOrWhiteSpace(settings.SnelStartSubscriptionKeyEncrypted))
        {
            throw new ArgumentException("SnelStart subscription key is required.");
        }

        var subscriptionKey = _secretEncryptionService.Decrypt(settings.SnelStartSubscriptionKeyEncrypted);

        _logger.LogInformation("Testing SnelStart connectivity for bank account {BankAccountId}", bankAccountId);

        return await _snelStartConnectionTestClient.TestAsync(
            settings.SnelStartAuthUrl,
            settings.SnelStartApiBaseUrl,
            settings.SnelStartClientKey,
            subscriptionKey,
            cancellationToken);
    }

    private async Task<BankAccountSetting> GetValidatedSettingsAsync(
        Guid tenantId,
        Guid bankAccountId,
        CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            throw new KeyNotFoundException("Tenant not found.");
        }

        var bankAccount = await _bankAccountRepository.GetByIdAsync(bankAccountId, cancellationToken);
        if (bankAccount is null || bankAccount.TenantId != tenantId)
        {
            throw new KeyNotFoundException("Bank account not found.");
        }

        var settings = await _bankAccountSettingsRepository.GetByBankAccountIdAsync(bankAccountId, cancellationToken);
        if (settings is null)
        {
            throw new KeyNotFoundException("Bank account settings not found.");
        }

        return settings;
    }
}
