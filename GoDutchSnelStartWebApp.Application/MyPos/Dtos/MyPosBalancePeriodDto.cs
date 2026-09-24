namespace GoDutchSnelStartWebApp.Application.MyPos.Dtos;

public sealed class MyPosBalancePeriodDto
{
    public string Label { get; set; } = string.Empty;

    public DateTime FromUtc { get; set; }

    public DateTime ToUtc { get; set; }

    public decimal BeginBalance { get; set; }

    public decimal EndBalance { get; set; }

    public decimal Mutation { get; set; }

    public int TransactionCount { get; set; }
}
