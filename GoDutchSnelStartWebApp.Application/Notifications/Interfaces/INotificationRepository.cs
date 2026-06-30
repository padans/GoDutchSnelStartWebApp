using GoDutchSnelStartWebApp.Application.Notifications.Dtos;

namespace GoDutchSnelStartWebApp.Application.Notifications.Interfaces;

public interface INotificationRepository
{
    Task InsertAsync(NotificationDto notification, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationDto>> GetUnreadAsync(CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(CancellationToken cancellationToken = default);
}
