namespace Blazor.Chat.App.ServiceDefaults.Repositories
{
    /// <summary>
    /// Repository interface for Cosmos DB-based chat operations
    /// </summary>
    public interface IChatCosmosRepository
    {
        /// <summary>
        /// Upsert a message document to Cosmos DB with idempotency via outboxId
        /// </summary>
        Task UpsertMessageAsync(
            CosmosMessageDocument message, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get messages for a session with pagination
        /// </summary>
        Task<CosmosMessagePage> GetSessionMessagesAsync(
            Guid sessionId,
            int page = 1,
            int pageSize = 50,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a specific message by ID
        /// </summary>
        Task<CosmosMessageDocument?> GetMessageByIdAsync(
            Guid sessionId,
            Guid messageId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Update/edit a message document
        /// </summary>
        Task UpsertEditedMessageAsync(
            CosmosMessageDocument editedMessage,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Mark a message as deleted (soft delete)
        /// </summary>
        Task MarkMessageAsDeletedAsync(
            Guid sessionId,
            Guid messageId,
            Guid outboxId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Upsert session snapshot document for quick session information retrieval
        /// </summary>
        Task UpsertSessionSnapshotAsync(
            CosmosSessionSnapshot snapshot,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get session snapshot
        /// </summary>
        Task<CosmosSessionSnapshot?> GetSessionSnapshotAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Bulk upsert multiple messages (for efficiency during outbox processing)
        /// </summary>
        Task BulkUpsertMessagesAsync(
            IEnumerable<CosmosMessageDocument> messages,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Cosmos DB message document structure
    /// </summary>
    public record CosmosMessageDocument
    {
        public string id { get; init; } = string.Empty; // Cosmos DB id (messageId)
        public Guid sessionId { get; init; }
        public Guid senderUserId { get; init; }
        public string senderDisplayName { get; init; } = string.Empty;
        public DateTime sentAt { get; init; }
        public MessageBody body { get; init; } = new();
        public List<MessageAttachment> attachments { get; init; } = new();
        public Guid outboxId { get; init; }
        public MessageMetadata metadata { get; init; } = new();
        public bool isDeleted { get; init; }
        public string documentType { get; init; } = "message"; // For document type discrimination
    }

    /// <summary>
    /// Message body content structure
    /// </summary>
    public record MessageBody
    {
        public string content { get; init; } = string.Empty;
        public string messageType { get; init; } = "text";
        public Guid? replyToMessageId { get; init; }
    }

    /// <summary>
    /// Message attachment structure
    /// </summary>
    public record MessageAttachment
    {
        public string fileName { get; init; } = string.Empty;
        public string contentType { get; init; } = string.Empty;
        public long size { get; init; }
        public string url { get; init; } = string.Empty;
    }

    /// <summary>
    /// Message metadata for edits, versions, etc.
    /// </summary>
    public record MessageMetadata
    {
        public DateTime? editedAt { get; init; }
        public int version { get; init; } = 1;
        public List<EditHistory> editHistory { get; init; } = new();
    }

    /// <summary>
    /// Edit history entry
    /// </summary>
    public record EditHistory
    {
        public DateTime editedAt { get; init; }
        public string previousContent { get; init; } = string.Empty;
        public Guid editedByUserId { get; init; }
    }

    /// <summary>
    /// Paginated result for Cosmos message queries
    /// </summary>
    public record CosmosMessagePage
    {
        public List<CosmosMessageDocument> Messages { get; init; } = new();
        public string? ContinuationToken { get; init; }
        public bool HasMoreResults { get; init; }
        public int Count { get; init; }
    }

    /// <summary>
    /// Session snapshot document for quick session information
    /// </summary>
    public record CosmosSessionSnapshot
    {
        public string id { get; init; } = string.Empty; // "session-{sessionId}-snapshot"
        public Guid sessionId { get; init; }
        public int messagesCount { get; init; }
        public DateTime lastUpdatedAt { get; init; }
        public DateTime lastActivityAt { get; init; }
        public List<Guid> recentParticipants { get; init; } = new();
        public CosmosMessageDocument? lastMessage { get; init; }
        public string documentType { get; init; } = "session-snapshot";
    }
}