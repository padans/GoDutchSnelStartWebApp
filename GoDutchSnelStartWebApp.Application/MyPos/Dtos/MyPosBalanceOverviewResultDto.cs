namespace GoDutchSnelStartWebApp.Application.MyPos.Dtos;

public sealed class MyPosBalanceOverviewResultDto
{
    public Guid TenantId { get; set; }

    public Guid TenantMyPosConnectionId { get; set; }

    public decimal ReferenceBalance { get; set; }

    public DateTime ReferenceUtc { get; set; }

    public DateTime FromUtc { get; set; }

    public DateTime ToUtc { get; set; }

    public string Granularity { get; set; } = "Month";

    public int TotalTransactionCount { get; set; }

    public List<MyPosBalancePeriodDto> Periods { get; set; } = [];
}
