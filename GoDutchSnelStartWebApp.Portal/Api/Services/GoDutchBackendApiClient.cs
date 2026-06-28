using System.Net;
using System.Net.Http.Json;
using GoDutchSnelStartWebApp.Portal.Api.Interfaces;
using GoDutchSnelStartWebApp.Portal.Models;

namespace GoDutchSnelStartWebApp.Portal.Api.Services;

public sealed class GoDutchBackendApiClient : IGoDutchBackendApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GoDutchBackendApiClient> _logger;

    public GoDutchBackendApiClient(
        HttpClient httpClient,
        ILogger<GoDutchBackendApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<TenantGoDutchConnectionViewModel?> GetTenantGoDutchConnectionAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/tenants/{tenantId}/godutch/connection";

        _logger.LogInformation(
            "Tenant GoDutch connection ophalen via {Url}",
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

                return await response.Content.ReadFromJsonAsync<TenantGoDutchConnectionViewModel>(
                    cancellationToken: ct);
            },
            operationName: $"Tenant GoDutch connection ophalen voor tenant {tenantId}",
            cancellationToken);
    }

    public async Task SaveTenantGoDutchConnectionAsync(
        Guid tenantId,
        TenantGoDutchConnectionViewModel request,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = $"api/tenants/{tenantId}/godutch/connection";

        _logger.LogInformation(
            "Tenant GoDutch connection opslaan via {Url}",
            baseUrl);

        await ExecuteWithRetryAsync<object?>(
            async ct =>
            {
                request.TenantId = tenantId;
                request.Password = string.IsNullOrWhiteSpace(request.Password)
                    ? null
                    : request.Password;

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
            operationName: $"Tenant GoDutch connection opslaan voor tenant {tenantId}",
            cancellationToken);
    }

    public async Task<IReadOnlyList<GoDutchAccountLookupViewModel>> GetGoDutchAccountsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/tenants/{tenantId}/godutch/accounts";

        _logger.LogInformation(
            "GoDutch bankrekeningen ophalen via {Url}",
            url);

        return await ExecuteWithRetryAsync(
                   async ct =>
                   {
                       var result = await _httpClient.GetFromJsonAsync<List<GoDutchAccountLookupViewModel>>(
                           url,
                           ct);

                       return (IReadOnlyList<GoDutchAccountLookupViewModel>)(result ?? []);
                   },
                   operationName: $"GoDutch bankrekeningen ophalen voor tenant {tenantId}",
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
}
