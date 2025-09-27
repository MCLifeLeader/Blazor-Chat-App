using Blazor.Chat.App.ApiService.Models;
using Blazor.Chat.App.Data.Db;
using Blazor.Chat.App.Data.Repositories;
using Blazor.Chat.App.ServiceDefaults.Repositories;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Blazor.Chat.App.ApiService.Services;

/// <summary>
/// Chat service implementation that orchestrates between SQL and Cosmos repositories using the outbox pattern
/// </summary>
public class ChatService : IChatService
{
    private readonly ISqlChatRepository _sqlChatRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IChatCosmosRepository _cosmosRepository;
    private readonly ILogger<ChatService> _logger;

    /// <summary>
    /// Initializes a new instance of the ChatService class
    /// </summary>
    /// <param name="sqlChatRepository">Repository for SQL Server chat operations</param>
    /// <param name="outboxRepository">Repository for outbox pattern management</param>
    /// <param name="cosmosRepository">Repository for Cosmos DB operations</param>
    /// <param name="logger">Logger instance</param>
    public ChatService(
        ISqlChatRepository sqlChatRepository,
        IOutboxRepository outboxRepository,
        IChatCosmosRepository cosmosRepository,
        ILogger<ChatService> logger)
    {
        _sqlChatRepository = sqlChatRepository ?? throw new ArgumentNullException(nameof(sqlChatRepository));
        _outboxRepository = outboxRepository ?? throw new ArgumentNullException(nameof(outboxRepository));
        _cosmosRepository = cosmosRepository ?? throw new ArgumentNullException(nameof(cosmosRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Session Operations

    /// <inheritdoc />
    public async Task<ChatSessionDto> CreateSessionAsync(CreateSessionDto createSessionDto, string createdByUserId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new chat session '{Title}' for user {UserId}", createSessionDto.Title, createdByUserId);

        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            Title = createSessionDto.Title,
            IsGroup = createSessionDto.IsGroup,
            CreatedByUserId = createdByUserId,
            TenantId = createSessionDto.TenantId,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };

        var createdSession = await _sqlChatRepository.CreateSessionAsync(session, cancellationToken);

        // Add creator as participant
        var creatorParticipant = new ChatParticipant
        {
            SessionId = createdSession.Id,
            UserId = createdByUserId,
            Role = ChatParticipantRole.Owner
        };

        await _sqlChatRepository.AddParticipantAsync(creatorParticipant, cancellationToken);

        // Add initial participants
        foreach (var userId in createSessionDto.InitialParticipantIds)
        {
            if (userId != createdByUserId) // Don't add creator twice
            {
                var participant = new ChatParticipant
                {
                    SessionId = createdSession.Id,
                    UserId = userId,
                    Role = ChatParticipantRole.Member
                };
                await _sqlChatRepository.AddParticipantAsync(participant, cancellationToken);
            }
        }

        _logger.LogInformation("Created chat session {SessionId} with {ParticipantCount} participants", 
            createdSession.Id, createSessionDto.InitialParticipantIds.Count + 1);

        return MapToSessionDto(createdSession, createSessionDto.InitialParticipantIds.Count + 1, 0);
    }

    /// <inheritdoc />
    public async Task<ChatSessionDto?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sqlChatRepository.GetSessionByIdAsync(sessionId, cancellationToken);
        if (session is null)
            return null;

        var participants = await _sqlChatRepository.GetSessionParticipantsAsync(sessionId, cancellationToken);
        var messageCount = await _sqlChatRepository.GetSessionMessageCountAsync(sessionId, cancellationToken);

        return MapToSessionDto(session, participants.Count(), messageCount);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ChatSessionDto>> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var sessions = await _sqlChatRepository.GetSessionsByUserIdAsync(userId, cancellationToken);
        var sessionDtos = new List<ChatSessionDto>();

        foreach (var session in sessions)
        {
            var participants = await _sqlChatRepository.GetSessionParticipantsAsync(session.Id, cancellationToken);
            var messageCount = await _sqlChatRepository.GetSessionMessageCountAsync(session.Id, cancellationToken);
            sessionDtos.Add(MapToSessionDto(session, participants.Count(), messageCount));
        }

        return sessionDtos;
    }

    #endregion

    #region Message Operations

    /// <inheritdoc />
    public async Task<MessageOperationResponseDto> AddMessageAsync(Guid sessionId, AddMessageDto addMessageDto, string senderUserId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding message to session {SessionId} from user {UserId}", sessionId, senderUserId);

        // Verify user is participant
        if (!await _sqlChatRepository.IsUserParticipantAsync(sessionId, senderUserId, cancellationToken))
        {
            throw new UnauthorizedAccessException($"User {senderUserId} is not a participant in session {sessionId}");
        }

        var messageId = Guid.NewGuid();
        var outboxId = Guid.NewGuid();

        // Create message metadata
        var message = new ChatMessage
        {
            Id = messageId,
            SessionId = sessionId,
            SenderUserId = senderUserId,
            SentAt = DateTime.UtcNow,
            Preview = TruncateToPreview(addMessageDto.Content),
            MessageLength = addMessageDto.Content.Length,
            MessageStatus = ChatMessageStatus.Pending,
            OutboxId = outboxId,
            ReplyToMessageId = addMessageDto.ReplyToMessageId
        };

        // Create Cosmos document for outbox
        var cosmosDocument = new CosmosMessageDocument
        {
            id = messageId.ToString(),
            sessionId = sessionId,
            senderUserId = Guid.Parse(senderUserId),
            sentAt = message.SentAt,
            body = new MessageBody
            {
                content = addMessageDto.Content,
                messageType = addMessageDto.MessageType ?? "text",
                replyToMessageId = addMessageDto.ReplyToMessageId
            },
            attachments = addMessageDto.Attachments.Select(a => new MessageAttachment
            {
                fileName = a.FileName,
                contentType = a.ContentType,
                size = a.Size,
                url = a.Url
            }).ToList(),
            outboxId = outboxId
        };

        // Create outbox entry
        var outboxEntry = new ChatOutbox
        {
            Id = outboxId,
            MessageType = "message-created",
            PayloadJson = JsonSerializer.Serialize(cosmosDocument),
            SessionId = sessionId,
            MessageId = messageId,
            CreatedAt = DateTime.UtcNow,
            Status = ChatOutboxStatus.Pending
        };

        // Save both in transaction
        await _sqlChatRepository.SaveMessageWithOutboxAsync(message, outboxEntry, cancellationToken);

        _logger.LogInformation("Saved message {MessageId} with outbox entry {OutboxId}", messageId, outboxId);

        return new MessageOperationResponseDto
        {
            MessageId = messageId,
            OutboxId = outboxId,
            Status = "Accepted",
            Timestamp = DateTime.UtcNow
        };
    }

    /// <inheritdoc />
    public async Task<ChatMessagesPageDto> GetSessionMessagesAsync(Guid sessionId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting messages for session {SessionId}, page {Page}, pageSize {PageSize}", sessionId, page, pageSize);

        try
        {
            // Try to get from Cosmos first (fast read)
            var cosmosPage = await _cosmosRepository.GetSessionMessagesAsync(sessionId, page, pageSize, cancellationToken);
                
            if (cosmosPage.Messages.Any())
            {
                var messages = cosmosPage.Messages.Select(MapFromCosmosDocument).ToList();
                    
                return new ChatMessagesPageDto
                {
                    Messages = messages,
                    TotalCount = cosmosPage.Count, // This might not be accurate in Cosmos, but good enough for UI
                    PageNumber = page,
                    PageSize = pageSize,
                    HasNextPage = cosmosPage.HasMoreResults,
                    HasPreviousPage = page > 1
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve messages from Cosmos for session {SessionId}, falling back to SQL", sessionId);
        }

        // Fallback to SQL if Cosmos fails or has no data
        var skip = (page - 1) * pageSize;
        var sqlMessages = await _sqlChatRepository.GetSessionMessagesAsync(sessionId, skip, pageSize, cancellationToken);
        var totalCount = await _sqlChatRepository.GetSessionMessageCountAsync(sessionId, cancellationToken);

        return new ChatMessagesPageDto
        {
            Messages = sqlMessages.Select(MapFromSqlMessage).ToList(),
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
            HasNextPage = (page * pageSize) < totalCount,
            HasPreviousPage = page > 1
        };
    }

    /// <inheritdoc />
    public async Task<MessageOperationResponseDto> EditMessageAsync(Guid sessionId, Guid messageId, EditMessageDto editMessageDto, string userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Editing message {MessageId} in session {SessionId} by user {UserId}", messageId, sessionId, userId);

        var message = await _sqlChatRepository.GetMessageByIdAsync(messageId, cancellationToken);
        if (message is null)
        {
            throw new ArgumentException($"Message {messageId} not found");
        }

        if (message.SenderUserId != userId)
        {
            throw new UnauthorizedAccessException($"User {userId} cannot edit message {messageId}");
        }

        var outboxId = Guid.NewGuid();

        // Update message metadata
        message.Preview = TruncateToPreview(editMessageDto.Content);
        message.MessageLength = editMessageDto.Content.Length;
        message.EditedAt = DateTime.UtcNow;
        message.Version++;

        // Create updated Cosmos document
        var existingCosmosDoc = await _cosmosRepository.GetMessageByIdAsync(sessionId, messageId, cancellationToken);
        if (existingCosmosDoc is not null)
        {
            var editedCosmosDoc = existingCosmosDoc with
            {
                body = existingCosmosDoc.body with { content = editMessageDto.Content },
                attachments = editMessageDto.Attachments.Select(a => new MessageAttachment
                {
                    fileName = a.FileName,
                    contentType = a.ContentType,
                    size = a.Size,
                    url = a.Url
                }).ToList(),
                outboxId = outboxId,
                metadata = existingCosmosDoc.metadata with
                {
                    editedAt = DateTime.UtcNow,
                    version = message.Version,
                    editHistory = existingCosmosDoc.metadata.editHistory.Concat(new[]
                    {
                        new EditHistory
                        {
                            editedAt = DateTime.UtcNow,
                            previousContent = existingCosmosDoc.body.content,
                            editedByUserId = Guid.Parse(userId)
                        }
                    }).ToList()
                }
            };

            // Create outbox entry for edit
            var outboxEntry = new ChatOutbox
            {
                Id = outboxId,
                MessageType = "message-edited",
                PayloadJson = JsonSerializer.Serialize(editedCosmosDoc),
                SessionId = sessionId,
                MessageId = messageId,
                CreatedAt = DateTime.UtcNow,
                Status = ChatOutboxStatus.Pending
            };

            // Save SQL changes and outbox entry
            await _sqlChatRepository.SaveMessageWithOutboxAsync(message, outboxEntry, cancellationToken);
        }

        return new MessageOperationResponseDto
        {
            MessageId = messageId,
            OutboxId = outboxId,
            Status = "Accepted",
            Timestamp = DateTime.UtcNow
        };
    }

    /// <inheritdoc />
    public async Task<MessageOperationResponseDto> DeleteMessageAsync(Guid sessionId, Guid messageId, string userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting message {MessageId} in session {SessionId} by user {UserId}", messageId, sessionId, userId);

        var message = await _sqlChatRepository.GetMessageByIdAsync(messageId, cancellationToken);
        if (message is null)
        {
            throw new ArgumentException($"Message {messageId} not found");
        }

        if (message.SenderUserId != userId)
        {
            throw new UnauthorizedAccessException($"User {userId} cannot delete message {messageId}");
        }

        var outboxId = Guid.NewGuid();

        // Mark message as deleted in SQL
        await _sqlChatRepository.UpdateMessageStatusAsync(messageId, ChatMessageStatus.Deleted, cancellationToken);

        // Create outbox entry for deletion
        var outboxEntry = new ChatOutbox
        {
            Id = outboxId,
            MessageType = "message-deleted",
            PayloadJson = JsonSerializer.Serialize(new { messageId, sessionId, deletedBy = userId, deletedAt = DateTime.UtcNow }),
            SessionId = sessionId,
            MessageId = messageId,
            CreatedAt = DateTime.UtcNow,
            Status = ChatOutboxStatus.Pending
        };

        // Save outbox entry
        await _outboxRepository.CreateOutboxEntryAsync(outboxEntry, cancellationToken);

        return new MessageOperationResponseDto
        {
            MessageId = messageId,
            OutboxId = outboxId,
            Status = "Accepted",
            Timestamp = DateTime.UtcNow
        };
    }

    #endregion

    #region Participant Operations

    /// <inheritdoc />
    public async Task<ChatParticipantDto> AddParticipantAsync(Guid sessionId, AddParticipantDto addParticipantDto, CancellationToken cancellationToken = default)
    {
        var participant = new ChatParticipant
        {
            SessionId = sessionId,
            UserId = addParticipantDto.UserId,
            Role = Enum.Parse<ChatParticipantRole>(addParticipantDto.Role),
            JoinedAt = DateTime.UtcNow
        };

        var addedParticipant = await _sqlChatRepository.AddParticipantAsync(participant, cancellationToken);

        return new ChatParticipantDto
        {
            Id = addedParticipant.Id,
            SessionId = addedParticipant.SessionId,
            UserId = addedParticipant.UserId,
            DisplayName = addedParticipant.User?.UserName ?? "Unknown",
            JoinedAt = addedParticipant.JoinedAt,
            Role = addedParticipant.Role.ToString(),
            IsMuted = addedParticipant.IsMuted
        };
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ChatParticipantDto>> GetSessionParticipantsAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var participants = await _sqlChatRepository.GetSessionParticipantsAsync(sessionId, cancellationToken);

        return participants.Select(p => new ChatParticipantDto
        {
            Id = p.Id,
            SessionId = p.SessionId,
            UserId = p.UserId,
            DisplayName = p.User?.UserName ?? "Unknown",
            JoinedAt = p.JoinedAt,
            LeftAt = p.LeftAt,
            Role = p.Role.ToString(),
            IsMuted = p.IsMuted,
            LastReadMessageId = p.LastReadMessageId,
            LastReadAt = p.LastReadAt
        });
    }

    /// <inheritdoc />
    public async Task RemoveParticipantAsync(Guid sessionId, string userId, CancellationToken cancellationToken = default)
    {
        var participant = await _sqlChatRepository.GetParticipantAsync(sessionId, userId, cancellationToken);
        if (participant is not null)
        {
            await _sqlChatRepository.RemoveParticipantAsync(participant.Id, cancellationToken);
        }
    }

    #endregion

    #region Utility Operations

    /// <inheritdoc />
    public async Task<bool> IsUserParticipantAsync(Guid sessionId, string userId, CancellationToken cancellationToken = default)
    {
        return await _sqlChatRepository.IsUserParticipantAsync(sessionId, userId, cancellationToken);
    }

    #endregion

    #region Mapping Methods

    /// <summary>
    /// Maps a ChatSession entity to a ChatSessionDto
    /// </summary>
    /// <param name="session">The session entity to map</param>
    /// <param name="participantCount">Number of participants in the session</param>
    /// <param name="messageCount">Number of messages in the session</param>
    /// <returns>Mapped session DTO</returns>
    private static ChatSessionDto MapToSessionDto(ChatSession session, int participantCount, int messageCount)
    {
        return new ChatSessionDto
        {
            Id = session.Id,
            Title = session.Title,
            IsGroup = session.IsGroup,
            CreatedByUserId = session.CreatedByUserId,
            CreatedAt = session.CreatedAt,
            LastActivityAt = session.LastActivityAt,
            TenantId = session.TenantId,
            State = session.State.ToString(),
            ParticipantCount = participantCount,
            MessageCount = messageCount
        };
    }

    /// <summary>
    /// Maps a Cosmos DB message document to a ChatMessageDto
    /// </summary>
    /// <param name="doc">The Cosmos message document to map</param>
    /// <returns>Mapped message DTO</returns>
    private static ChatMessageDto MapFromCosmosDocument(CosmosMessageDocument doc)
    {
        return new ChatMessageDto
        {
            Id = Guid.Parse(doc.id),
            SessionId = doc.sessionId,
            SenderUserId = doc.senderUserId.ToString(),
            SenderDisplayName = doc.senderDisplayName,
            SentAt = doc.sentAt,
            Content = doc.body.content,
            Preview = TruncateToPreview(doc.body.content),
            MessageLength = doc.body.content.Length,
            Status = "Sent", // Cosmos messages are sent
            ReplyToMessageId = doc.body.replyToMessageId,
            EditedAt = doc.metadata.editedAt,
            Version = doc.metadata.version,
            Attachments = doc.attachments.Select(a => new MessageAttachmentDto
            {
                FileName = a.fileName,
                ContentType = a.contentType,
                Size = a.size,
                Url = a.url
            }).ToList(),
            MessageType = doc.body.messageType
        };
    }

    /// <summary>
    /// Maps a SQL ChatMessage entity to a ChatMessageDto
    /// </summary>
    /// <param name="message">The SQL message entity to map</param>
    /// <returns>Mapped message DTO</returns>
    private static ChatMessageDto MapFromSqlMessage(ChatMessage message)
    {
        return new ChatMessageDto
        {
            Id = message.Id,
            SessionId = message.SessionId,
            SenderUserId = message.SenderUserId,
            SenderDisplayName = message.SenderUser?.UserName ?? "Unknown",
            SentAt = message.SentAt,
            Content = "Content available in full chat view", // SQL only has preview
            Preview = message.Preview,
            MessageLength = message.MessageLength,
            Status = message.MessageStatus.ToString(),
            ReplyToMessageId = message.ReplyToMessageId,
            EditedAt = message.EditedAt,
            Version = message.Version,
            Attachments = new List<MessageAttachmentDto>(), // Would need separate table for attachments in SQL
            MessageType = "text"
        };
    }

    /// <summary>
    /// Truncates content to create a preview string
    /// </summary>
    /// <param name="content">The content to truncate</param>
    /// <param name="maxLength">Maximum length for the preview</param>
    /// <returns>Truncated content with ellipsis if needed</returns>
    private static string TruncateToPreview(string content, int maxLength = 500)
    {
        if (content.Length <= maxLength)
        {
            return content;
        }

        return content[..maxLength] + "...";
    }

    #endregion
}