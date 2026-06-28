namespace GoDutchSnelStartWebApp.Application.MyPos.Dtos;

public sealed class MyPosTransactionImportResultDto
{
    public Guid TenantId { get; set; }
    public Guid TenantMyPosConnectionId { get; set; }
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }
    public int FetchedCount { get; set; }
    public int InsertedOrUpdatedCount { get; set; }
    public string Message { get; set; } = string.Empty;
    public int InsertedCount { get; set; }

    public int UpdatedCount { get; set; }

    public int SkippedCount { get; set; }

    public int DuplicateInImportCount { get; set; }
}
