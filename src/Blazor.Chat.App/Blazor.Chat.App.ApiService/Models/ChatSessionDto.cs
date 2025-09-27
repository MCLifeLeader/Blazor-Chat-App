namespace Blazor.Chat.App.ApiService.Models
{
    /// <summary>
    /// DTO for chat session information
    /// </summary>
    public record ChatSessionDto
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public bool IsGroup { get; init; }
        public string CreatedByUserId { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public DateTime LastActivityAt { get; init; }
        public string? TenantId { get; init; }
        public string State { get; init; } = string.Empty;
        public int ParticipantCount { get; init; }
        public int MessageCount { get; init; }
    }
}