using System.ComponentModel.DataAnnotations;

namespace Blazor.Chat.App.ApiService.Models;

/// <summary>
/// DTO for adding a new message to a chat session
/// </summary>
public record AddMessageDto
{
    /// <summary>
    /// The content/text of the message
    /// </summary>
    [Required]
    [StringLength(10000, MinimumLength = 1)]
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Optional ID of the message this is replying to
    /// </summary>
    public Guid? ReplyToMessageId { get; init; }

    /// <summary>
    /// List of file attachments for this message
    /// </summary>
    public List<MessageAttachmentDto> Attachments { get; init; } = new();

    /// <summary>
    /// Type of message (e.g., "text", "image", "file")
    /// </summary>
    [StringLength(100)]
    public string? MessageType { get; init; } = "text";
}