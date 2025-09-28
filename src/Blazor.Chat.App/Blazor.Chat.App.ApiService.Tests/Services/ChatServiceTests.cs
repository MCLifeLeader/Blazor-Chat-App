using Blazor.Chat.App.ApiService.Models;
using Blazor.Chat.App.ApiService.Services;
using Blazor.Chat.App.Data.Sql.Repositories;
using Blazor.Chat.App.Data.Cosmos.Repositories;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace Blazor.Chat.App.ApiService.Tests.Services;

/// <summary>
/// Unit tests for the ChatService class.
/// Tests the core functionality of chat operations including message sending,
/// session management, and the transactional outbox pattern.
/// </summary>
[TestFixture]
public class ChatServiceTests
{
    private IChatService _chatService = null!;
    private ISqlChatRepository _sqlRepository = null!;
    private IChatCosmosRepository _cosmosRepository = null!;
    private ILogger<ChatService> _logger = null!;

    [SetUp]
    public void Setup()
    {
        _sqlRepository = Substitute.For<ISqlChatRepository>();
        _cosmosRepository = Substitute.For<IChatCosmosRepository>();
        _logger = Substitute.For<ILogger<ChatService>>();

        _chatService = new ChatService(_sqlRepository, _cosmosRepository, _logger);
    }

    [Test]
    public async Task AddMessageAsync_ValidMessage_ReturnsSuccessResult()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var userId = "test-user-123";
        var messageDto = new AddMessageDto
        {
            Content = "Hello, world!",
            Attachments = new List<MessageAttachmentDto>()
        };

        var expectedOutboxId = Guid.NewGuid();
        var expectedMessageId = Guid.NewGuid();

        _sqlRepository.SaveMessageWithOutboxAsync(sessionId, userId, messageDto.Content, messageDto.Attachments)
            .Returns(new MessageOperationResponseDto
            {
                MessageId = expectedMessageId,
                OutboxId = expectedOutboxId,
                Success = true
            });

        // Act
        var result = await _chatService.AddMessageAsync(sessionId, messageDto, userId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Success, Is.True);
        Assert.That(result.MessageId, Is.EqualTo(expectedMessageId));
        Assert.That(result.OutboxId, Is.EqualTo(expectedOutboxId));

        await _sqlRepository.Received(1).SaveMessageWithOutboxAsync(
            sessionId, userId, messageDto.Content, messageDto.Attachments);
    }

    [Test]
    public async Task CreateSessionAsync_ValidRequest_ReturnsSessionDto()
    {
        // Arrange
        var userId = "test-user-123";
        var createDto = new CreateSessionDto
        {
            Title = "Test Session",
            IsGroup = true
        };

        var expectedSessionId = Guid.NewGuid();
        var expectedSession = new ChatSessionDto
        {
            Id = expectedSessionId,
            Title = createDto.Title,
            IsGroup = createDto.IsGroup,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };

        _sqlRepository.CreateSessionAsync(userId, createDto.Title, createDto.IsGroup)
            .Returns(expectedSession);

        // Act
        var result = await _chatService.CreateSessionAsync(createDto, userId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(expectedSessionId));
        Assert.That(result.Title, Is.EqualTo(createDto.Title));
        Assert.That(result.IsGroup, Is.EqualTo(createDto.IsGroup));
        Assert.That(result.CreatedByUserId, Is.EqualTo(userId));

        await _sqlRepository.Received(1).CreateSessionAsync(userId, createDto.Title, createDto.IsGroup);
    }

    [Test]
    public async Task GetSessionMessagesAsync_ValidRequest_ReturnsChatMessagesPage()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var page = 1;
        var pageSize = 50;

        var expectedMessages = new List<ChatMessageDto>
        {
            new ChatMessageDto
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                Content = "Test message 1",
                SenderUserId = "user-1",
                SenderDisplayName = "User One",
                SentAt = DateTime.UtcNow.AddMinutes(-10)
            }
        };

        var expectedResult = new ChatMessagesPageDto
        {
            Messages = expectedMessages,
            TotalCount = expectedMessages.Count,
            Page = page,
            PageSize = pageSize,
            TotalPages = 1
        };

        // Setup Cosmos to return messages
        _cosmosRepository.GetMessagesAsync(sessionId, page, pageSize)
            .Returns(expectedResult);

        // Act
        var result = await _chatService.GetSessionMessagesAsync(sessionId, page, pageSize);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Messages.Count(), Is.EqualTo(1));
        Assert.That(result.Page, Is.EqualTo(page));
        Assert.That(result.PageSize, Is.EqualTo(pageSize));

        // Verify it called Cosmos
        await _cosmosRepository.Received(1).GetMessagesAsync(sessionId, page, pageSize);
    }

    [Test]
    public void ChatService_Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        var service = new ChatService(_sqlRepository, _cosmosRepository, _logger);

        // Assert
        Assert.That(service, Is.Not.Null);
        Assert.That(service, Is.InstanceOf<IChatService>());
    }
}