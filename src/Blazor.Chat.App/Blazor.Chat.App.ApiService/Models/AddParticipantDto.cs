using System.ComponentModel.DataAnnotations;

namespace Blazor.Chat.App.ApiService.Models
{
    /// <summary>
    /// DTO for adding a participant to a chat session
    /// </summary>
    public record AddParticipantDto
    {
        [Required]
        public string UserId { get; init; } = string.Empty;

        public string Role { get; init; } = "Member";
    }
}