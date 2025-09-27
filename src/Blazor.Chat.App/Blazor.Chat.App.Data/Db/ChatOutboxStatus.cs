namespace Blazor.Chat.App.Data.Db;

/// <summary>
/// Enumeration of possible outbox entry statuses
/// </summary>
public enum ChatOutboxStatus
{
    Pending = 0,      // Waiting to be processed
    Processing = 1,   // Currently being processed
    Completed = 2,    // Successfully processed
    DeadLetter = 3    // Failed after maximum retries
}