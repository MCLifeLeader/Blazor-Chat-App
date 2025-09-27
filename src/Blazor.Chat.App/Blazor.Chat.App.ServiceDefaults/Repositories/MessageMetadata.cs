namespace Blazor.Chat.App.ServiceDefaults.Repositories;

/// <summary>
/// Message metadata for edits, versions, etc.
/// </summary>
public record MessageMetadata
{
    public DateTime? editedAt { get; init; }
    public int version { get; init; } = 1;
    public List<EditHistory> editHistory { get; init; } = new();
}