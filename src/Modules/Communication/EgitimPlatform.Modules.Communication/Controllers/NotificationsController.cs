using EgitimPlatform.Modules.Communication.Features;
using EgitimPlatform.Modules.Communication.Features.ManagePreferences;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EgitimPlatform.Modules.Communication.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public class NotificationsController(
    ListNotificationsHandler listHandler,
    GetUnreadNotificationCountHandler countHandler,
    MarkNotificationReadHandler markReadHandler,
    MarkAllNotificationsReadHandler markAllReadHandler,
    INotificationPreferencesHandler preferencesHandler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] bool? isRead, CancellationToken ct)
    {
        var result = await listHandler.HandleAsync(isRead, ct);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
    {
        var count = await countHandler.HandleAsync(ct);
        return Ok(new { count });
    }

    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        await markReadHandler.HandleAsync(id, ct);
        return NoContent();
    }

    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await markAllReadHandler.HandleAsync(ct);
        return NoContent();
    }

    [HttpGet("preferences")]
    [ProducesResponseType(typeof(NotificationPreferencesDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPreferences(CancellationToken ct)
    {
        var result = await preferencesHandler.GetPreferencesAsync(ct);
        return Ok(result);
    }

    [HttpPut("preferences")]
    [ProducesResponseType(typeof(NotificationPreferencesDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdateNotificationPreferencesRequest req, CancellationToken ct)
    {
        var result = await preferencesHandler.UpdatePreferencesAsync(req, ct);
        return Ok(result);
    }
}
