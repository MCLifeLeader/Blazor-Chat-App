using Blazor.Chat.App.ApiService.Models;
using Blazor.Chat.App.Web;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using RichardSzalay.MockHttp;
using System.Net;
using System.Text.Json;

namespace Blazor.Chat.App.Web.Tests.Clients;

/// <summary>
/// Unit tests for the ChatApiClient class.
/// Tests HTTP client interactions using MockHttp to simulate API responses.
/// </summary>
[TestFixture]
public class ChatApiClientTests
{
    private MockHttpMessageHandler _mockHttp = null!;
    private HttpClient _httpClient = null!;
    private ChatApiClient _client = null!;

    // Use the shared JsonSerializerOptions from ChatApiClient to ensure consistency
    private JsonSerializerOptions JsonOptions => ChatApiClient.JsonOptions;

    [SetUp]
    public void Setup()
    {
        _mockHttp = new MockHttpMessageHandler();
        _httpClient = _mockHttp.ToHttpClient();
        _httpClient.BaseAddress = new Uri("https://localhost");
        _client = new ChatApiClient(_httpClient);
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient?.Dispose();
        _mockHttp?.Dispose();
    }

    #region CreateSessionAsync Tests

    [Test]
    public async Task CreateSessionAsync_WithValidRequest_ReturnsSessionDto()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new CreateSessionDto
        {
            Title = "Test Session",
            IsGroup = true,
            TenantId = "tenant-123"
        };

        var expectedResponse = new ChatSessionDto
        {
            Id = sessionId,
            Title = request.Title,
            IsGroup = request.IsGroup,
            TenantId = request.TenantId,
            CreatedByUserId = "user-123",
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            State = "Active",
            ParticipantCount = 1,
            MessageCount = 0
        };

        _mockHttp.When(HttpMethod.Post, "/api/chats")
            .Respond("application/json", JsonSerializer.Serialize(expectedResponse, JsonOptions));

