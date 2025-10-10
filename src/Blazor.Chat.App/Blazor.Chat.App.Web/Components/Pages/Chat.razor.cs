using Blazor.Chat.App.ApiService.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Blazor.Chat.App.Web.Components.Pages;

/// <summary>
/// Code-behind for the Chat page component.
/// Handles chat session management, message loading, and real-time updates.
/// </summary>
public partial class Chat : ComponentBase, IDisposable
{
    [Parameter] public Guid? SessionId { get; set; }

    private List<ChatSessionDto>? chatSessions;
    private ChatSessionDto? currentSession;
    private List<ChatMessageDto>? messages;
    private string? currentUserId;
    private string? errorMessage;
    private string? globalErrorMessage;
    
    private bool isLoadingSessions = false;
    private bool isLoadingMessages = false;
    private bool isSendingMessage = false;
    
    private Timer? messageRefreshTimer;
    private const int RefreshIntervalMs = 5000; // 5 seconds

    protected override async Task OnInitializedAsync()
    {
        await LoadCurrentUser();
        await LoadChatSessions();
        
        // Set up automatic message refresh
        messageRefreshTimer = new Timer(async _ => await RefreshMessages(), null, 
            TimeSpan.FromMilliseconds(RefreshIntervalMs), 
            TimeSpan.FromMilliseconds(RefreshIntervalMs));
    }

    protected override async Task OnParametersSetAsync()
    {
        if (SessionId.HasValue)
        {
            await LoadSession();
            await LoadMessages();
        }
    }

    private async Task LoadCurrentUser()
    {
        try
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            currentUserId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
        catch (Exception ex)
        {
            globalErrorMessage = "Failed to load user information.";
            Console.WriteLine($"Error loading user: {ex.Message}");
        }
    }

    private async Task LoadChatSessions()
    {
        isLoadingSessions = true;
        try
        {
            // For now, we'll create a mock list since we don't have a GetSessions endpoint yet
            // In a real implementation, this would call ChatApi.GetSessionsAsync()
            chatSessions = new List<ChatSessionDto>();
            
            // Create a sample session for demonstration
            if (!chatSessions.Any() && !string.IsNullOrEmpty(currentUserId))
            {
                var sampleSession = await CreateSampleSession();
                if (sampleSession is not null)
                {
                    chatSessions.Add(sampleSession);
                }
            }
        }
        catch (Exception ex)
        {
            globalErrorMessage = "Failed to load chat sessions.";
            Console.WriteLine($"Error loading sessions: {ex.Message}");
        }
        finally
        {
            isLoadingSessions = false;
            StateHasChanged();
        }
    }

    private async Task<ChatSessionDto?> CreateSampleSession()
    {
        try
        {
            var createRequest = new CreateSessionDto
            {
                Title = "General Chat",
                IsGroup = true
            };
            
            return await ChatApi.CreateSessionAsync(createRequest);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating sample session: {ex.Message}");
            return null;
        }
    }

    private async Task LoadSession()
    {
        if (!SessionId.HasValue) return;

        try
        {
            currentSession = chatSessions?.FirstOrDefault(s => s.Id == SessionId.Value);
            if (currentSession is null)
            {
                // Session not found in our list, might be a direct link
                globalErrorMessage = "Chat session not found.";
            }
        }
        catch (Exception ex)
        {
            globalErrorMessage = "Failed to load chat session.";
            Console.WriteLine($"Error loading session: {ex.Message}");
        }
    }

    private async Task LoadMessages()
    {
        if (!SessionId.HasValue) return;

        isLoadingMessages = true;
        errorMessage = string.Empty;
        
        try
        {
            var result = await ChatApi.GetMessagesAsync(SessionId.Value, page: 1, pageSize: 50);
            messages = result?.Messages?.ToList() ?? new List<ChatMessageDto>();
        }
        catch (Exception ex)
        {
            errorMessage = "Failed to load messages.";
            Console.WriteLine($"Error loading messages: {ex.Message}");
            messages = new List<ChatMessageDto>();
        }
        finally
        {
            isLoadingMessages = false;
            StateHasChanged();
        }
    }

