namespace Blazor.Chat.App.ApiService.Models;

/// <summary>
/// DTO for chat participant information
/// </summary>
public record ChatParticipantDto
{
    public Guid Id { get; init; }
    public Guid SessionId { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public DateTime JoinedAt { get; init; }
    public DateTime? LeftAt { get; init; }
    public bool IsMuted { get; init; }
    public string Role { get; init; } = string.Empty;
    public Guid? LastReadMessageId { get; init; }
    public DateTime? LastReadAt { get; init; }
}