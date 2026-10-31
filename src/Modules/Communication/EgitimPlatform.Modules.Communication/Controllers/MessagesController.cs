using EgitimPlatform.Modules.Communication.Features.ManageMessages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EgitimPlatform.Modules.Communication.Controllers;

[ApiController]
[Route("api/v1/communication/messages")]
[Authorize]
public class MessagesController(MessagesHandler messagesHandler) : ControllerBase
{
    [HttpGet("conversations")]
    [ProducesResponseType(typeof(List<ConversationSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListConversations(CancellationToken ct)
    {
        var result = await messagesHandler.ListConversationsAsync(ct);
        return Ok(result);
    }

    [HttpGet("recipients")]
    [ProducesResponseType(typeof(List<RecipientDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRecipients(CancellationToken ct)
    {
        var result = await messagesHandler.ListPermittedRecipientsAsync(ct);
        return Ok(result);
    }

    [HttpGet("conversations/{conversationId:guid}")]
    [ProducesResponseType(typeof(List<MessageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConversationMessages(
        Guid conversationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await messagesHandler.GetConversationMessagesAsync(conversationId, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTotalUnreadCount(CancellationToken ct)
    {
        var count = await messagesHandler.GetTotalUnreadCountAsync(ct);
        return Ok(new { count });
    }

    [HttpPatch("conversations/{conversationId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkConversationRead(Guid conversationId, CancellationToken ct)
    {
        await messagesHandler.MarkConversationAsReadAsync(conversationId, ct);
        return NoContent();
    }

    [HttpPost]
    [ProducesResponseType(typeof(MessageDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request, CancellationToken ct)
    {
        var result = await messagesHandler.SendMessageAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
