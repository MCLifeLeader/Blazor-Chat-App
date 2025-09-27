namespace Blazor.Chat.App.Data.Repositories;

/// <summary>
/// Statistics about outbox processing
/// </summary>
public record OutboxStatistics
{
    public int PendingCount { get; init; }
    public int ProcessingCount { get; init; }
    public int CompletedCount { get; init; }
    public int DeadLetterCount { get; init; }
    public int FailedCount { get; init; }
    public DateTime? OldestPendingAt { get; init; }
}