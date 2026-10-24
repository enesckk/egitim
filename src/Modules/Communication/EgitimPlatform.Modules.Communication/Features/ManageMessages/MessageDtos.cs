using System.ComponentModel.DataAnnotations;

namespace EgitimPlatform.Modules.Communication.Features.ManageMessages;

public record ConversationSummaryDto(
    Guid Id,
    Guid OtherUserId,
    string OtherUserName,
    string OtherUserRole,
    string? Subject,
    string? LastMessageSnippet,
    DateTimeOffset LastMessageAt,
    int UnreadCount);

public record MessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderUserId,
    string SenderUserName,
    string SenderRole,
    string Content,
    DateTimeOffset CreatedAt,
    bool IsRead,
    DateTimeOffset? ReadAt);

public record SendMessageRequest(
    Guid? ConversationId,
    Guid? RecipientUserId,
    [Required, MinLength(1), MaxLength(4000)] string Content,
    string? Subject);

public record StartConversationRequest(
    [Required] Guid RecipientUserId,
    [Required, MinLength(1), MaxLength(4000)] string InitialMessage,
    string? Subject);
