namespace GoDutchSnelStartWebApp.Application.MyPos.Interfaces;

public interface IMyPosAutoSyncService
{
    Task RunOnceAsync(CancellationToken cancellationToken = default);
}
