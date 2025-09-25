using Blazor.Chat.App.Data.Db;

namespace Blazor.Chat.App.ServiceDefaults.Repositories
{
    /// <summary>
    /// Repository interface for managing outbox entries
    /// </summary>
    public interface IOutboxRepository
    {
        /// <summary>
        /// Get pending outbox entries for processing
        /// </summary>
        Task<IEnumerable<ChatOutbox>> GetPendingEntriesAsync(
            int maxCount = 100, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Mark an outbox entry as being processed
        /// </summary>
        Task<bool> MarkAsProcessingAsync(
            Guid outboxId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Mark an outbox entry as successfully processed
        /// </summary>
        Task MarkAsCompletedAsync(
            Guid outboxId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Mark an outbox entry as failed with error details
        /// </summary>
        Task MarkAsFailedAsync(
            Guid outboxId, 
            string errorMessage, 
            DateTime? nextRetryAt = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Mark an outbox entry as dead letter after max retries
        /// </summary>
        Task MarkAsDeadLetterAsync(
            Guid outboxId, 
            string finalError, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get outbox entries that are ready for retry
        /// </summary>
        Task<IEnumerable<ChatOutbox>> GetEntriesReadyForRetryAsync(
            int maxCount = 100, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get statistics about outbox entries
        /// </summary>
        Task<OutboxStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
    }

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
}