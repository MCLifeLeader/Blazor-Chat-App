using System.ComponentModel.DataAnnotations;

namespace Blazor.Chat.App.ApiService.Models
{
    /// <summary>
    /// DTO for adding a new message to a chat session
    /// </summary>
    public record AddMessageDto
    {
        [Required]
        [StringLength(10000, MinimumLength = 1)]
        public string Content { get; init; } = string.Empty;

        public Guid? ReplyToMessageId { get; init; }

        public List<MessageAttachmentDto> Attachments { get; init; } = new();

        [StringLength(100)]
        public string? MessageType { get; init; } = "text";
    }
}