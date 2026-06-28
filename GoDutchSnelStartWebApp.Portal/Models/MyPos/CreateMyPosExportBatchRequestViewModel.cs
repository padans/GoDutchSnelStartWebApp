namespace GoDutchSnelStartWebApp.Portal.Models.MyPos;

public sealed class CreateMyPosExportBatchRequestViewModel
{
    public Guid? TenantMyPosConnectionId { get; set; }

    public DateTime FromUtc { get; set; }

    public DateTime ToUtc { get; set; }

    public bool IncludeExported { get; set; }
}