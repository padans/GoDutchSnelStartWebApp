using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Application.Abstractions.Repositories.SnelStart;
using GoDutchSnelStartWebApp.Application.Abstractions.Security;
using GoDutchSnelStartWebApp.Application.ConnectivityTests.Dtos;
using GoDutchSnelStartWebApp.Application.ConnectivityTests.Interfaces;
using GoDutchSnelStartWebApp.Application.Configuration;
using GoDutchSnelStartWebApp.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GoDutchSnelStartWebApp.Application.ConnectivityTests.Services;

public sealed class ConnectionTestService : IConnectionTestService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly IBankAccountSettingsRepository _bankAccountSettingsRepository;
    private readonly ITenantSnelStartConnectionRepository _tenantSnelStartConnectionRepository;
    private readonly ISecretEncryptionService _secretEncryptionService;
    private readonly ISnelStartConnectionTestClient _snelStartConnectionTestClient;
    private readonly IOptions<SnelStartGlobalOptions> _snelStartGlobal;
    private readonly ILogger<ConnectionTestService> _logger;

    public ConnectionTestService(
        ITenantRepository tenantRepository,
        IBankAccountRepository bankAccountRepository,
        IBankAccountSettingsRepository bankAccountSettingsRepository,
        ITenantSnelStartConnectionRepository tenantSnelStartConnectionRepository,
        ISecretEncryptionService secretEncryptionService,
        ISnelStartConnectionTestClient snelStartConnectionTestClient,
        IOptions<SnelStartGlobalOptions> snelStartGlobal,
        ILogger<ConnectionTestService> logger)
    {
        _tenantRepository = tenantRepository;
        _bankAccountRepository = bankAccountRepository;
        _bankAccountSettingsRepository = bankAccountSettingsRepository;
        _tenantSnelStartConnectionRepository = tenantSnelStartConnectionRepository;
        _secretEncryptionService = secretEncryptionService;
        _snelStartConnectionTestClient = snelStartConnectionTestClient;
        _snelStartGlobal = snelStartGlobal;
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

    public async Task<ConnectionTestResultDto> TestTenantSnelStartConnectionAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var connection = await _tenantSnelStartConnectionRepository.GetByTenantIdAsync(tenantId, cancellationToken);
        if (connection is null)
            return new ConnectionTestResultDto { Success = false, Provider = "SnelStart", Message = "Geen SnelStart-koppeling gevonden voor deze tenant." };

        if (string.IsNullOrWhiteSpace(connection.ClientKeyEncrypted))
            return new ConnectionTestResultDto { Success = false, Provider = "SnelStart", Message = "Maatwerksleutel is nog niet ingesteld." };

        var clientKey = _secretEncryptionService.Decrypt(connection.ClientKeyEncrypted);

        var subscriptionKey = !string.IsNullOrWhiteSpace(connection.SubscriptionKeyEncrypted)
            ? _secretEncryptionService.Decrypt(connection.SubscriptionKeyEncrypted)
            : _snelStartGlobal.Value.SubscriptionKey;

        if (string.IsNullOrWhiteSpace(subscriptionKey))
            return new ConnectionTestResultDto { Success = false, Provider = "SnelStart", Message = "SubscriptionKey is niet geconfigureerd." };

        _logger.LogInformation("SnelStart verbindingstest gestart voor tenant {TenantId}", tenantId);

        return await _snelStartConnectionTestClient.TestAsync(
            connection.AuthUrl,
            connection.ApiBaseUrl,
            clientKey,
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
