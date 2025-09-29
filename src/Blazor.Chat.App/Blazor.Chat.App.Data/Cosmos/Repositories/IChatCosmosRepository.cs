namespace Blazor.Chat.App.Data.Cosmos.Repositories;

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
    /// <summary>
    /// Delete a message document from Cosmos DB
    /// </summary>
    Task DeleteMessageAsync(
        Guid messageId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upsert a session snapshot document
    /// </summary>
    Task UpsertSessionSnapshotAsync(
        object snapshotData,
        CancellationToken cancellationToken = default);
}