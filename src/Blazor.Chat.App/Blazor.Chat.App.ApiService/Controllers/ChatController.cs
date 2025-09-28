using Blazor.Chat.App.ApiService.Models;
using Blazor.Chat.App.ApiService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Blazor.Chat.App.ApiService.Controllers;

/// <summary>
/// REST API controller for chat operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    /// <summary>
    /// Initializes a new instance of the ChatController class
    /// </summary>
    /// <param name="chatService">Chat service for business logic</param>
    /// <param name="logger">Logger instance</param>
    public ChatController(IChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a new chat session
    /// </summary>
    /// <param name="request">Session creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created session details</returns>
    [HttpPost("sessions")]
    public async Task<ActionResult<ChatSessionDto>> CreateSession(
        [FromBody] CreateSessionDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var session = await _chatService.CreateSessionAsync(request, userId, cancellationToken);
            
            _logger.LogInformation("Created chat session {SessionId} for user {UserId}", session.Id, userId);
            return CreatedAtAction(nameof(GetSession), new { sessionId = session.Id }, session);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid request for session creation: {Error}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating chat session");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get a specific chat session
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Session details</returns>
    [HttpGet("sessions/{sessionId:guid}")]
    public async Task<ActionResult<ChatSessionDto>> GetSession(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var session = await _chatService.GetSessionAsync(sessionId, cancellationToken);
            
            if (session == null)
            {
                return NotFound($"Session {sessionId} not found or access denied");
            }
            
            return Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session {SessionId}", sessionId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Add a message to a chat session
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="request">Message details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Message operation response</returns>
    [HttpPost("sessions/{sessionId:guid}/messages")]
    public async Task<ActionResult<MessageOperationResponseDto>> AddMessage(
        Guid sessionId,
        [FromBody] AddMessageDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _chatService.AddMessageAsync(sessionId, request, userId, cancellationToken);
            
            _logger.LogInformation("Added message {MessageId} to session {SessionId}", response.MessageId, sessionId);
            return Accepted(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("Not authorized to send messages to this session");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid message request: {Error}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding message to session {SessionId}", sessionId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get messages for a chat session
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of messages per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of messages</returns>
    [HttpGet("sessions/{sessionId:guid}/messages")]
    public async Task<ActionResult<PaginatedMessagesDto>> GetMessages(
        Guid sessionId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            var userId = GetCurrentUserId();
            var messages = await _chatService.GetSessionMessagesAsync(sessionId, page, pageSize, cancellationToken);
            
            return Ok(messages);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("Not authorized to view messages in this session");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving messages for session {SessionId}", sessionId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Edit a message
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="messageId">Message identifier</param>
    /// <param name="request">Updated message content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Message operation response</returns>
    [HttpPatch("sessions/{sessionId:guid}/messages/{messageId:guid}")]
    public async Task<ActionResult<MessageOperationResponseDto>> EditMessage(
        Guid sessionId,
        Guid messageId,
        [FromBody] EditMessageDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _chatService.EditMessageAsync(sessionId, messageId, request, userId, cancellationToken);
            
            _logger.LogInformation("Edited message {MessageId} in session {SessionId}", messageId, sessionId);
            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("Not authorized to edit this message");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid edit request: {Error}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing message {MessageId}", messageId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete a message
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="messageId">Message identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Message operation response</returns>
    [HttpDelete("sessions/{sessionId:guid}/messages/{messageId:guid}")]
    public async Task<ActionResult<MessageOperationResponseDto>> DeleteMessage(
        Guid sessionId,
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _chatService.DeleteMessageAsync(sessionId, messageId, userId, cancellationToken);
            
            _logger.LogInformation("Deleted message {MessageId} from session {SessionId}", messageId, sessionId);
            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("Not authorized to delete this message");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid delete request: {Error}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting message {MessageId}", messageId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Add a participant to a chat session
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="request">Participant details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Participant details</returns>
    [HttpPost("sessions/{sessionId:guid}/participants")]
    public async Task<ActionResult<ChatParticipantDto>> AddParticipant(
        Guid sessionId,
        [FromBody] AddParticipantDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var participant = await _chatService.AddParticipantAsync(sessionId, request, cancellationToken);
            
            _logger.LogInformation("Added participant {ParticipantUserId} to session {SessionId}", request.UserId, sessionId);
            return Ok(participant);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("Not authorized to add participants to this session");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid participant request: {Error}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding participant to session {SessionId}", sessionId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Remove a participant from a chat session
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="participantId">Participant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("sessions/{sessionId:guid}/participants/{participantId:guid}")]
    public async Task<ActionResult> RemoveParticipant(
        Guid sessionId,
        Guid participantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _chatService.RemoveParticipantAsync(participantId, userId, cancellationToken);
            
            _logger.LogInformation("Removed participant {ParticipantId} from session {SessionId}", participantId, sessionId);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("Not authorized to remove participants from this session");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid remove participant request: {Error}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing participant {ParticipantId}", participantId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get current user ID from claims
    /// </summary>
    /// <returns>User ID</returns>
    private string GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            throw new UnauthorizedAccessException("User ID not found in claims");
        }
        return userIdClaim;
    }
}