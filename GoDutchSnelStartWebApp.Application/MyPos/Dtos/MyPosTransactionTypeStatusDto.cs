namespace GoDutchSnelStartWebApp.Application.MyPos.Dtos;

public sealed class MyPosTransactionTypeStatusDto
{
    public string TransactionType { get; init; } = string.Empty;
    public bool IsMapped { get; init; }
    public bool HasActiveMapping { get; init; }
    public string? SnelStartGrootboekNummer { get; init; }
    public string? SnelStartGrootboekNaam { get; init; }
}

public sealed class MyPosTransactionTypeStatusResultDto
{
    public IReadOnlyList<MyPosTransactionTypeStatusDto> Types { get; init; } = [];
    public int UnmappedCount { get; init; }
    public bool AllMapped => UnmappedCount == 0;
}
