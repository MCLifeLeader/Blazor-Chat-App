namespace Blazor.Chat.App.ApiService.Models.Settings
{
    public class Outboxprocessor
    {
        /// <summary>
        /// Configuration section name.
        /// </summary>
        public string SectionName = "OutboxProcessor";

        /// <summary>
        /// Number of items to process per batch.
        /// </summary>
        public int BatchSize { get; set; } = 100;

        /// <summary>
        /// Interval in seconds between outbox processing runs.
        /// </summary>
        public int ProcessingIntervalSeconds { get; set; } = 5;

        /// <summary>
        /// Maximum retry attempts for processing a failed outbox item.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Maximum degree of parallelism for processing outbox items.
        /// </summary>
        public int MaxConcurrency { get; set; } = 5;
    }
}