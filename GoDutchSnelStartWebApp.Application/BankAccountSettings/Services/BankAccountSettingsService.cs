using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Application.Abstractions.Security;
using GoDutchSnelStartWebApp.Application.BankAccountSettings.Dtos;
using GoDutchSnelStartWebApp.Application.BankAccountSettings.Interfaces;
using GoDutchSnelStartWebApp.Application.ConnectivityTests.Dtos;
using GoDutchSnelStartWebApp.Application.ConnectivityTests.Interfaces;
using GoDutchSnelStartWebApp.Domain.Entities;
using GoDutchSnelStartWebApp.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace GoDutchSnelStartWebApp.Application.BankAccountSettings.Services;

public sealed class BankAccountSettingsService : IBankAccountSettingsService
{
    private readonly IBankAccountSettingsRepository _settingsRepository;
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ISecretEncryptionService _secretEncryptionService;
    private readonly ISnelStartConnectionTestClient _snelStartConnectionTestClient;
    private readonly ILogger<BankAccountSettingsService> _logger;

    public BankAccountSettingsService(
        IBankAccountSettingsRepository settingsRepository,
        IBankAccountRepository bankAccountRepository,
        ITenantRepository tenantRepository,
        ISecretEncryptionService secretEncryptionService,
        ISnelStartConnectionTestClient snelStartConnectionTestClient,
        ILogger<BankAccountSettingsService> logger)
    {
        _settingsRepository = settingsRepository;
        _bankAccountRepository = bankAccountRepository;
        _tenantRepository = tenantRepository;
        _secretEncryptionService = secretEncryptionService;
        _snelStartConnectionTestClient = snelStartConnectionTestClient;
        _logger = logger;
    }

    public async Task<BankAccountSettingsDto?> GetByBankAccountIdAsync(
        Guid tenantId,
        Guid bankAccountId,
        CancellationToken cancellationToken = default)
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

        var settings = await _settingsRepository.GetByBankAccountIdAsync(bankAccountId, cancellationToken);
        if (settings is null)
        {
            return null;
        }

