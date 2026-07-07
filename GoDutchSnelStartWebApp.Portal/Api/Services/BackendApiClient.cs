using System.Net;
using System.Net.Http.Json;
using GoDutchSnelStartWebApp.Portal.Api.Interfaces;
using GoDutchSnelStartWebApp.Portal.Models;
using GoDutchSnelStartWebApp.Portal.Models.MyPos;
using System.Net.Http.Json;

namespace GoDutchSnelStartWebApp.Portal.Api.Services;

public sealed class BackendApiClient : IBackendApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BackendApiClient> _logger;

    public BackendApiClient(
        HttpClient httpClient,
        ILogger<BackendApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BankAccountViewModel>> GetBankAccountsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/tenants/{tenantId}/bankaccounts";

        _logger.LogInformation("Ophalen bankaccounts via {Url}", url);

        return await ExecuteWithRetryAsync(
                   async ct =>
                   {
                       var result = await _httpClient.GetFromJsonAsync<List<BankAccountViewModel>>(url, ct);
                       return (IReadOnlyList<BankAccountViewModel>)(result ?? []);
                   },
                   operationName: "bankaccounts ophalen",
                   cancellationToken)
               ?? [];
    }

    public async Task<BankAccountViewModel?> GetBankAccountByIdAsync(
        Guid tenantId,
        Guid bankAccountId,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/tenants/{tenantId}/bankaccounts/{bankAccountId}";

        _logger.LogInformation("Ophalen bankaccount via {Url}", url);

        return await ExecuteWithRetryAsync(
            ct => _httpClient.GetFromJsonAsync<BankAccountViewModel>(url, ct),
            operationName: $"bankaccount ophalen {bankAccountId}",
            cancellationToken);
    }

    public async Task<BankAccountSyncStatusViewModel?> GetSyncStatusAsync(
        Guid tenantId,
        Guid bankAccountId,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/tenants/{tenantId}/bankaccounts/{bankAccountId}/sync-status";

        _logger.LogInformation("Ophalen sync-status via {Url}", url);

        return await ExecuteWithRetryAsync(
            ct => _httpClient.GetFromJsonAsync<BankAccountSyncStatusViewModel>(url, ct),
            operationName: $"sync-status ophalen voor bankaccount {bankAccountId}",
            cancellationToken);
    }

    public async Task<BankAccountResyncResultViewModel> ForceResyncAsync(
        Guid tenantId,
        Guid bankAccountId,
        DateTime fromUtc,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/tenants/{tenantId}/bankaccounts/{bankAccountId}/resync";

        _logger.LogInformation(
            "Force resync via {Url} vanaf {FromUtc}",
            url,
            fromUtc);

        var request = new ForceResyncRequestViewModel
        {
            FromUtc = fromUtc
        };

        var result = await ExecuteWithRetryAsync(
            async ct =>
            {
                using var response = await _httpClient.PostAsJsonAsync(url, request, ct);
                response.EnsureSuccessStatusCode();

                var responseResult = await response.Content.ReadFromJsonAsync<BankAccountResyncResultViewModel>(
                    cancellationToken: ct);

                return responseResult ?? throw new InvalidOperationException("Backend heeft geen resync-resultaat teruggegeven.");
            },
            operationName: $"force resync voor bankaccount {bankAccountId}",
            cancellationToken);

        return result ?? throw new InvalidOperationException("Force resync failed.");
    }

    public async Task UpdateBankAccountSnelStartSettingsAsync(
        Guid tenantId,
        Guid bankAccountId,
        UpdateBankAccountSnelStartSettingsRequestViewModel request,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/tenants/{tenantId}/bankaccounts/{bankAccountId}";

        _logger.LogInformation(
            "SnelStart bankaccountinstellingen opslaan via {Url}",
            url);

        await ExecuteWithRetryAsync<object?>(
            async ct =>
            {
                using var response = await _httpClient.PutAsJsonAsync(url, request, ct);
                response.EnsureSuccessStatusCode();
                return null;
            },
            operationName: $"SnelStart instellingen opslaan voor bankaccount {bankAccountId}",
            cancellationToken);
    }

    public async Task<BankAccountSettingsViewModel?> GetBankAccountSettingsAsync(
        Guid tenantId,
        Guid bankAccountId,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/tenants/{tenantId}/bankaccounts/{bankAccountId}/settings";

        _logger.LogInformation("Ophalen bankaccount settings via {Url}", url);

        return await ExecuteWithRetryAsync(
            async ct =>
            {
                using var response = await _httpClient.GetAsync(url, ct);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<BankAccountSettingsViewModel>(
                    cancellationToken: ct);
            },
            operationName: $"bankaccount settings ophalen voor bankaccount {bankAccountId}",
            cancellationToken);
    }

    public async Task SaveBankAccountSettingsAsync(
        Guid tenantId,
        Guid bankAccountId,
        BankAccountSettingsViewModel request,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = $"api/tenants/{tenantId}/bankaccounts/{bankAccountId}/settings";

        _logger.LogInformation(
            "Bankaccount settings opslaan voor BankAccountId {BankAccountId}",
            bankAccountId);

        await ExecuteWithRetryAsync<object?>(
            async ct =>
            {
                HttpResponseMessage response;

                if (request.Id.HasValue && request.Id.Value != Guid.Empty)
                {
                    response = await _httpClient.PutAsJsonAsync(
                        $"{baseUrl}/{request.Id.Value}",
                        request,
                        ct);
                }
                else
                {
                    response = await _httpClient.PostAsJsonAsync(
                        baseUrl,
                        request,
                        ct);
                }

                using (response)
                {
                    response.EnsureSuccessStatusCode();
                }

                return null;
            },
            operationName: $"bankaccount settings opslaan voor bankaccount {bankAccountId}",
            cancellationToken);
    }

    public async Task<ConnectionTestResultViewModel> TestSnelStartAsync(
        Guid tenantId,
        Guid bankAccountId,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/tenants/{tenantId}/bankaccounts/{bankAccountId}/settings/test-snelstart";

        _logger.LogInformation(
            "SnelStart verbindingstest starten via {Url}",
            url);

        return await ExecuteWithRetryAsync(
                   async ct =>
                   {
                       using var response = await _httpClient.PostAsync(url, content: null, ct);
                       response.EnsureSuccessStatusCode();

                       var result = await response.Content.ReadFromJsonAsync<ConnectionTestResultViewModel>(
                           cancellationToken: ct);

                       return result ?? new ConnectionTestResultViewModel
                       {
                           Success = false,
                           Provider = "SnelStart",
                           Message = "Geen testresultaat ontvangen van de backend."
                       };
                   },
                   operationName: $"SnelStart test voor bankaccount {bankAccountId}",
                   cancellationToken)
               ?? new ConnectionTestResultViewModel
               {
                   Success = false,
                   Provider = "SnelStart",
                   Message = "Geen testresultaat ontvangen van de backend."
               };
    }

    public async Task<IReadOnlyList<SnelStartDagboekLookupViewModel>> GetSnelStartDagboekenAsync(
    Guid tenantId,
    Guid bankAccountId,
    CancellationToken cancellationToken = default)
    {
        var url = $"api/tenants/{tenantId}/bankaccounts/{bankAccountId}/snelstart/lookups/dagboeken";

        _logger.LogInformation(
            "SnelStart dagboeken ophalen via {Url}",
            url);

        return await ExecuteWithRetryAsync(
                   async ct =>
                   {
                       var result = await _httpClient.GetFromJsonAsync<List<SnelStartDagboekLookupViewModel>>(
                           url,
                           ct);

                       return (IReadOnlyList<SnelStartDagboekLookupViewModel>)(result ?? []);
                   },
                   operationName: $"SnelStart dagboeken ophalen voor bankaccount {bankAccountId}",
                   cancellationToken)
               ?? [];
    }

    public async Task<IReadOnlyList<SnelStartGrootboekLookupViewModel>> GetSnelStartGrootboekenAsync(
        Guid tenantId,
        Guid bankAccountId,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/tenants/{tenantId}/bankaccounts/{bankAccountId}/snelstart/lookups/grootboeken";

        _logger.LogInformation(
            "SnelStart grootboeken ophalen via {Url}",
            url);

        return await ExecuteWithRetryAsync(
                   async ct =>
                   {
                       var result = await _httpClient.GetFromJsonAsync<List<SnelStartGrootboekLookupViewModel>>(
                           url,
                           ct);

                       return (IReadOnlyList<SnelStartGrootboekLookupViewModel>)(result ?? []);
                   },
                   operationName: $"SnelStart grootboeken ophalen voor bankaccount {bankAccountId}",
                   cancellationToken)
               ?? [];
    }



    public async Task<IReadOnlyList<SnelStartAdministrationViewModel>> GetSnelStartAdministrationsByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/snelstart-administrations/by-tenant/{tenantId}";

        _logger.LogInformation("SnelStart administraties ophalen via {Url}", url);

        return await ExecuteWithRetryAsync(
                   async ct =>
                   {
                       var result = await _httpClient.GetFromJsonAsync<List<SnelStartAdministrationViewModel>>(url, ct);
                       return (IReadOnlyList<SnelStartAdministrationViewModel>)(result ?? []);
                   },
                   operationName: $"SnelStart administraties ophalen voor tenant {tenantId}",
                   cancellationToken)
               ?? [];
    }

    public async Task<Guid> CreateSnelStartAdministrationAsync(
        CreateSnelStartAdministrationRequestViewModel request,
        CancellationToken cancellationToken = default)
    {
        const string url = "api/snelstart-administrations";

        _logger.LogInformation(
            "SnelStart administratie technisch aanmaken via {Url} voor TenantId {TenantId}",
            url,
            request.TenantId);

        var result = await ExecuteWithRetryAsync(
            async ct =>
            {
                using var response = await _httpClient.PostAsJsonAsync(url, request, ct);
                response.EnsureSuccessStatusCode();

                var createResult = await response.Content.ReadFromJsonAsync<CreateBankAccountResponseViewModel>(
                    cancellationToken: ct);

                if (createResult is null || createResult.Id == Guid.Empty)
                {
                    throw new InvalidOperationException("Backend heeft geen geldige SnelStart-administratie-id teruggegeven.");
                }

                return createResult.Id;
            },
            operationName: $"SnelStart administratie aanmaken voor tenant {request.TenantId}",
            cancellationToken);

        return result == Guid.Empty
            ? throw new InvalidOperationException("Creating SnelStart administration failed.")
            : result;
    }

    public async Task UpdateSnelStartAdministrationAsync(
        Guid id,
        UpdateSnelStartAdministrationRequestViewModel request,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/snelstart-administrations/{id}";

        _logger.LogInformation(
            "SnelStart administratie technisch bijwerken via {Url} voor TenantId {TenantId}",
            url,
            request.TenantId);

        await ExecuteWithRetryAsync<object?>(
            async ct =>
            {
                using var response = await _httpClient.PutAsJsonAsync(url, request, ct);
                response.EnsureSuccessStatusCode();
                return null;
            },
            operationName: $"SnelStart administratie bijwerken {id}",
            cancellationToken);
    }

    public async Task<BankAccountSnelStartLinkViewModel?> GetBankAccountSnelStartLinkByBankAccountIdAsync(
        Guid bankAccountId,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/bankaccount-snelstart-links/by-bankaccount/{bankAccountId}";

        _logger.LogInformation("SnelStart link ophalen via {Url}", url);

        return await ExecuteWithRetryAsync(
            async ct =>
            {
                using var response = await _httpClient.GetAsync(url, ct);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<BankAccountSnelStartLinkViewModel>(
                    cancellationToken: ct);
            },
            operationName: $"SnelStart link ophalen voor bankaccount {bankAccountId}",
            cancellationToken);
    }

    public async Task<Guid> CreateBankAccountSnelStartLinkAsync(
        CreateBankAccountSnelStartLinkRequestViewModel request,
        CancellationToken cancellationToken = default)
    {
        var url = "api/bankaccount-snelstart-links";

        _logger.LogInformation(
            "SnelStart link aanmaken via {Url} voor BankAccountId {BankAccountId}",
            url,
            request.BankAccountId);

        var result = await ExecuteWithRetryAsync(
            async ct =>
            {
                using var response = await _httpClient.PostAsJsonAsync(url, request, ct);
                response.EnsureSuccessStatusCode();

                var createResult = await response.Content.ReadFromJsonAsync<CreateBankAccountResponseViewModel>(
                    cancellationToken: ct);

                if (createResult is null || createResult.Id == Guid.Empty)
                {
                    throw new InvalidOperationException("Backend heeft geen geldig SnelStart-link-id teruggegeven.");
                }

                return createResult.Id;
            },
            operationName: $"SnelStart link aanmaken voor bankaccount {request.BankAccountId}",
            cancellationToken);

        return result == Guid.Empty
            ? throw new InvalidOperationException("Creating SnelStart link failed.")
            : result;
    }

    public async Task UpdateBankAccountSnelStartLinkAsync(
        Guid id,
        UpdateBankAccountSnelStartLinkRequestViewModel request,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/bankaccount-snelstart-links/{id}";

        _logger.LogInformation(
            "SnelStart link bijwerken via {Url} voor BankAccountId {BankAccountId}",
            url,
            request.BankAccountId);

        await ExecuteWithRetryAsync<object?>(
            async ct =>
            {
                using var response = await _httpClient.PutAsJsonAsync(url, request, ct);
                response.EnsureSuccessStatusCode();
                return null;
            },
            operationName: $"SnelStart link bijwerken voor bankaccount {request.BankAccountId}",
            cancellationToken);
    }


    public async Task<IReadOnlyList<SnelStartGrootboekLookupViewModel>> GetTenantSnelStartGrootboekenAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/tenants/{tenantId}/snelstart/lookups/grootboeken";

        _logger.LogInformation(
            "Tenantgerichte SnelStart grootboeken ophalen via {Url}",
            url);

        return await ExecuteWithRetryAsync(
                   async ct =>
                   {
                       var result = await _httpClient.GetFromJsonAsync<List<SnelStartGrootboekLookupViewModel>>(
                           url,
                           ct);

                       return (IReadOnlyList<SnelStartGrootboekLookupViewModel>)(result ?? []);
                   },
                   operationName: $"tenantgerichte SnelStart grootboeken ophalen voor tenant {tenantId}",
                   cancellationToken)
               ?? [];
    }

    public async Task<SnelStartGrootboekLookupViewModel> CreateTenantSnelStartGrootboekAsync(
        Guid tenantId,
        CreateSnelStartGrootboekRequestViewModel request,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/tenants/{tenantId}/snelstart/lookups/grootboeken";

        _logger.LogInformation(
            "Tenantgericht SnelStart grootboek aanmaken via {Url}. Nummer: {Nummer}",
            url,
            request.Nummer);

        var result = await ExecuteWithRetryAsync(
            async ct =>
            {
                using var response = await _httpClient.PostAsJsonAsync(url, request, ct);
                response.EnsureSuccessStatusCode();

                var created = await response.Content.ReadFromJsonAsync<SnelStartGrootboekLookupViewModel>(
                    cancellationToken: ct);

                return created ?? throw new InvalidOperationException("Backend heeft geen geldig SnelStart grootboek teruggegeven.");
            },
            operationName: $"tenantgericht SnelStart grootboek aanmaken voor tenant {tenantId}",
            cancellationToken);

        return result ?? throw new InvalidOperationException("Creating SnelStart general ledger account failed.");
    }


    public async Task<IReadOnlyList<MyPosTransactionTypeMappingViewModel>> GetMyPosTransactionTypeMappingsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/tenants/{tenantId}/mypos/transaction-type-mappings";

        _logger.LogInformation(
            "myPOS transactietype mappings ophalen via {Url}",
            url);

        return await ExecuteWithRetryAsync(
                   async ct =>
                   {
                       var result = await _httpClient.GetFromJsonAsync<List<MyPosTransactionTypeMappingViewModel>>(
                           url,
                           ct);

                       return (IReadOnlyList<MyPosTransactionTypeMappingViewModel>)(result ?? []);
                   },
                   operationName: $"myPOS transactietype mappings ophalen voor tenant {tenantId}",
                   cancellationToken)
               ?? [];
    }

    public async Task<MyPosTransactionTypeMappingViewModel> UpsertMyPosTransactionTypeMappingAsync(
        Guid tenantId,
        UpsertMyPosTransactionTypeMappingRequestViewModel request,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/tenants/{tenantId}/mypos/transaction-type-mappings";

        _logger.LogInformation(
            "myPOS transactietype mapping opslaan via {Url} voor code {TransactionCode}",
            url,
            request.TransactionCode);

        var result = await ExecuteWithRetryAsync(
            async ct =>
            {
                using var response = await _httpClient.PostAsJsonAsync(url, request, ct);
                response.EnsureSuccessStatusCode();

                var mapping = await response.Content.ReadFromJsonAsync<MyPosTransactionTypeMappingViewModel>(
                    cancellationToken: ct);

                return mapping ?? throw new InvalidOperationException("Backend heeft geen myPOS mapping teruggegeven.");
            },
            operationName: $"myPOS transactietype mapping opslaan voor code {request.TransactionCode}",
            cancellationToken);

        return result ?? throw new InvalidOperationException("Saving myPOS transaction type mapping failed.");
    }


    public async Task<TenantMyPosConnectionViewModel?> GetTenantMyPosConnectionAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/tenants/{tenantId}/mypos/connection";

        _logger.LogInformation(
            "myPOS connection ophalen via {Url}",
            url);

        return await ExecuteWithRetryAsync(
            async ct =>
            {
                using var response = await _httpClient.GetAsync(url, ct);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<TenantMyPosConnectionViewModel>(
                    cancellationToken: ct);
            },
            operationName: $"myPOS connection ophalen voor tenant {tenantId}",
            cancellationToken);
    }

    public async Task<MyPosTransactionImportResultViewModel> ImportMyPosTransactionsAsync(
        Guid tenantId,
        Guid tenantMyPosConnectionId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var url =
            $"api/tenants/{tenantId}/mypos/transactions/import" +
            $"?tenantMyPosConnectionId={tenantMyPosConnectionId}" +
            $"&fromUtc={Uri.EscapeDataString(fromUtc.ToUniversalTime().ToString("O"))}" +
            $"&toUtc={Uri.EscapeDataString(toUtc.ToUniversalTime().ToString("O"))}";

        _logger.LogInformation(
            "myPOS transacties importeren via {Url}",
            url);

        var result = await ExecuteWithRetryAsync(
            async ct =>
            {
                using var response = await _httpClient.PostAsync(url, content: null, ct);
                response.EnsureSuccessStatusCode();

                var importResult = await response.Content.ReadFromJsonAsync<MyPosTransactionImportResultViewModel>(
                    cancellationToken: ct);

                return importResult ?? throw new InvalidOperationException("Backend heeft geen myPOS importresultaat teruggegeven.");
            },
            operationName: $"myPOS transacties importeren voor tenant {tenantId}",
            cancellationToken);

        return result ?? throw new InvalidOperationException("Importing myPOS transactions failed.");
    }

    public async Task<IReadOnlyList<MyPosRawTransactionViewModel>> GetMyPosRawTransactionsAsync(
        Guid tenantId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var url =
            $"api/tenants/{tenantId}/mypos/transactions" +
            $"?fromUtc={Uri.EscapeDataString(fromUtc.ToUniversalTime().ToString("O"))}" +
            $"&toUtc={Uri.EscapeDataString(toUtc.ToUniversalTime().ToString("O"))}";

        _logger.LogInformation(
            "myPOS raw transacties ophalen via {Url}",
            url);

        return await ExecuteWithRetryAsync(
                   async ct =>
                   {
                       var result = await _httpClient.GetFromJsonAsync<List<MyPosRawTransactionViewModel>>(
                           url,
                           ct);

                       return (IReadOnlyList<MyPosRawTransactionViewModel>)(result ?? []);
                   },
                   operationName: $"myPOS raw transacties ophalen voor tenant {tenantId}",
                   cancellationToken)
               ?? [];
    }


    public async Task<IReadOnlyList<MyPosTransactionTotalViewModel>> GetMyPosTransactionTotalsAsync(
        Guid tenantId,
        DateTime fromUtc,
        DateTime toUtc,
        bool includeExported = false,
        CancellationToken cancellationToken = default)
    {
        var url =
            $"api/tenants/{tenantId}/mypos/transaction-totals" +
            $"?fromUtc={Uri.EscapeDataString(fromUtc.ToUniversalTime().ToString("O"))}" +
            $"&toUtc={Uri.EscapeDataString(toUtc.ToUniversalTime().ToString("O"))}" +
            $"&includeExported={includeExported.ToString().ToLowerInvariant()}";

        _logger.LogInformation(
            "myPOS totalisaties ophalen via {Url}",
            url);

        return await ExecuteWithRetryAsync(
                   async ct =>
                   {
                       var result = await _httpClient.GetFromJsonAsync<List<MyPosTransactionTotalViewModel>>(
                           url,
                           ct);

                       return (IReadOnlyList<MyPosTransactionTotalViewModel>)(result ?? []);
                   },
                   operationName: $"myPOS totalisaties ophalen voor tenant {tenantId}",
                   cancellationToken)
               ?? [];
    }

    private async Task<T?> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T?>> action,
        string operationName,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;

        var delays = new[]
        {
            TimeSpan.FromMilliseconds(300),
            TimeSpan.FromMilliseconds(800)
        };

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await action(cancellationToken);
            }
            catch (HttpRequestException ex) when (attempt < maxAttempts)
            {
                _logger.LogWarning(
                    ex,
                    "Poging {Attempt}/{MaxAttempts} mislukt bij {OperationName}. Nieuwe poging volgt.",
                    attempt,
                    maxAttempts,
                    operationName);

                await Task.Delay(delays[attempt - 1], cancellationToken);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested && attempt < maxAttempts)
            {
                _logger.LogWarning(
                    ex,
                    "Timeout bij poging {Attempt}/{MaxAttempts} voor {OperationName}. Nieuwe poging volgt.",
                    attempt,
                    maxAttempts,
                    operationName);

                await Task.Delay(delays[attempt - 1], cancellationToken);
            }
        }

        return await action(cancellationToken);
    }
    public async Task<Guid> CreateBankAccountAsync(
     Guid tenantId,
     CreateBankAccountRequestViewModel request,
     CancellationToken cancellationToken = default)
    {
        var url = $"api/tenants/{tenantId}/bankaccounts";

        _logger.LogInformation(
            "Nieuw bankaccount aanmaken via {Url}",
            url);

        var result = await ExecuteWithRetryAsync(
            async ct =>
            {
                using var response = await _httpClient.PostAsJsonAsync(url, request, ct);
                response.EnsureSuccessStatusCode();

                var createResult = await response.Content.ReadFromJsonAsync<CreateBankAccountResponseViewModel>(
                    cancellationToken: ct);

                if (createResult is null || createResult.Id == Guid.Empty)
                {
                    throw new InvalidOperationException("Backend heeft geen geldig bankaccount-id teruggegeven.");
                }

                return createResult.Id;
            },
            operationName: "bankaccount aanmaken",
            cancellationToken);

        if (result == Guid.Empty)
        {
            throw new InvalidOperationException("Creating bank account failed.");
        }

        return result;
    }

    public async Task<IReadOnlyList<SnelStartBtwTariefLookupViewModel>> GetTenantSnelStartBtwTarievenAsync(
    Guid tenantId,
    CancellationToken cancellationToken = default)
    {
        var url = $"api/tenants/{tenantId}/snelstart/lookups/btw-tarieven";

        _logger.LogInformation("Ophalen tenantgerichte SnelStart btw-tarieven via {Url}", url);

        return await ExecuteWithRetryAsync(
                   async ct =>
                   {
                       var result = await _httpClient.GetFromJsonAsync<List<SnelStartBtwTariefLookupViewModel>>(
                           url,
                           ct);

                       return (IReadOnlyList<SnelStartBtwTariefLookupViewModel>)(result ?? []);
                   },
                   operationName: "tenantgerichte SnelStart btw-tarieven ophalen",
                   cancellationToken)
               ?? [];
    }

    public async Task<IReadOnlyList<SnelStartDagboekLookupViewModel>> GetTenantSnelStartDagboekenAsync(
    Guid tenantId,
    CancellationToken cancellationToken = default)
    {
        var url = $"api/tenants/{tenantId}/snelstart/lookups/dagboeken";

        _logger.LogInformation(
            "Tenantgerichte SnelStart dagboeken ophalen via {Url}",
            url);

        return await ExecuteWithRetryAsync(
                   async ct =>
                   {
                       var result = await _httpClient.GetFromJsonAsync<List<SnelStartDagboekLookupViewModel>>(
                           url,
                           ct);

                       return (IReadOnlyList<SnelStartDagboekLookupViewModel>)(result ?? []);
                   },
                   operationName: $"tenantgerichte SnelStart dagboeken ophalen voor tenant {tenantId}",
                   cancellationToken)
               ?? [];
    }

    public async Task UpdateTenantMyPosConnectionAsync(
        Guid tenantId,
        Guid connectionId,
        UpdateTenantMyPosConnectionRequestViewModel request,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/tenants/{tenantId}/mypos/connection/{connectionId}";

        _logger.LogInformation(
            "myPOS connection bijwerken via {Url}",
            url);

        await ExecuteWithRetryAsync<object?>(
            async ct =>
            {
                using var response = await _httpClient.PutAsJsonAsync(url, request, ct);
                response.EnsureSuccessStatusCode();
                return null;
            },
            operationName: $"myPOS connection bijwerken voor tenant {tenantId}",
            cancellationToken);
    }

    public async Task<MyPosExportBatchViewModel> CreateMyPosExportBatchConceptAsync(
    Guid tenantId,
    CreateMyPosExportBatchRequestViewModel request,
    CancellationToken cancellationToken = default)
    {
        var url = $"api/tenants/{tenantId}/mypos/export-batches/concept";

        _logger.LogInformation(
            "myPOS exportbatch concept aanmaken via {Url}",
            url);

        var result = await ExecuteWithRetryAsync(
            async ct =>
            {
                using var response = await _httpClient.PostAsJsonAsync(url, request, ct);
                response.EnsureSuccessStatusCode();

                var batch = await response.Content.ReadFromJsonAsync<MyPosExportBatchViewModel>(
                    cancellationToken: ct);

                return batch;
            },
            operationName: $"myPOS exportbatch concept aanmaken voor tenant {tenantId}",
            cancellationToken);

        return result ?? throw new InvalidOperationException("myPOS exportbatch response was leeg.");
    }

    public async Task<IReadOnlyList<MyPosExportBatchViewModel>> GetMyPosExportBatchesAsync(
        Guid tenantId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var url =
            $"api/tenants/{tenantId}/mypos/export-batches" +
            $"?fromUtc={Uri.EscapeDataString(fromUtc.ToString("O"))}" +
            $"&toUtc={Uri.EscapeDataString(toUtc.ToString("O"))}";

        _logger.LogInformation(
            "myPOS exportbatches ophalen via {Url}",
            url);

        return await ExecuteWithRetryAsync(
                   async ct =>
                   {
                       var result = await _httpClient.GetFromJsonAsync<List<MyPosExportBatchViewModel>>(
                           url,
                           ct);

                       return (IReadOnlyList<MyPosExportBatchViewModel>)(result ?? []);
                   },
                   operationName: $"myPOS exportbatches ophalen voor tenant {tenantId}",
                   cancellationToken)
               ?? [];
    }

    public async Task<MyPosExportBatchExportResultViewModel> ExportMyPosBatchToSnelStartAsync(
    Guid tenantId,
    Guid batchId,
    CancellationToken cancellationToken = default)
    {
        var url =
            $"api/tenants/{tenantId:D}/mypos/export-batches/{batchId:D}/export/snelstart-bankboek";

        using var response = await _httpClient.PostAsync(
            url,
            content: null,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new InvalidOperationException(
                $"myPOS exportbatch naar SnelStart mislukt. HTTP {(int)response.StatusCode}: {error}");
        }

        var result = await response.Content.ReadFromJsonAsync<MyPosExportBatchExportResultViewModel>(
            cancellationToken: cancellationToken);

        if (result is null)
        {
            throw new InvalidOperationException("myPOS exportresultaat was leeg.");
        }

        return result;
    }

    public async Task<MyPosAutoSyncSettingsViewModel> GetMyPosAutoSyncSettingsAsync(
        CancellationToken cancellationToken = default)
    {
        const string url = "api/mypos/auto-sync/settings";

        _logger.LogInformation("myPOS auto-sync instellingen ophalen via {Url}", url);

        var result = await ExecuteWithRetryAsync(
            ct => _httpClient.GetFromJsonAsync<MyPosAutoSyncSettingsViewModel>(url, ct),
            operationName: "myPOS auto-sync instellingen ophalen",
            cancellationToken);

        return result ?? throw new InvalidOperationException("myPOS auto-sync instellingen response was leeg.");
    }

    public async Task UpdateMyPosAutoSyncSettingsAsync(
        MyPosAutoSyncSettingsViewModel settings,
        CancellationToken cancellationToken = default)
    {
        const string url = "api/mypos/auto-sync/settings";

        _logger.LogInformation("myPOS auto-sync instellingen opslaan via {Url}", url);

        await ExecuteWithRetryAsync<object?>(
            async ct =>
            {
                using var response = await _httpClient.PutAsJsonAsync(url, settings, ct);
                response.EnsureSuccessStatusCode();
                return null;
            },
            operationName: "myPOS auto-sync instellingen opslaan",
            cancellationToken);
    }

    public async Task TriggerMyPosAutoSyncAsync(
        CancellationToken cancellationToken = default)
    {
        const string url = "api/mypos/auto-sync/run";

        _logger.LogInformation("myPOS auto-sync handmatig triggeren via {Url}", url);

        await ExecuteWithRetryAsync<object?>(
            async ct =>
            {
                using var response = await _httpClient.PostAsync(url, content: null, ct);
                response.EnsureSuccessStatusCode();
                return null;
            },
            operationName: "myPOS auto-sync handmatig uitvoeren",
            cancellationToken);
    }

    public async Task<Guid> OnboardTenantAsync(
        OnboardTenantRequestViewModel request,
        CancellationToken cancellationToken = default)
    {
        const string url = "api/tenants";

        _logger.LogInformation("Nieuwe tenant aanmaken via {Url}", url);

        var payload = new
        {
            Name          = request.CompanyName,
            CompanyName   = request.CompanyName,
            ContactName   = request.ContactName,
            Email         = request.Email,
            Phone         = request.Phone,
            Address       = request.Address,
            PostalCode    = request.PostalCode,
            City          = request.City,
            KvkNumber     = request.KvkNumber,
            GoDutchEnabled = request.GoDutchEnabled,
            MyPosEnabled  = request.MyPosEnabled,
            IsTrial       = request.IsTrial,
            TrialDurationDays = request.TrialDurationDays
        };

        var result = await ExecuteWithRetryAsync<Guid>(
            async ct =>
            {
                using var response = await _httpClient.PostAsJsonAsync(url, payload, ct);
                response.EnsureSuccessStatusCode();

                var location = response.Headers.Location?.ToString() ?? string.Empty;
                var idSegment = location.Split('/').LastOrDefault();
                return Guid.TryParse(idSegment, out var id) ? id : Guid.Empty;
            },
            operationName: "tenant aanmaken",
            cancellationToken);

        return result;
    }

    public async Task<int> GetUnreadNotificationCountAsync(
        CancellationToken cancellationToken = default)
    {
        const string url = "api/notifications/count";

        return await ExecuteWithRetryAsync(
            ct => _httpClient.GetFromJsonAsync<int>(url, ct),
            operationName: "ongelezen notificaties tellen",
            cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationViewModel>> GetUnreadNotificationsAsync(
        CancellationToken cancellationToken = default)
    {
        const string url = "api/notifications";

        var result = await ExecuteWithRetryAsync(
            ct => _httpClient.GetFromJsonAsync<List<NotificationViewModel>>(url, ct),
            operationName: "notificaties ophalen",
            cancellationToken);

        return result ?? [];
    }

    public async Task MarkNotificationAsReadAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/notifications/{id}/read";

        await ExecuteWithRetryAsync<object?>(
            async ct =>
            {
                using var response = await _httpClient.PutAsync(url, content: null, ct);
                response.EnsureSuccessStatusCode();
                return null;
            },
            operationName: "notificatie als gelezen markeren",
            cancellationToken);
    }

    public async Task MarkAllNotificationsAsReadAsync(
        CancellationToken cancellationToken = default)
    {
        const string url = "api/notifications/read-all";

        await ExecuteWithRetryAsync<object?>(
            async ct =>
            {
                using var response = await _httpClient.PutAsync(url, content: null, ct);
                response.EnsureSuccessStatusCode();
                return null;
            },
            operationName: "alle notificaties als gelezen markeren",
            cancellationToken);
    }

    public async Task<MyPosTransactionTypeStatusResultViewModel> GetMyPosTransactionTypeStatusAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/tenants/{tenantId}/mypos/transaction-types/status";

        _logger.LogInformation("myPOS transactietype status ophalen via {Url}", url);

        var result = await ExecuteWithRetryAsync(
            ct => _httpClient.GetFromJsonAsync<MyPosTransactionTypeStatusResultViewModel>(url, ct),
            operationName: $"myPOS transactietype status ophalen voor tenant {tenantId}",
            cancellationToken);

        return result ?? new MyPosTransactionTypeStatusResultViewModel();
    }

    public async Task SubmitGoDutchLeadAsync(
        GoDutchLeadViewModel lead,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/godutch-leads", lead, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

}