    private async Task RefreshMessages()
    {
        if (!SessionId.HasValue || isLoadingMessages) return;

        try
        {
            var result = await ChatApi.GetMessagesAsync(SessionId.Value, page: 1, pageSize: 50);
            var newMessages = result?.Messages?.ToList() ?? new List<ChatMessageDto>();
            
            // Only update if there are actually new messages
            if (messages is null || newMessages.Count != messages.Count || 
                !newMessages.SequenceEqual(messages, new MessageComparer()))
            {
                messages = newMessages;
                await InvokeAsync(StateHasChanged);
            }
        }
        catch
        {
            // Silent fail for background refresh
        }
    }

    private async Task HandleSendMessage(AddMessageDto messageDto)
    {
        if (!SessionId.HasValue) return;

        isSendingMessage = true;
        errorMessage = string.Empty;
        
        try
        {
            var result = await ChatApi.SendMessageAsync(SessionId.Value, messageDto);
            if (result is not null)
            {
                // Refresh messages to show the new message
                await LoadMessages();
            }
        }
        catch (Exception ex)
        {
            errorMessage = "Failed to send message. Please try again.";
            Console.WriteLine($"Error sending message: {ex.Message}");
        }
        finally
        {
            isSendingMessage = false;
            StateHasChanged();
        }
    }

    private async Task HandleEditMessage(ChatMessageDto message)
    {
        // For now, we'll implement basic edit functionality
        // In a more advanced implementation, this could open an edit dialog
        var newContent = await JSRuntime.InvokeAsync<string>("prompt", 
            new object[] { "Edit your message:", message.Content });
        
        if (!string.IsNullOrEmpty(newContent) && newContent != message.Content)
        {
            try
            {
                var editRequest = new EditMessageDto { Content = newContent };
                await ChatApi.EditMessageAsync(SessionId!.Value, message.Id, editRequest);
                await LoadMessages(); // Refresh to show updated message
            }
            catch (Exception ex)
            {
                errorMessage = "Failed to edit message.";
                Console.WriteLine($"Error editing message: {ex.Message}");
            }
        }
    }

    private async Task HandleDeleteMessage(ChatMessageDto message)
    {
        var confirmed = await JSRuntime.InvokeAsync<bool>("confirm", 
            new object[] { "Are you sure you want to delete this message?" });
        
        if (confirmed)
        {
            try
            {
                await ChatApi.DeleteMessageAsync(SessionId!.Value, message.Id);
                await LoadMessages(); // Refresh to show message as deleted
            }
            catch (Exception ex)
            {
                errorMessage = "Failed to delete message.";
                Console.WriteLine($"Error deleting message: {ex.Message}");
            }
        }
    }

    private async Task CreateNewChat()
    {
        var title = await JSRuntime.InvokeAsync<string>("prompt", 
            new object[] { "Enter a title for the new chat:" });
        
        if (!string.IsNullOrEmpty(title))
        {
            try
            {
                var createRequest = new CreateSessionDto
                {
                    Title = title,
                    IsGroup = true
                };
                
                var newSession = await ChatApi.CreateSessionAsync(createRequest);
                if (newSession is not null)
                {
                    chatSessions ??= new List<ChatSessionDto>();
                    chatSessions.Add(newSession);
                    await SelectSession(newSession.Id);
                }
            }
            catch (Exception ex)
            {
                globalErrorMessage = "Failed to create new chat.";
                Console.WriteLine($"Error creating chat: {ex.Message}");
            }
        }
    }

    private async Task SelectSession(Guid sessionId)
    {
        SessionId = sessionId;
        await LoadSession();
        await LoadMessages();
        StateHasChanged();
    }

    public void Dispose()
    {
        messageRefreshTimer?.Dispose();
    }

    /// <summary>
    /// Comparer for checking if message lists have changed
    /// </summary>
    private class MessageComparer : IEqualityComparer<ChatMessageDto>
    {
        public bool Equals(ChatMessageDto? x, ChatMessageDto? y)
        {
            if (x is null && y is null) return true;
            if (x is null || y is null) return false;
            return x.Id == y.Id && x.Content == y.Content && x.EditedAt == y.EditedAt;
        }

        public int GetHashCode(ChatMessageDto obj)
        {
            return HashCode.Combine(obj.Id, obj.Content, obj.EditedAt);
        }
    }
}