namespace GoDutchSnelStartWebApp.Application.GoDutchTransactions.Interfaces;

public interface IGoDutchAutoSyncService
{
    Task RunOnceAsync(CancellationToken cancellationToken = default);
}