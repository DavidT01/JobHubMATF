using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notification.API.Services;

namespace Notification.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class NotificationsController(
    INotificationService notifications,
    IConfiguration configuration) : ControllerBase
{
    [Authorize]
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

    [Authorize]
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

    [Authorize]
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

    [Authorize]
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
    /// Service-to-service create (Identity). Requires X-Api-Key.
    /// </summary>
    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromHeader(Name = "X-Api-Key")] string? apiKey,
        [FromBody] CreateNotificationRequest request)
    {
        if (!IsValidApiKey(apiKey))
        {
            return Unauthorized(new { Message = "Invalid API key." });
        }

        if (string.IsNullOrWhiteSpace(request.UserId) ||
            string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { Message = "UserId, Title and Message are required." });
        }

        await notifications.NotifyAsync(request.UserId.Trim(), request.Title.Trim(), request.Message.Trim());
        return Accepted();
    }

    [AllowAnonymous]
    [HttpPost("batch")]
    public async Task<IActionResult> CreateBatch(
        [FromHeader(Name = "X-Api-Key")] string? apiKey,
        [FromBody] CreateNotificationBatchRequest request)
    {
        if (!IsValidApiKey(apiKey))
        {
            return Unauthorized(new { Message = "Invalid API key." });
        }

        if (request.UserIds is null || request.UserIds.Count == 0 ||
            string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { Message = "UserIds, Title and Message are required." });
        }

        await notifications.NotifyManyAsync(request.UserIds, request.Title.Trim(), request.Message.Trim());
        return Accepted();
    }

    private bool IsValidApiKey(string? apiKey)
    {
        var expected = configuration["NotificationApi:ApiKey"];
        return !string.IsNullOrWhiteSpace(expected)
               && string.Equals(expected, apiKey, StringComparison.Ordinal);
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

public sealed class CreateNotificationBatchRequest
{
    public List<string>? UserIds { get; set; }
    public string? Title { get; set; }
    public string? Message { get; set; }
}
