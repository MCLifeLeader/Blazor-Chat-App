using Blazor.Chat.App.Data.Db;

namespace Blazor.Chat.App.Data.Repositories;

/// <summary>
/// Repository interface for SQL Server-based chat operations
/// </summary>
public interface ISqlChatRepository
{
    // Session operations
    Task<ChatSession> CreateSessionAsync(ChatSession session, CancellationToken cancellationToken = default);
    Task<ChatSession?> GetSessionByIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatSession>> GetSessionsByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<ChatSession> UpdateSessionAsync(ChatSession session, CancellationToken cancellationToken = default);

    // Participant operations
    Task<ChatParticipant> AddParticipantAsync(ChatParticipant participant, CancellationToken cancellationToken = default);
    Task<ChatParticipant?> GetParticipantAsync(Guid sessionId, string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatParticipant>> GetSessionParticipantsAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task RemoveParticipantAsync(Guid participantId, CancellationToken cancellationToken = default);
    Task<ChatParticipant> UpdateParticipantAsync(ChatParticipant participant, CancellationToken cancellationToken = default);

    // Message operations (transactional with outbox)
    Task<(ChatMessage message, ChatOutbox outboxEntry)> SaveMessageWithOutboxAsync(
        ChatMessage message, 
        ChatOutbox outboxEntry, 
        CancellationToken cancellationToken = default);

    Task<ChatMessage?> GetMessageByIdAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatMessage>> GetSessionMessagesAsync(
        Guid sessionId, 
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);
    Task<int> GetSessionMessageCountAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<ChatMessage> UpdateMessageStatusAsync(Guid messageId, ChatMessageStatus status, CancellationToken cancellationToken = default);

    // User operations
    Task<bool> IsUserParticipantAsync(Guid sessionId, string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetSessionParticipantUserIdsAsync(Guid sessionId, CancellationToken cancellationToken = default);
}