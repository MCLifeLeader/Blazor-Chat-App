using Blazor.Chat.App.Data.Db;

namespace Blazor.Chat.App.Data.Repositories;

/// <summary>
/// Repository interface for SQL Server-based chat operations
/// </summary>
public interface ISqlChatRepository
{
    #region Session Operations

    /// <summary>
    /// Creates a new chat session in the database
    /// </summary>
    /// <param name="session">Session entity to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created session entity</returns>
    Task<ChatSession> CreateSessionAsync(ChatSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a chat session by its unique identifier
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Session entity if found, null otherwise</returns>
    Task<ChatSession?> GetSessionByIdAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all sessions where the specified user is a participant
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of user's sessions</returns>
    Task<IEnumerable<ChatSession>> GetSessionsByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing session in the database
    /// </summary>
    /// <param name="session">Updated session entity</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated session entity</returns>
    Task<ChatSession> UpdateSessionAsync(ChatSession session, CancellationToken cancellationToken = default);

    #endregion

    #region Participant Operations

    /// <summary>
    /// Adds a new participant to a chat session
    /// </summary>
    /// <param name="participant">Participant entity to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Added participant entity</returns>
    Task<ChatParticipant> AddParticipantAsync(ChatParticipant participant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific participant by session and user ID
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="userId">User identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Participant entity if found, null otherwise</returns>
    Task<ChatParticipant?> GetParticipantAsync(Guid sessionId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active participants for a session
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of active participants</returns>
    Task<IEnumerable<ChatParticipant>> GetSessionParticipantsAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a participant as having left the session
    /// </summary>
    /// <param name="participantId">Participant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task RemoveParticipantAsync(Guid participantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing participant
    /// </summary>
    /// <param name="participant">Updated participant entity</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated participant entity</returns>
    Task<ChatParticipant> UpdateParticipantAsync(ChatParticipant participant, CancellationToken cancellationToken = default);

    #endregion

    #region Message Operations

    /// <summary>
    /// Saves a message and outbox entry in a single transaction to ensure consistency
    /// </summary>
    /// <param name="message">Message entity to save</param>
    /// <param name="outboxEntry">Outbox entry for eventual consistency with Cosmos DB</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple containing the saved message and outbox entry</returns>
    Task<(ChatMessage message, ChatOutbox outboxEntry)> SaveMessageWithOutboxAsync(
        ChatMessage message, 
        ChatOutbox outboxEntry, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a message by its unique identifier
    /// </summary>
    /// <param name="messageId">Message identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Message entity if found, null otherwise</returns>
    Task<ChatMessage?> GetMessageByIdAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves paginated messages for a session
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="skip">Number of messages to skip</param>
    /// <param name="take">Number of messages to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of messages</returns>
    Task<IEnumerable<ChatMessage>> GetSessionMessagesAsync(
        Guid sessionId, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of non-deleted messages in a session
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Message count</returns>
    Task<int> GetSessionMessageCountAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of a message
    /// </summary>
    /// <param name="messageId">Message identifier</param>
    /// <param name="status">New message status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated message entity</returns>
    Task<ChatMessage> UpdateMessageStatusAsync(Guid messageId, ChatMessageStatus status, CancellationToken cancellationToken = default);

    #endregion

    #region User Operations

    /// <summary>
    /// Checks if a user is an active participant in a session
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="userId">User identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user is an active participant, false otherwise</returns>
    Task<bool> IsUserParticipantAsync(Guid sessionId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the user IDs of all active participants in a session
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of user IDs</returns>
    Task<IEnumerable<string>> GetSessionParticipantUserIdsAsync(Guid sessionId, CancellationToken cancellationToken = default);

    #endregion
}