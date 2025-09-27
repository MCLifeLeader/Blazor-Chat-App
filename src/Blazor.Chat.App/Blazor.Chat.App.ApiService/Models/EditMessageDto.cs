using System.ComponentModel.DataAnnotations;

namespace Blazor.Chat.App.ApiService.Models
{
    /// <summary>
    /// DTO for editing a message
    /// </summary>
    public record EditMessageDto
    {
        [Required]
        [StringLength(10000, MinimumLength = 1)]
        public string Content { get; init; } = string.Empty;

        public List<MessageAttachmentDto> Attachments { get; init; } = new();
    }
}