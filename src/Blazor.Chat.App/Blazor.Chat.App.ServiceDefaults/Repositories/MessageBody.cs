namespace Blazor.Chat.App.ServiceDefaults.Repositories;

/// <summary>
/// Message body content structure
/// </summary>
public record MessageBody
{
    public string content { get; init; } = string.Empty;
    public string messageType { get; init; } = "text";
    public Guid? replyToMessageId { get; init; }
}