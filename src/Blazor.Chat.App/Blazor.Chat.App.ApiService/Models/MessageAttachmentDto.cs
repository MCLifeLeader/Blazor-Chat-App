using System.ComponentModel.DataAnnotations;

namespace Blazor.Chat.App.ApiService.Models
{
    /// <summary>
    /// DTO for message attachment information
    /// </summary>
    public record MessageAttachmentDto
    {
        [Required]
        public string FileName { get; init; } = string.Empty;

        [Required]
        public string ContentType { get; init; } = string.Empty;

        public long Size { get; init; }

        [Required]
        public string Url { get; init; } = string.Empty;
    }
}