        // Act
        var result = await _client.CreateSessionAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(sessionId);
        result.Title.Should().Be(request.Title);
        result.IsGroup.Should().Be(request.IsGroup);
        result.TenantId.Should().Be(request.TenantId);
    }

    [Test]
    public async Task CreateSessionAsync_WhenServerReturnsError_ThrowsHttpRequestException()
    {
        // Arrange
        var request = new CreateSessionDto { Title = "Test Session" };

        _mockHttp.When(HttpMethod.Post, "/api/chats")
            .Respond(HttpStatusCode.InternalServerError);

        // Act
        Func<Task> act = async () => await _client.CreateSessionAsync(request);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Test]
    public async Task CreateSessionAsync_WhenServerReturnsBadRequest_ThrowsHttpRequestException()
    {
        // Arrange
        var request = new CreateSessionDto { Title = "" }; // Invalid request

        _mockHttp.When(HttpMethod.Post, "/api/chats")
            .Respond(HttpStatusCode.BadRequest);

        // Act
        Func<Task> act = async () => await _client.CreateSessionAsync(request);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    #endregion

    #region SendMessageAsync Tests

    [Test]
    public async Task SendMessageAsync_WithValidRequest_ReturnsMessageResponse()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var outboxId = Guid.NewGuid();

        var request = new AddMessageDto
        {
            Content = "Test message",
            Attachments = new List<MessageAttachmentDto>()
        };

        var expectedResponse = new MessageOperationResponseDto
        {
            MessageId = messageId,
            OutboxId = outboxId,
            Status = "Accepted",
            Timestamp = DateTime.UtcNow
        };

        _mockHttp.When(HttpMethod.Post, $"/api/chats/{sessionId}/messages")
            .Respond("application/json", JsonSerializer.Serialize(expectedResponse, JsonOptions));

        // Act
        var result = await _client.SendMessageAsync(sessionId, request);

        // Assert
        result.Should().NotBeNull();
        result!.MessageId.Should().Be(messageId);
        result.OutboxId.Should().Be(outboxId);
        result.Status.Should().Be("Accepted");
    }

    [Test]
    public async Task SendMessageAsync_WhenServerReturnsUnauthorized_ThrowsHttpRequestException()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new AddMessageDto { Content = "Test" };

        _mockHttp.When(HttpMethod.Post, $"/api/chats/{sessionId}/messages")
            .Respond(HttpStatusCode.Forbidden);

        // Act
        Func<Task> act = async () => await _client.SendMessageAsync(sessionId, request);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    #endregion

    #region GetMessagesAsync Tests

    [Test]
    public async Task GetMessagesAsync_WithDefaultPagination_ReturnsMessages()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var expectedResponse = new PaginatedMessagesDto
        {
            Messages = new List<ChatMessageDto>
            {
                new() { Id = Guid.NewGuid(), Content = "Message 1" },
                new() { Id = Guid.NewGuid(), Content = "Message 2" }
            },
            Page = 1,
            PageSize = 50,
            TotalCount = 2,
            HasNextPage = false,
            HasPreviousPage = false
        };

        _mockHttp.When(HttpMethod.Get, $"/api/chats/{sessionId}/messages?page=1&pageSize=50")
            .Respond("application/json", JsonSerializer.Serialize(expectedResponse, JsonOptions));

        // Act
        var result = await _client.GetMessagesAsync(sessionId);

        // Assert
        result.Should().NotBeNull();
        result!.Messages.Should().HaveCount(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(50);
    }

    [Test]
    public async Task GetMessagesAsync_WithCustomPagination_ReturnsMessages()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var page = 2;
        var pageSize = 25;

        var expectedResponse = new PaginatedMessagesDto
        {
            Messages = new List<ChatMessageDto>(),
            Page = page,
            PageSize = pageSize,
            TotalCount = 100,
            HasNextPage = true,
            HasPreviousPage = true
        };

        _mockHttp.When(HttpMethod.Get, $"/api/chats/{sessionId}/messages?page={page}&pageSize={pageSize}")
            .Respond("application/json", JsonSerializer.Serialize(expectedResponse, JsonOptions));

        // Act
        var result = await _client.GetMessagesAsync(sessionId, page, pageSize);

        // Assert
        result.Should().NotBeNull();
        result!.Page.Should().Be(page);
        result.PageSize.Should().Be(pageSize);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeTrue();
    }

    [Test]
    public async Task GetMessagesAsync_WhenSessionNotFound_ThrowsHttpRequestException()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        _mockHttp.When(HttpMethod.Get, $"/api/chats/{sessionId}/messages")
            .Respond(HttpStatusCode.NotFound);

        // Act
        Func<Task> act = async () => await _client.GetMessagesAsync(sessionId);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    #endregion

    #region EditMessageAsync Tests

    [Test]
    public async Task EditMessageAsync_WithValidRequest_ReturnsMessageResponse()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        var request = new EditMessageDto
        {
            Content = "Updated content",
            Attachments = new List<MessageAttachmentDto>()
        };

        var expectedResponse = new MessageOperationResponseDto
        {
            MessageId = messageId,
            OutboxId = Guid.NewGuid(),
            Status = "Accepted",
            Timestamp = DateTime.UtcNow
        };

        _mockHttp.When(HttpMethod.Patch, $"/api/chats/{sessionId}/messages/{messageId}")
            .Respond("application/json", JsonSerializer.Serialize(expectedResponse, JsonOptions));

        // Act
        var result = await _client.EditMessageAsync(sessionId, messageId, request);

        // Assert
        result.Should().NotBeNull();
        result!.MessageId.Should().Be(messageId);
        result.Status.Should().Be("Accepted");
    }

    [Test]
    public async Task EditMessageAsync_WhenUnauthorized_ThrowsHttpRequestException()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var request = new EditMessageDto { Content = "Updated" };

        _mockHttp.When(HttpMethod.Patch, $"/api/chats/{sessionId}/messages/{messageId}")
            .Respond(HttpStatusCode.Forbidden);

        // Act
        Func<Task> act = async () => await _client.EditMessageAsync(sessionId, messageId, request);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    #endregion

    #region DeleteMessageAsync Tests

    [Test]
    public async Task DeleteMessageAsync_WithSuccess_ReturnsTrue()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        _mockHttp.When(HttpMethod.Delete, $"/api/chats/{sessionId}/messages/{messageId}")
            .Respond(HttpStatusCode.OK);

        // Act
        var result = await _client.DeleteMessageAsync(sessionId, messageId);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task DeleteMessageAsync_WhenNotFound_ReturnsFalse()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        _mockHttp.When(HttpMethod.Delete, $"/api/chats/{sessionId}/messages/{messageId}")
            .Respond(HttpStatusCode.NotFound);

        // Act
        var result = await _client.DeleteMessageAsync(sessionId, messageId);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task DeleteMessageAsync_WhenUnauthorized_ReturnsFalse()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        _mockHttp.When(HttpMethod.Delete, $"/api/chats/{sessionId}/messages/{messageId}")
            .Respond(HttpStatusCode.Forbidden);

        // Act
        var result = await _client.DeleteMessageAsync(sessionId, messageId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region AddParticipantAsync Tests

    [Test]
    public async Task AddParticipantAsync_WithSuccess_ReturnsTrue()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new AddParticipantDto
        {
            UserId = "user-456",
            Role = "Member"
        };

        _mockHttp.When(HttpMethod.Post, $"/api/chats/{sessionId}/participants")
            .Respond(HttpStatusCode.OK);

        // Act
        var result = await _client.AddParticipantAsync(sessionId, request);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task AddParticipantAsync_WhenFails_ReturnsFalse()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new AddParticipantDto { UserId = "user-456" };

        _mockHttp.When(HttpMethod.Post, $"/api/chats/{sessionId}/participants")
            .Respond(HttpStatusCode.BadRequest);

        // Act
        var result = await _client.AddParticipantAsync(sessionId, request);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region RemoveParticipantAsync Tests

    [Test]
    public async Task RemoveParticipantAsync_WithSuccess_ReturnsTrue()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        _mockHttp.When(HttpMethod.Delete, $"/api/chats/{sessionId}/participants/{participantId}")
            .Respond(HttpStatusCode.NoContent);

        // Act
        var result = await _client.RemoveParticipantAsync(sessionId, participantId);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task RemoveParticipantAsync_WhenNotFound_ReturnsFalse()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        _mockHttp.When(HttpMethod.Delete, $"/api/chats/{sessionId}/participants/{participantId}")
            .Respond(HttpStatusCode.NotFound);

        // Act
        var result = await _client.RemoveParticipantAsync(sessionId, participantId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Network Error Tests

    [Test]
    public async Task SendMessageAsync_OnNetworkTimeout_ThrowsTaskCanceledException()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new AddMessageDto { Content = "Test" };

        _mockHttp.When(HttpMethod.Post, $"/api/chats/{sessionId}/messages")
            .Respond(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(30));
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        Func<Task> act = async () => await _client.SendMessageAsync(sessionId, request, cts.Token);

        // Assert
        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    #endregion
}
