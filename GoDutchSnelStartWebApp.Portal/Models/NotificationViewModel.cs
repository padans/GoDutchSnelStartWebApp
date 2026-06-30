namespace GoDutchSnelStartWebApp.Portal.Models;

public sealed class NotificationViewModel
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string Severity { get; set; } = "Warning";
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? ReadUtc { get; set; }
}
