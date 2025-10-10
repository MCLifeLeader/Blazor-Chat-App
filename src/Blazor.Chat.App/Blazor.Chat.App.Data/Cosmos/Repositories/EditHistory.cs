namespace Blazor.Chat.App.Data.Cosmos.Repositories;

/// <summary>
/// Edit history entry
/// </summary>
public record EditHistory
{
    public DateTime editedAt { get; init; }
    public string previousContent { get; init; } = string.Empty;
    public string editedByUserId { get; init; } = string.Empty;
}