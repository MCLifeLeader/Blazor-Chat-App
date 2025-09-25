using Blazor.Chat.App.Data.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Blazor.Chat.App.Data.Repositories
{
    /// <summary>
    /// SQL Server implementation of chat repository using Entity Framework Core
    /// </summary>
    public class SqlChatRepository : ISqlChatRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SqlChatRepository> _logger;

        public SqlChatRepository(ApplicationDbContext context, ILogger<SqlChatRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Session Operations

        public async Task<ChatSession> CreateSessionAsync(ChatSession session, CancellationToken cancellationToken = default)
        {
            _context.ChatSessions.Add(session);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created chat session {SessionId} with title '{Title}'", session.Id, session.Title);
            return session;
        }

        public async Task<ChatSession?> GetSessionByIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            return await _context.ChatSessions
                .Include(s => s.CreatedByUser)
                .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        }

        public async Task<IEnumerable<ChatSession>> GetSessionsByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _context.ChatSessions
                .Include(s => s.CreatedByUser)
                .Where(s => s.Participants.Any(p => p.UserId == userId && p.LeftAt == null))
                .OrderByDescending(s => s.LastActivityAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<ChatSession> UpdateSessionAsync(ChatSession session, CancellationToken cancellationToken = default)
        {
            _context.ChatSessions.Update(session);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated chat session {SessionId}", session.Id);
            return session;
        }

        #endregion

        #region Participant Operations

        public async Task<ChatParticipant> AddParticipantAsync(ChatParticipant participant, CancellationToken cancellationToken = default)
        {
            _context.ChatParticipants.Add(participant);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Added participant {UserId} to session {SessionId}", participant.UserId, participant.SessionId);
            return participant;
        }

        public async Task<ChatParticipant?> GetParticipantAsync(Guid sessionId, string userId, CancellationToken cancellationToken = default)
        {
            return await _context.ChatParticipants
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.SessionId == sessionId && p.UserId == userId, cancellationToken);
        }

        public async Task<IEnumerable<ChatParticipant>> GetSessionParticipantsAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            return await _context.ChatParticipants
                .Include(p => p.User)
                .Where(p => p.SessionId == sessionId && p.LeftAt == null)
                .OrderBy(p => p.JoinedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task RemoveParticipantAsync(Guid participantId, CancellationToken cancellationToken = default)
        {
            var participant = await _context.ChatParticipants.FindAsync(new object[] { participantId }, cancellationToken);
            if (participant is not null)
            {
                participant.LeftAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Removed participant {ParticipantId} from session {SessionId}", participantId, participant.SessionId);
            }
        }

        public async Task<ChatParticipant> UpdateParticipantAsync(ChatParticipant participant, CancellationToken cancellationToken = default)
        {
            _context.ChatParticipants.Update(participant);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated participant {ParticipantId}", participant.Id);
            return participant;
        }

        #endregion

        #region Message Operations

        public async Task<(ChatMessage message, ChatOutbox outboxEntry)> SaveMessageWithOutboxAsync(
            ChatMessage message, 
            ChatOutbox outboxEntry, 
            CancellationToken cancellationToken = default)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Add both message and outbox entry in the same transaction
                _context.ChatMessages.Add(message);
                _context.ChatOutbox.Add(outboxEntry);

                // Update session last activity
                var session = await _context.ChatSessions.FindAsync(new object[] { message.SessionId }, cancellationToken);
                if (session is not null)
                {
                    session.LastActivityAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Saved message {MessageId} with outbox entry {OutboxId} for session {SessionId}", 
                    message.Id, outboxEntry.Id, message.SessionId);

                return (message, outboxEntry);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to save message {MessageId} with outbox entry {OutboxId}", 
                    message.Id, outboxEntry.Id);
                throw;
            }
        }

        public async Task<ChatMessage?> GetMessageByIdAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            return await _context.ChatMessages
                .Include(m => m.SenderUser)
                .Include(m => m.ReplyToMessage)
                .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);
        }

        public async Task<IEnumerable<ChatMessage>> GetSessionMessagesAsync(
            Guid sessionId, 
            int skip = 0, 
            int take = 50, 
            CancellationToken cancellationToken = default)
        {
            return await _context.ChatMessages
                .Include(m => m.SenderUser)
                .Include(m => m.ReplyToMessage)
                .Where(m => m.SessionId == sessionId && m.MessageStatus != ChatMessageStatus.Deleted)
                .OrderByDescending(m => m.SentAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetSessionMessageCountAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            return await _context.ChatMessages
                .CountAsync(m => m.SessionId == sessionId && m.MessageStatus != ChatMessageStatus.Deleted, cancellationToken);
        }

        public async Task<ChatMessage> UpdateMessageStatusAsync(Guid messageId, ChatMessageStatus status, CancellationToken cancellationToken = default)
        {
            var message = await _context.ChatMessages.FindAsync(new object[] { messageId }, cancellationToken);
            if (message is null)
            {
                throw new InvalidOperationException($"Message {messageId} not found");
            }

            message.MessageStatus = status;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated message {MessageId} status to {Status}", messageId, status);
            return message;
        }

        #endregion

        #region User Operations

        public async Task<bool> IsUserParticipantAsync(Guid sessionId, string userId, CancellationToken cancellationToken = default)
        {
            return await _context.ChatParticipants
                .AnyAsync(p => p.SessionId == sessionId && p.UserId == userId && p.LeftAt == null, cancellationToken);
        }

        public async Task<IEnumerable<string>> GetSessionParticipantUserIdsAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            return await _context.ChatParticipants
                .Where(p => p.SessionId == sessionId && p.LeftAt == null)
                .Select(p => p.UserId)
                .ToListAsync(cancellationToken);
        }

        #endregion
    }
}