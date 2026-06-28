using GoDutchSnelStartWebApp.Application.Configuration;
using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GoDutchSnelStartWebApp.Infrastructure.ExternalServices.BackgroundWorkers;

public sealed class GoDutchAutoSyncBackgroundWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IOptions<GoDutchAutoSyncOptions> _options;
    private readonly ILogger<GoDutchAutoSyncBackgroundWorker> _logger;

    public GoDutchAutoSyncBackgroundWorker(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<GoDutchAutoSyncOptions> options,
        ILogger<GoDutchAutoSyncBackgroundWorker> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = _options.Value;

        if (!settings.Enabled)
        {
            _logger.LogInformation("GoDutch auto sync background worker is uitgeschakeld.");
            return;
        }

        var pollIntervalMinutes = NormalizePollIntervalMinutes(settings.IntervalMinutes);

        _logger.LogInformation(
            "GoDutch auto sync background worker gestart. Poll interval: {PollIntervalMinutes} minuten. De syncfrequentie per bankrekening komt uit de database.",
            pollIntervalMinutes);

        _logger.LogInformation("Startup-check wordt uitgevoerd: alleen koppelingen waarvan NextRunUtc verlopen is worden verwerkt.");
        await RunOnceAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(pollIntervalMinutes), stoppingToken);

                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("GoDutch auto sync worker wordt gestopt.");
                break;
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();

        var service = scope.ServiceProvider
            .GetRequiredService<IGoDutchAutoSyncService>();

        _logger.LogInformation("GoDutch auto sync pollronde gestart.");

        await service.RunOnceAsync(stoppingToken);

        _logger.LogInformation("GoDutch auto sync pollronde afgerond.");
    }

    private static int NormalizePollIntervalMinutes(int intervalMinutes)
        => intervalMinutes < 1 ? 1 : intervalMinutes;
}
