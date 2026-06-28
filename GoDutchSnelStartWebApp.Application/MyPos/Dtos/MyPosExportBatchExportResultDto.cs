namespace GoDutchSnelStartWebApp.Application.MyPos.Dtos;

public sealed class MyPosExportBatchExportResultDto
{
    public Guid BatchId { get; set; }

    public Guid TenantId { get; set; }

    public bool Success { get; set; }

    public int LineCount { get; set; }

    public int ExportedLineCount { get; set; }

    public int FailedLineCount { get; set; }

    public string? SnelStartReference { get; set; }

    public string Message { get; set; } = string.Empty;
}