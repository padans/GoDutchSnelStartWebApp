namespace GoDutchSnelStartWebApp.Application.Notifications.Interfaces;

public interface IEmailNotificationService
{
    Task SendAsync(string subject, string body, CancellationToken cancellationToken = default);

    Task SendToAsync(
        string toAddress,
        string toName,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);
}
