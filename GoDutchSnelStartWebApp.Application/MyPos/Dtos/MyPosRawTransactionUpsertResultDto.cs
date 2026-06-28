namespace GoDutchSnelStartWebApp.Application.MyPos.Dtos;

public sealed class MyPosRawTransactionUpsertResultDto
{
    public int InputCount { get; set; }

    public int InsertedCount { get; set; }

    public int UpdatedCount { get; set; }

    public int SkippedCount { get; set; }

    public int DuplicateInImportCount { get; set; }

    public int DatabaseOperationCount => InsertedCount + UpdatedCount;
}