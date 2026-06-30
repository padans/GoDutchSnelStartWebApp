using GoDutchSnelStartWebApp.Application.Notifications.Dtos;
using GoDutchSnelStartWebApp.Application.Notifications.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GoDutchSnelStartWebApp.Web.Controllers;

[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationRepository _repository;

    public NotificationsController(INotificationRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> GetUnreadCountAsync(CancellationToken cancellationToken)
    {
        var count = await _repository.GetUnreadCountAsync(cancellationToken);
        return Ok(count);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> GetUnreadAsync(CancellationToken cancellationToken)
    {
        var notifications = await _repository.GetUnreadAsync(cancellationToken);
        return Ok(notifications);
    }

    [HttpPut("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAsReadAsync(Guid id, CancellationToken cancellationToken)
    {
        await _repository.MarkAsReadAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPut("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllAsReadAsync(CancellationToken cancellationToken)
    {
        await _repository.MarkAllAsReadAsync(cancellationToken);
        return NoContent();
    }
}
