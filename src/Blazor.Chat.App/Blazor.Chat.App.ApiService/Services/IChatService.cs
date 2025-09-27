using Blazor.Chat.App.ApiService.Models;
using Blazor.Chat.App.Data.Db;

namespace Blazor.Chat.App.ApiService.Services;

/// <summary>
/// Service interface for chat operations that orchestrates between SQL and Cosmos repositories
/// </summary>
public interface IChatService
{
    // Session operations
    Task<ChatSessionDto> CreateSessionAsync(CreateSessionDto createSessionDto, string createdByUserId, CancellationToken cancellationToken = default);
    Task<ChatSessionDto?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatSessionDto>> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default);

    // Message operations
    Task<MessageOperationResponseDto> AddMessageAsync(Guid sessionId, AddMessageDto addMessageDto, string senderUserId, CancellationToken cancellationToken = default);
    Task<ChatMessagesPageDto> GetSessionMessagesAsync(Guid sessionId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<MessageOperationResponseDto> EditMessageAsync(Guid sessionId, Guid messageId, EditMessageDto editMessageDto, string userId, CancellationToken cancellationToken = default);
    Task<MessageOperationResponseDto> DeleteMessageAsync(Guid sessionId, Guid messageId, string userId, CancellationToken cancellationToken = default);

    // Participant operations
    Task<ChatParticipantDto> AddParticipantAsync(Guid sessionId, AddParticipantDto addParticipantDto, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatParticipantDto>> GetSessionParticipantsAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task RemoveParticipantAsync(Guid sessionId, string userId, CancellationToken cancellationToken = default);

    // Utility operations
    Task<bool> IsUserParticipantAsync(Guid sessionId, string userId, CancellationToken cancellationToken = default);
}