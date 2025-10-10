using Blazor.Chat.App.ApiService.Models;
using Blazor.Chat.App.Data.Db;

namespace Blazor.Chat.App.ApiService.Services;

/// <summary>
/// Service interface for chat operations that orchestrates between SQL and Cosmos repositories
/// </summary>
public interface IChatService
{
    #region Session Operations

    /// <summary>
    /// Creates a new chat session with the specified parameters
    /// </summary>
    /// <param name="createSessionDto">Session creation parameters</param>
    /// <param name="createdByUserId">ID of the user creating the session</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created session details</returns>
    Task<ChatSessionDto> CreateSessionAsync(CreateSessionDto createSessionDto, string createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a chat session by its unique identifier
    /// </summary>
    /// <param name="sessionId">Unique session identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Session details if found, null otherwise</returns>
    Task<ChatSessionDto?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all chat sessions for a specific user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of user's chat sessions</returns>
    Task<IEnumerable<ChatSessionDto>> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default);

    #endregion

    #region Message Operations

    /// <summary>
    /// Adds a new message to a chat session
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="addMessageDto">Message content and metadata</param>
    /// <param name="senderUserId">ID of the user sending the message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Message operation response with acceptance details</returns>
    Task<MessageOperationResponseDto> AddMessageAsync(Guid sessionId, AddMessageDto addMessageDto, string senderUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves paginated messages for a chat session
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of messages per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated message list</returns>
    Task<ChatMessagesPageDto> GetSessionMessagesAsync(Guid sessionId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Edits an existing message in a chat session
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="messageId">Message identifier</param>
    /// <param name="editMessageDto">Updated message content</param>
    /// <param name="userId">ID of the user editing the message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Message operation response</returns>
    Task<MessageOperationResponseDto> EditMessageAsync(Guid sessionId, Guid messageId, EditMessageDto editMessageDto, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a message from a chat session
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="messageId">Message identifier</param>
    /// <param name="userId">ID of the user deleting the message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Message operation response</returns>
    Task<MessageOperationResponseDto> DeleteMessageAsync(Guid sessionId, Guid messageId, string userId, CancellationToken cancellationToken = default);

    #endregion

    #region Participant Operations

    /// <summary>
    /// Adds a new participant to a chat session
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="addParticipantDto">Participant details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Added participant details</returns>
    Task<ChatParticipantDto> AddParticipantAsync(Guid sessionId, AddParticipantDto addParticipantDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all participants for a chat session
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of session participants</returns>
    Task<IEnumerable<ChatParticipantDto>> GetSessionParticipantsAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a participant from a chat session
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="userId">User identifier to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task RemoveParticipantAsync(Guid sessionId, string userId, CancellationToken cancellationToken = default);

    #endregion

    #region Utility Operations

    /// <summary>
    /// Checks if a user is a participant in a chat session
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="userId">User identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user is a participant, false otherwise</returns>
    Task<bool> IsUserParticipantAsync(Guid sessionId, string userId, CancellationToken cancellationToken = default);

    #endregion
}