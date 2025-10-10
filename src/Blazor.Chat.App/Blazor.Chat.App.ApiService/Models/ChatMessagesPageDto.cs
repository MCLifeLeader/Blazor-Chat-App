namespace Blazor.Chat.App.ApiService.Models;

/// <summary>
/// DTO for paginated list of messages
/// </summary>
public record ChatMessagesPageDto
{
    public List<ChatMessageDto> Messages { get; init; } = new();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public bool HasNextPage { get; init; }
    public bool HasPreviousPage { get; init; }
}