namespace GoDutchSnelStartWebApp.Portal.Models.MyPos;

public sealed class MyPosTransactionTypeStatusViewModel
{
    public string TransactionType { get; set; } = string.Empty;
    public bool IsMapped { get; set; }
    public bool HasActiveMapping { get; set; }
    public string? SnelStartGrootboekNummer { get; set; }
    public string? SnelStartGrootboekNaam { get; set; }
}

public sealed class MyPosTransactionTypeStatusResultViewModel
{
    public List<MyPosTransactionTypeStatusViewModel> Types { get; set; } = [];
    public int UnmappedCount { get; set; }
    public bool AllMapped => UnmappedCount == 0;
}
