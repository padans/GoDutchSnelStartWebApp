using GoDutchSnelStartWebApp.Application.Abstractions.Repositories.MyPos;
using GoDutchSnelStartWebApp.Application.Configuration;
using GoDutchSnelStartWebApp.Application.MyPos.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GoDutchSnelStartWebApp.Infrastructure.ExternalServices.BackgroundWorkers;

public sealed class MyPosAutoSyncBackgroundWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IOptions<MyPosAutoSyncOptions> _options;
    private readonly ILogger<MyPosAutoSyncBackgroundWorker> _logger;

    public MyPosAutoSyncBackgroundWorker(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<MyPosAutoSyncOptions> options,
        ILogger<MyPosAutoSyncBackgroundWorker> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var defaultSettings = _options.Value;

        if (!defaultSettings.Enabled)
        {
            _logger.LogInformation("myPOS auto sync background worker is uitgeschakeld.");
            return;
        }

        _logger.LogInformation("myPOS auto sync background worker gestart.");
        _logger.LogInformation("Startup-run wordt uitgevoerd.");
        await RunOnceAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var intervalMinutes = await GetIntervalMinutesAsync(stoppingToken);

                _logger.LogDebug("myPOS auto sync wacht {IntervalMinutes} minuten tot volgende run.", intervalMinutes);

                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);

                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("myPOS auto sync worker wordt gestopt.");
                break;
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IMyPosAutoSyncService>();

        _logger.LogInformation("myPOS auto sync pollronde gestart.");
        await service.RunOnceAsync(stoppingToken);
        _logger.LogInformation("myPOS auto sync pollronde afgerond.");
    }

    private async Task<int> GetIntervalMinutesAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IMyPosAutoSyncSettingsRepository>();
            var dbSettings = await repository.GetAsync(cancellationToken);

            if (dbSettings is not null)
            {
                return dbSettings.IntervalMinutes < 1 ? 1 : dbSettings.IntervalMinutes;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "myPOS interval uit database ophalen mislukt, appsettings-waarde wordt gebruikt.");
        }

        var fallback = _options.Value.IntervalMinutes;
        return fallback < 1 ? 1 : fallback;
    }
}
