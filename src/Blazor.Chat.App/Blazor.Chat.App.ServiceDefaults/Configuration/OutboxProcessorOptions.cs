namespace Blazor.Chat.App.ServiceDefaults.Configuration
{
    /// <summary>
    /// Configuration options for the outbox processor background service
    /// </summary>
    public class OutboxProcessorOptions
    {
        /// <summary>
        /// Configuration section name
        /// </summary>
        public const string SectionName = "OutboxProcessor";

        /// <summary>
        /// Number of outbox entries to process in each batch
        /// </summary>
        public int BatchSize { get; set; } = 100;

        /// <summary>
        /// Interval between processing attempts in seconds
        /// </summary>
        public int ProcessingIntervalSeconds { get; set; } = 5;

        /// <summary>
        /// Maximum number of retry attempts before marking as dead letter
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Maximum number of concurrent processing operations
        /// </summary>
        public int MaxConcurrency { get; set; } = 5;

        /// <summary>
        /// Base delay in seconds for exponential backoff (e.g., attempt 1: 2s, attempt 2: 4s, attempt 3: 8s)
        /// </summary>
        public int BaseRetryDelaySeconds { get; set; } = 2;

        /// <summary>
        /// Whether the outbox processor is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
}