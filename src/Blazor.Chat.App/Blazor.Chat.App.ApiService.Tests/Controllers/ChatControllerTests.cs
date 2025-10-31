using Blazor.Chat.App.ApiService.Controllers;
using Blazor.Chat.App.ApiService.Models;
using Blazor.Chat.App.ApiService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using System.Security.Claims;

namespace Blazor.Chat.App.ApiService.Tests.Controllers;

/// <summary>
/// Unit tests for the ChatController class.
/// Tests HTTP endpoints without accessing the database by mocking the ChatService.
/// </summary>
[TestFixture]
public class ChatControllerTests
{
    private ChatController _controller = null!;
    private IChatService _chatService = null!;
    private ILogger<ChatController> _logger = null!;
    private const string TestUserId = "test-user-123";

    [SetUp]
    public void Setup()
    {
        _chatService = Substitute.For<IChatService>();
        _logger = Substitute.For<ILogger<ChatController>>();
        _controller = new ChatController(_chatService, _logger);

        // Setup authenticated user context
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    #region Constructor Tests

    [Test]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        var controller = new ChatController(_chatService, _logger);

        Assert.That(controller, Is.Not.Null);
        Assert.That(controller, Is.InstanceOf<ChatController>());
    }

    [Test]
    public void Constructor_WithNullChatService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ChatController(null!, _logger));
    }

    [Test]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ChatController(_chatService, null!));
    }

    #endregion

    #region CreateSession Tests

    [Test]
    public async Task CreateSession_WithValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new CreateSessionDto
        {
            Title = "Test Session",
            IsGroup = true,
            TenantId = "tenant-1"
        };

        var expectedSession = new ChatSessionDto
        {
            Id = sessionId,
            Title = request.Title,
            IsGroup = request.IsGroup,
            CreatedByUserId = TestUserId,
            TenantId = request.TenantId,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            State = "Active",
            ParticipantCount = 1,
            MessageCount = 0
        };

        _chatService.CreateSessionAsync(request, TestUserId, Arg.Any<CancellationToken>())
            .Returns(expectedSession);

        // Act
        var result = await _controller.CreateSession(request);

        // Assert
        Assert.That(result, Is.Not.Null);
        var createdResult = result.Result as CreatedAtActionResult;
        Assert.That(createdResult, Is.Not.Null);
        Assert.That(createdResult!.StatusCode, Is.EqualTo(201));
        
        var returnedSession = createdResult.Value as ChatSessionDto;
        Assert.That(returnedSession, Is.Not.Null);
        Assert.That(returnedSession!.Id, Is.EqualTo(sessionId));
        Assert.That(returnedSession.Title, Is.EqualTo(request.Title));

        await _chatService.Received(1).CreateSessionAsync(request, TestUserId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateSession_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateSessionDto { Title = "" };
        _chatService.CreateSessionAsync(Arg.Any<CreateSessionDto>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ArgumentException("Title is required"));

        // Act
        var result = await _controller.CreateSession(request);

        // Assert
        var badRequestResult = result.Result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
        Assert.That(badRequestResult!.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task CreateSession_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new CreateSessionDto { Title = "Test" };
        _chatService.CreateSessionAsync(Arg.Any<CreateSessionDto>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreateSession(request);

        // Assert
        var statusCodeResult = result.Result as ObjectResult;
        Assert.That(statusCodeResult, Is.Not.Null);
        Assert.That(statusCodeResult!.StatusCode, Is.EqualTo(500));
    }

    #endregion

    #region GetSession Tests

    [Test]
    public async Task GetSession_WithExistingSession_ReturnsOk()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var expectedSession = new ChatSessionDto
        {
            Id = sessionId,
            Title = "Test Session",
            IsGroup = true,
            CreatedByUserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };

        _chatService.GetSessionAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(expectedSession);

        // Act
        var result = await _controller.GetSession(sessionId);

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.StatusCode, Is.EqualTo(200));

        var returnedSession = okResult.Value as ChatSessionDto;
        Assert.That(returnedSession, Is.Not.Null);
        Assert.That(returnedSession!.Id, Is.EqualTo(sessionId));
    }

    [Test]
    public async Task GetSession_WithNonExistingSession_ReturnsNotFound()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _chatService.GetSessionAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns((ChatSessionDto?)null);

        // Act
        var result = await _controller.GetSession(sessionId);

        // Assert
        var notFoundResult = result.Result as NotFoundObjectResult;
        Assert.That(notFoundResult, Is.Not.Null);
        Assert.That(notFoundResult!.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task GetSession_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _chatService.GetSessionAsync(sessionId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetSession(sessionId);

        // Assert
        var statusCodeResult = result.Result as ObjectResult;
        Assert.That(statusCodeResult, Is.Not.Null);
        Assert.That(statusCodeResult!.StatusCode, Is.EqualTo(500));
    }

    #endregion

    #region AddMessage Tests

    [Test]
    public async Task AddMessage_WithValidRequest_ReturnsAccepted()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var outboxId = Guid.NewGuid();
        var request = new AddMessageDto
        {
            Content = "Hello, world!",
            Attachments = new List<MessageAttachmentDto>()
        };

        var expectedResponse = new MessageOperationResponseDto
        {
            MessageId = messageId,
            OutboxId = outboxId,
            Status = "Accepted",
            Timestamp = DateTime.UtcNow
        };

        _chatService.AddMessageAsync(sessionId, request, TestUserId, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _controller.AddMessage(sessionId, request);

        // Assert
        var acceptedResult = result.Result as AcceptedResult;
        Assert.That(acceptedResult, Is.Not.Null);
        Assert.That(acceptedResult!.StatusCode, Is.EqualTo(202));

        var response = acceptedResult.Value as MessageOperationResponseDto;
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.MessageId, Is.EqualTo(messageId));
        Assert.That(response.Status, Is.EqualTo("Accepted"));
    }

    [Test]
    public async Task AddMessage_WhenUserNotAuthorized_ReturnsForbid()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new AddMessageDto { Content = "Test" };

        _chatService.AddMessageAsync(sessionId, request, TestUserId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new UnauthorizedAccessException("Not a participant"));

        // Act
        var result = await _controller.AddMessage(sessionId, request);

        // Assert
        var forbidResult = result.Result as ForbidResult;
        Assert.That(forbidResult, Is.Not.Null);
    }

    [Test]
    public async Task AddMessage_WithInvalidContent_ReturnsBadRequest()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new AddMessageDto { Content = "" };

        _chatService.AddMessageAsync(sessionId, request, TestUserId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ArgumentException("Content is required"));

        // Act
        var result = await _controller.AddMessage(sessionId, request);

        // Assert
        var badRequestResult = result.Result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
        Assert.That(badRequestResult!.StatusCode, Is.EqualTo(400));
    }

    #endregion

    #region GetMessages Tests

    [Test]
    public async Task GetMessages_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var page = 1;
        var pageSize = 50;

        var expectedResult = new ChatMessagesPageDto
        {
            Messages = new List<ChatMessageDto>(),
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = 0,
            HasNextPage = false,
            HasPreviousPage = false
        };

        _chatService.GetSessionMessagesAsync(sessionId, page, pageSize, Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _controller.GetMessages(sessionId, page, pageSize);

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.StatusCode, Is.EqualTo(200));

        var messages = okResult.Value as ChatMessagesPageDto;
        Assert.That(messages, Is.Not.Null);
        Assert.That(messages!.PageNumber, Is.EqualTo(page));
    }

    [Test]
    public async Task GetMessages_WithInvalidPage_NormalizesToOne()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var page = -1;
        var pageSize = 50;

        var expectedResult = new ChatMessagesPageDto
        {
            Messages = new List<ChatMessageDto>(),
            PageNumber = 1,
            PageSize = pageSize,
            TotalCount = 0,
            HasNextPage = false,
            HasPreviousPage = false
        };

        _chatService.GetSessionMessagesAsync(sessionId, 1, pageSize, Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _controller.GetMessages(sessionId, page, pageSize);

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);

        await _chatService.Received(1).GetSessionMessagesAsync(sessionId, 1, Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetMessages_WithInvalidPageSize_NormalizesToFifty()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var page = 1;
        var pageSize = 200; // Over max

        _chatService.GetSessionMessagesAsync(sessionId, page, 50, Arg.Any<CancellationToken>())
            .Returns(new ChatMessagesPageDto());

        // Act
        var result = await _controller.GetMessages(sessionId, page, pageSize);

        // Assert
        await _chatService.Received(1).GetSessionMessagesAsync(sessionId, page, 50, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetMessages_WhenUserNotAuthorized_ReturnsForbid()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _chatService.GetSessionMessagesAsync(sessionId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.GetMessages(sessionId);

        // Assert
        var forbidResult = result.Result as ForbidResult;
        Assert.That(forbidResult, Is.Not.Null);
    }

    #endregion

    #region EditMessage Tests

    [Test]
    public async Task EditMessage_WithValidRequest_ReturnsOk()
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

        _chatService.EditMessageAsync(sessionId, messageId, request, TestUserId, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _controller.EditMessage(sessionId, messageId, request);

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.StatusCode, Is.EqualTo(200));
    }

    [Test]
    public async Task EditMessage_WhenUserNotAuthorized_ReturnsForbid()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var request = new EditMessageDto { Content = "Updated" };

        _chatService.EditMessageAsync(sessionId, messageId, request, TestUserId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.EditMessage(sessionId, messageId, request);

        // Assert
        var forbidResult = result.Result as ForbidResult;
        Assert.That(forbidResult, Is.Not.Null);
    }

    [Test]
    public async Task EditMessage_WithInvalidContent_ReturnsBadRequest()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var request = new EditMessageDto { Content = "" };

        _chatService.EditMessageAsync(sessionId, messageId, request, TestUserId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ArgumentException("Content is required"));

        // Act
        var result = await _controller.EditMessage(sessionId, messageId, request);

        // Assert
        var badRequestResult = result.Result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
    }

    #endregion

    #region DeleteMessage Tests

    [Test]
    public async Task DeleteMessage_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        var expectedResponse = new MessageOperationResponseDto
        {
            MessageId = messageId,
            OutboxId = Guid.NewGuid(),
            Status = "Accepted",
            Timestamp = DateTime.UtcNow
        };

        _chatService.DeleteMessageAsync(sessionId, messageId, TestUserId, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _controller.DeleteMessage(sessionId, messageId);

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.StatusCode, Is.EqualTo(200));
    }

    [Test]
    public async Task DeleteMessage_WhenUserNotAuthorized_ReturnsForbid()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        _chatService.DeleteMessageAsync(sessionId, messageId, TestUserId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.DeleteMessage(sessionId, messageId);

        // Assert
        var forbidResult = result.Result as ForbidResult;
        Assert.That(forbidResult, Is.Not.Null);
    }

    [Test]
    public async Task DeleteMessage_WhenMessageNotFound_ReturnsBadRequest()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        _chatService.DeleteMessageAsync(sessionId, messageId, TestUserId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ArgumentException("Message not found"));

        // Act
        var result = await _controller.DeleteMessage(sessionId, messageId);

        // Assert
        var badRequestResult = result.Result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
    }

    #endregion

    #region AddParticipant Tests

    [Test]
    public async Task AddParticipant_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var participantUserId = "user-456";
        var request = new AddParticipantDto
        {
            UserId = participantUserId,
            Role = "Member"
        };

        var expectedParticipant = new ChatParticipantDto
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            UserId = participantUserId,
            DisplayName = "Test User",
            Role = "Member",
            JoinedAt = DateTime.UtcNow
        };

        _chatService.AddParticipantAsync(sessionId, request, Arg.Any<CancellationToken>())
            .Returns(expectedParticipant);

        // Act
        var result = await _controller.AddParticipant(sessionId, request);

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.StatusCode, Is.EqualTo(200));

        var participant = okResult.Value as ChatParticipantDto;
        Assert.That(participant, Is.Not.Null);
        Assert.That(participant!.UserId, Is.EqualTo(participantUserId));
    }

    [Test]
    public async Task AddParticipant_WhenUserNotAuthorized_ReturnsForbid()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new AddParticipantDto { UserId = "user-456" };

        _chatService.AddParticipantAsync(sessionId, request, Arg.Any<CancellationToken>())
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.AddParticipant(sessionId, request);

        // Assert
        var forbidResult = result.Result as ForbidResult;
        Assert.That(forbidResult, Is.Not.Null);
    }

    [Test]
    public async Task AddParticipant_WithInvalidUserId_ReturnsBadRequest()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new AddParticipantDto { UserId = "" };

        _chatService.AddParticipantAsync(sessionId, request, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ArgumentException("UserId is required"));

        // Act
        var result = await _controller.AddParticipant(sessionId, request);

        // Assert
        var badRequestResult = result.Result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
    }

    #endregion

    #region RemoveParticipant Tests

    [Test]
    public async Task RemoveParticipant_WithValidRequest_ReturnsNoContent()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        _chatService.RemoveParticipantAsync(participantId, TestUserId, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.RemoveParticipant(sessionId, participantId);

        // Assert
        var noContentResult = result as NoContentResult;
        Assert.That(noContentResult, Is.Not.Null);
        Assert.That(noContentResult!.StatusCode, Is.EqualTo(204));
    }

    [Test]
    public async Task RemoveParticipant_WhenUserNotAuthorized_ReturnsForbid()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        _chatService.RemoveParticipantAsync(participantId, TestUserId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.RemoveParticipant(sessionId, participantId);

        // Assert
        var forbidResult = result as ForbidResult;
        Assert.That(forbidResult, Is.Not.Null);
    }

    [Test]
    public async Task RemoveParticipant_WhenParticipantNotFound_ReturnsBadRequest()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        _chatService.RemoveParticipantAsync(participantId, TestUserId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ArgumentException("Participant not found"));

        // Act
        var result = await _controller.RemoveParticipant(sessionId, participantId);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
    }

    #endregion
}
