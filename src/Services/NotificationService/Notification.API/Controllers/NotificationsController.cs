using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notification.API.Services;

namespace Notification.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class NotificationsController(INotificationService notifications) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var userId = CurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var items = await notifications.ListForUserAsync(userId);
        return Ok(items.Select(n => new
        {
            n.Id,
            n.Title,
            n.Message,
            n.CreatedAtUtc,
            n.IsRead
        }));
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount()
    {
        var userId = CurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var count = await notifications.CountUnreadAsync(userId);
        return Ok(new { count });
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var userId = CurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var ok = await notifications.MarkReadAsync(userId, id);
        return ok ? Ok(new { Message = "Notification marked as read." }) : NotFound();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = CurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        await notifications.MarkAllReadAsync(userId);
        return Ok(new { Message = "All notifications marked as read." });
    }

    /// <summary>
    /// Internal create endpoint for other services (same JWT for now).
    /// Later: service-to-service auth or RabbitMQ consumer.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateNotificationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId) ||
            string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { Message = "UserId, Title and Message are required." });
        }

        await notifications.NotifyAsync(request.UserId.Trim(), request.Title.Trim(), request.Message.Trim());
        return Accepted();
    }

    private string? CurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier);
}

public sealed class CreateNotificationRequest
{
    public string? UserId { get; set; }
    public string? Title { get; set; }
    public string? Message { get; set; }
}
