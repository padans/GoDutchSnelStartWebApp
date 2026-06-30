namespace GoDutchSnelStartWebApp.Application.Notifications.Interfaces;

public interface IEmailNotificationService
{
    Task SendAsync(string subject, string body, CancellationToken cancellationToken = default);
}
