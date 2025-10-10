namespace Blazor.Chat.App.ApiService.Models;

/// <summary>
/// DTO for chat message information
/// </summary>
public record ChatMessageDto
{
    public Guid Id { get; init; }
    public Guid SessionId { get; init; }
    public string SenderUserId { get; init; } = string.Empty;
    public string SenderDisplayName { get; init; } = string.Empty;
    public DateTime SentAt { get; init; }
    public string Content { get; init; } = string.Empty;
    public string Preview { get; init; } = string.Empty;
    public int MessageLength { get; init; }
    public string Status { get; init; } = string.Empty;
    public Guid? ReplyToMessageId { get; init; }
    public DateTime? EditedAt { get; init; }
    public int Version { get; init; }
    public List<MessageAttachmentDto> Attachments { get; init; } = new();
    public string MessageType { get; init; } = "text";
}