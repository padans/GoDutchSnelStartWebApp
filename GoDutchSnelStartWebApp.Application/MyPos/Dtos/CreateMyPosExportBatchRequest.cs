namespace GoDutchSnelStartWebApp.Application.MyPos.Dtos;

public sealed class CreateMyPosExportBatchRequest
{
    public Guid? TenantMyPosConnectionId { get; set; }
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }
    public bool IncludeExported { get; set; }
}
