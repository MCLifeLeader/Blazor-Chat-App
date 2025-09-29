using System.Text.Json;
using System.Text;
using Blazor.Chat.App.ApiService.Models;

namespace Blazor.Chat.App.Web;

/// <summary>
/// Client for communicating with the Chat API endpoints.
/// Provides methods for chat session management, messaging, and participant operations.
/// </summary>
public class ChatApiClient(HttpClient httpClient)
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Creates a new chat session.
    /// </summary>
    /// <param name="request">The session creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created session details</returns>
    public async Task<ChatSessionDto?> CreateSessionAsync(CreateSessionDto request, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await httpClient.PostAsync("/api/chats", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<ChatSessionDto>(responseJson, _jsonOptions);
    }

    /// <summary>
    /// Sends a message to a chat session.
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="request">The message request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The message operation response</returns>
    public async Task<MessageOperationResponseDto?> SendMessageAsync(Guid sessionId, AddMessageDto request, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await httpClient.PostAsync($"/api/chats/{sessionId}/messages", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<MessageOperationResponseDto>(responseJson, _jsonOptions);
    }

    /// <summary>
    /// Gets messages for a chat session with pagination.
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of messages per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated messages</returns>
    public async Task<PaginatedMessagesDto?> GetMessagesAsync(Guid sessionId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"/api/chats/{sessionId}/messages?page={page}&pageSize={pageSize}", cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<PaginatedMessagesDto>(responseJson, _jsonOptions);
    }

    /// <summary>
    /// Edits an existing message.
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="messageId">The message ID</param>
    /// <param name="request">The edit request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The message operation response</returns>
    public async Task<MessageOperationResponseDto?> EditMessageAsync(Guid sessionId, Guid messageId, EditMessageDto request, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await httpClient.PatchAsync($"/api/chats/{sessionId}/messages/{messageId}", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<MessageOperationResponseDto>(responseJson, _jsonOptions);
    }

    /// <summary>
    /// Deletes a message.
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="messageId">The message ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    public async Task<bool> DeleteMessageAsync(Guid sessionId, Guid messageId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"/api/chats/{sessionId}/messages/{messageId}", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Adds a participant to a chat session.
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="request">The participant request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    public async Task<bool> AddParticipantAsync(Guid sessionId, AddParticipantDto request, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await httpClient.PostAsync($"/api/chats/{sessionId}/participants", content, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Removes a participant from a chat session.
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="participantId">The participant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    public async Task<bool> RemoveParticipantAsync(Guid sessionId, Guid participantId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"/api/chats/{sessionId}/participants/{participantId}", cancellationToken);
        return response.IsSuccessStatusCode;
    }
}