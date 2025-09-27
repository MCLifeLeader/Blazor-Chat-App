namespace Blazor.Chat.App.Data.Db;

/// <summary>
/// Enumeration of possible message statuses
/// </summary>
public enum ChatMessageStatus
{
    Pending = 0,      // Waiting to be processed by outbox
    Sent = 1,         // Successfully sent to Cosmos DB
    Failed = 2,       // Failed to send after retries
    Edited = 3,       // Message has been edited
    Deleted = 4       // Message has been deleted
}