        return MapToDto(settings);
    }

    public async Task<Guid> CreateAsync(
        Guid tenantId,
        Guid bankAccountId,
        CreateBankAccountSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateCreateSnelStartCredentials(request);
        var exportFormat = ParseExportFormat(request.ExportFormat);

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

        var existing = await _settingsRepository.GetByBankAccountIdAsync(bankAccountId, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException("Settings already exist for this bank account.");
        }

        var settings = new BankAccountSetting
        {
            Id = Guid.NewGuid(),
            BankAccountId = bankAccountId,

            SnelStartAuthUrl = NormalizeSnelStartAuthUrl(request.SnelStartAuthUrl),
            SnelStartApiBaseUrl = NormalizeSnelStartApiBaseUrl(request.SnelStartApiBaseUrl),
            SnelStartClientKey = Normalize(request.SnelStartClientKey),
            SnelStartSubscriptionKeyEncrypted = string.IsNullOrWhiteSpace(request.SnelStartSubscriptionKey)
                ? null
                : _secretEncryptionService.Encrypt(request.SnelStartSubscriptionKey),

            ExportFormat = exportFormat,
            SyncEnabled = request.SyncEnabled
        };

        _logger.LogInformation(
            "Creating settings {SettingsId} for bank account {BankAccountId}",
            settings.Id,
            bankAccountId);

        await _settingsRepository.CreateAsync(settings, cancellationToken);

        _logger.LogInformation(
            "Settings {SettingsId} created successfully",
            settings.Id);

        return settings.Id;
    }

    public async Task UpdateAsync(
        Guid tenantId,
        Guid bankAccountId,
        Guid id,
        UpdateBankAccountSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var exportFormat = ParseExportFormat(request.ExportFormat);

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

        var existing = await _settingsRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null || existing.BankAccountId != bankAccountId)
        {
            throw new KeyNotFoundException("Bank account settings not found.");
        }

        ValidateUpdateSnelStartCredentials(existing, request);

        existing.SnelStartAuthUrl = NormalizeSnelStartAuthUrl(request.SnelStartAuthUrl);
        existing.SnelStartApiBaseUrl = NormalizeSnelStartApiBaseUrl(request.SnelStartApiBaseUrl);
        existing.SnelStartClientKey = Normalize(request.SnelStartClientKey);

        if (!string.IsNullOrWhiteSpace(request.SnelStartSubscriptionKey))
        {
            existing.SnelStartSubscriptionKeyEncrypted = _secretEncryptionService.Encrypt(request.SnelStartSubscriptionKey);
        }

        existing.ExportFormat = exportFormat;
        existing.SyncEnabled = request.SyncEnabled;

        await _settingsRepository.UpdateAsync(existing, cancellationToken);

        _logger.LogInformation(
            "Settings {SettingsId} updated successfully",
            id);
    }

    public async Task<ConnectionTestResultDto> TestSnelStartAsync(
        Guid tenantId,
        Guid bankAccountId,
        CancellationToken cancellationToken = default)
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

        var settings = await _settingsRepository.GetByBankAccountIdAsync(bankAccountId, cancellationToken);
        if (settings is null)
        {
            return new ConnectionTestResultDto
            {
                Success = false,
                Provider = "SnelStart",
                Message = "Er zijn nog geen SnelStart instellingen opgeslagen voor dit bankaccount."
            };
        }

        var decryptedSubscriptionKey = DecryptOptionalSecret(settings.SnelStartSubscriptionKeyEncrypted);

        var authUrl = NormalizeSnelStartAuthUrl(settings.SnelStartAuthUrl);
        var apiBaseUrl = NormalizeSnelStartApiBaseUrl(settings.SnelStartApiBaseUrl);

        _logger.LogInformation(
            "SnelStart verbindingstest gestart. TenantId: {TenantId}, BankAccountId: {BankAccountId}, ApiBaseUrl: {ApiBaseUrl}",
            tenantId,
            bankAccountId,
            apiBaseUrl);

        var result = await _snelStartConnectionTestClient.TestAsync(
            authUrl,
            apiBaseUrl,
            settings.SnelStartClientKey,
            decryptedSubscriptionKey,
            cancellationToken);

        if (result.Success)
        {
            _logger.LogInformation(
                "SnelStart verbindingstest geslaagd. TenantId: {TenantId}, BankAccountId: {BankAccountId}, TestedUrl: {TestedUrl}",
                tenantId,
                bankAccountId,
                result.TestedUrl);
        }
        else
        {
            _logger.LogWarning(
                "SnelStart verbindingstest mislukt. TenantId: {TenantId}, BankAccountId: {BankAccountId}, Message: {Message}",
                tenantId,
                bankAccountId,
                result.Message);
        }

        return result;
    }

    public async Task DeleteAsync(
        Guid tenantId,
        Guid bankAccountId,
        Guid id,
        CancellationToken cancellationToken = default)
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

        var existing = await _settingsRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null || existing.BankAccountId != bankAccountId)
        {
            throw new KeyNotFoundException("Bank account settings not found.");
        }

        await _settingsRepository.DeleteAsync(id, cancellationToken);

        _logger.LogInformation(
            "Settings {SettingsId} deleted successfully",
            id);
    }

    private string? DecryptOptionalSecret(string? encryptedValue)
    {
        if (string.IsNullOrWhiteSpace(encryptedValue))
        {
            return null;
        }

        try
        {
            return _secretEncryptionService.Decrypt(encryptedValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Decrypten van SnelStart subscription key is mislukt.");

            return null;
        }
    }

    private static void ValidateCreateSnelStartCredentials(CreateBankAccountSettingsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SnelStartClientKey))
        {
            throw new ArgumentException("SnelStart client key is required.", nameof(request.SnelStartClientKey));
        }

        if (string.IsNullOrWhiteSpace(request.SnelStartSubscriptionKey))
        {
            throw new ArgumentException("SnelStart subscription key is required.", nameof(request.SnelStartSubscriptionKey));
        }
    }

    private static void ValidateUpdateSnelStartCredentials(
        BankAccountSetting existing,
        UpdateBankAccountSettingsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SnelStartClientKey))
        {
            throw new ArgumentException("SnelStart client key is required.", nameof(request.SnelStartClientKey));
        }

        if (string.IsNullOrWhiteSpace(existing.SnelStartSubscriptionKeyEncrypted) &&
            string.IsNullOrWhiteSpace(request.SnelStartSubscriptionKey))
        {
            throw new ArgumentException("SnelStart subscription key is required.", nameof(request.SnelStartSubscriptionKey));
        }
    }

    private static SnelStartExportFormat ParseExportFormat(string? exportFormat)
    {
        if (string.IsNullOrWhiteSpace(exportFormat) ||
            string.Equals(exportFormat, "json", StringComparison.OrdinalIgnoreCase))
        {
            return SnelStartExportFormat.Mt940;
        }

        if (!Enum.TryParse<SnelStartExportFormat>(exportFormat.Trim(), ignoreCase: true, out var format))
        {
            return SnelStartExportFormat.Mt940;
        }

        return format;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeSnelStartAuthUrl(string? value)
    {
        var authUrl = string.IsNullOrWhiteSpace(value)
            ? "https://auth.snelstart.nl/b2b/token"
            : value.Trim();

        if (authUrl.EndsWith("/b2b/token", StringComparison.OrdinalIgnoreCase))
        {
            return authUrl;
        }

        return authUrl.TrimEnd('/') + "/b2b/token";
    }

    private static string NormalizeSnelStartApiBaseUrl(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "https://b2bapi.snelstart.nl/v2"
            : value.Trim().TrimEnd('/');
    }


    private static BankAccountSettingsDto MapToDto(BankAccountSetting settings)
    {
        return new BankAccountSettingsDto
        {
            Id = settings.Id,
            BankAccountId = settings.BankAccountId,


            SnelStartAuthUrl = NormalizeSnelStartAuthUrl(settings.SnelStartAuthUrl),
            SnelStartApiBaseUrl = NormalizeSnelStartApiBaseUrl(settings.SnelStartApiBaseUrl),
            SnelStartClientKey = settings.SnelStartClientKey,

            ExportFormat = settings.ExportFormat.ToString(),
            SyncEnabled = settings.SyncEnabled,

            HasSnelStartSubscriptionKey = !string.IsNullOrWhiteSpace(settings.SnelStartSubscriptionKeyEncrypted),

            CreatedUtc = settings.CreatedUtc,
            ModifiedUtc = settings.ModifiedUtc
        };
    }
}