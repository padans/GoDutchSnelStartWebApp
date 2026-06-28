using GoDutchSnelStartWebApp.Application.MyPos.Dtos;

namespace GoDutchSnelStartWebApp.Application.MyPos.Interfaces;

public interface IMyPosExportBatchExportService
{
    Task<MyPosExportBatchExportResultDto> ExportToSnelStartBankboekAsync(
        Guid tenantId,
        Guid batchId,
        CancellationToken cancellationToken = default);
}