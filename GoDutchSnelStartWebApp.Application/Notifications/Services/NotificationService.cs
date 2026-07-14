using GoDutchSnelStartWebApp.Application.Notifications.Dtos;
using GoDutchSnelStartWebApp.Application.Notifications.Interfaces;

namespace GoDutchSnelStartWebApp.Application.Notifications.Services;

public sealed class NotificationService : INotificationService
{
    private readonly INotificationRepository _repository;

    public NotificationService(INotificationRepository repository)
    {
        _repository = repository;
    }

    public Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
        => _repository.GetUnreadCountAsync(cancellationToken);

    public Task<IReadOnlyList<NotificationDto>> GetUnreadAsync(CancellationToken cancellationToken = default)
        => _repository.GetUnreadAsync(cancellationToken);

    public Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.MarkAsReadAsync(id, cancellationToken);

    public Task MarkAllAsReadAsync(CancellationToken cancellationToken = default)
        => _repository.MarkAllAsReadAsync(cancellationToken);
}
