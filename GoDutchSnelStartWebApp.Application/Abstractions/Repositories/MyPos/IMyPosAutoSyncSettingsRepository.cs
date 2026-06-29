using GoDutchSnelStartWebApp.Application.MyPos.Dtos;

namespace GoDutchSnelStartWebApp.Application.Abstractions.Repositories.MyPos;

public interface IMyPosAutoSyncSettingsRepository
{
    Task<MyPosAutoSyncSettingsDto?> GetAsync(CancellationToken cancellationToken = default);
    Task UpsertAsync(MyPosAutoSyncSettingsDto settings, CancellationToken cancellationToken = default);
}
