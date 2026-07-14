using GoDutchSnelStartWebApp.Application.MyPos.Dtos;

namespace GoDutchSnelStartWebApp.Application.MyPos.Interfaces;

public interface IMyPosAutoSyncService
{
    Task RunOnceAsync(CancellationToken cancellationToken = default);
    Task<MyPosAutoSyncSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task UpdateSettingsAsync(MyPosAutoSyncSettingsDto settings, CancellationToken cancellationToken = default);
}
