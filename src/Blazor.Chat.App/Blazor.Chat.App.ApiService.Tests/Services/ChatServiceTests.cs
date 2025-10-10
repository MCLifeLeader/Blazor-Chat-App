using Blazor.Chat.App.ApiService.Models;
using Blazor.Chat.App.ApiService.Services;
using Blazor.Chat.App.Data.Sql.Repositories;
using Blazor.Chat.App.Data.Cosmos.Repositories;
using Blazor.Chat.App.Data.Sql;
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
    private IOutboxRepository _outboxRepository = null!;
    private IChatCosmosRepository _cosmosRepository = null!;
    private ILogger<ChatService> _logger = null!;

    [SetUp]
    public void Setup()
    {
        _sqlRepository = Substitute.For<ISqlChatRepository>();
        _outboxRepository = Substitute.For<IOutboxRepository>();
        _cosmosRepository = Substitute.For<IChatCosmosRepository>();
        _logger = Substitute.For<ILogger<ChatService>>();

        _chatService = new ChatService(_sqlRepository, _outboxRepository, _cosmosRepository, _logger);
    }

    [Test]
    public void ChatService_Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        var service = new ChatService(_sqlRepository, _outboxRepository, _cosmosRepository, _logger);

        // Assert
        Assert.That(service, Is.Not.Null);
        Assert.That(service, Is.InstanceOf<IChatService>());
    }

    [Test]
    public async Task AddMessageAsync_ValidMessage_CallsRepositoryMethods()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var userId = "test-user-123";
        var messageDto = new AddMessageDto
        {
            Content = "Hello, world!",
            Attachments = new List<MessageAttachmentDto>()
        };

        // Mock that user is a participant
        _sqlRepository.IsUserParticipantAsync(sessionId, userId)
            .Returns(true);

        // Act
        var result = await _chatService.AddMessageAsync(sessionId, messageDto, userId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Status, Is.EqualTo("Accepted"));
        Assert.That(result.MessageId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(result.OutboxId, Is.Not.EqualTo(Guid.Empty));

        // Verify the repository was called to check participation
        await _sqlRepository.Received(1).IsUserParticipantAsync(sessionId, userId);
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

        // Act
        var result = await _chatService.CreateSessionAsync(createDto, userId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Title, Is.EqualTo(createDto.Title));
        Assert.That(result.IsGroup, Is.EqualTo(createDto.IsGroup));
        Assert.That(result.CreatedByUserId, Is.EqualTo(userId));
        Assert.That(result.Id, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public async Task GetSessionMessagesAsync_ValidRequest_ReturnsMessages()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var page = 1;
        var pageSize = 50;

        var cosmosPage = new CosmosMessagePage
        {
            Messages = new List<CosmosMessageDocument>(),
            Count = 0,
            HasMoreResults = false
        };

        // Setup Cosmos to return empty (will fallback to SQL)
        _cosmosRepository.GetSessionMessagesAsync(sessionId, page, pageSize)
            .Returns(cosmosPage);

        // Act
        var result = await _chatService.GetSessionMessagesAsync(sessionId, page, pageSize);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.PageNumber, Is.EqualTo(page));
        Assert.That(result.PageSize, Is.EqualTo(pageSize));

        // Verify it called Cosmos first
        await _cosmosRepository.Received(1).GetSessionMessagesAsync(sessionId, page, pageSize);
    }

    [Test]
    public async Task GetSessionMessagesAsync_WithCosmosData_MapsStringUserIdCorrectly()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var userId = "test-user-string-id-123"; // String user ID from Identity
        var page = 1;
        var pageSize = 50;

        var cosmosDocument = new CosmosMessageDocument
        {
            id = messageId.ToString(),
            sessionId = sessionId,
            senderUserId = userId, // String, not Guid
            senderDisplayName = "Test User",
            sentAt = DateTime.UtcNow,
            body = new MessageBody
            {
                content = "Test message content",
                messageType = "text"
            },
            attachments = new List<MessageAttachment>(),
            outboxId = Guid.NewGuid(),
            metadata = new MessageMetadata
            {
                version = 1,
                editHistory = new List<EditHistory>()
            },
            isDeleted = false,
            documentType = "message"
        };

        var cosmosPage = new CosmosMessagePage
        {
            Messages = new List<CosmosMessageDocument> { cosmosDocument },
            Count = 1,
            HasMoreResults = false
        };

        _cosmosRepository.GetSessionMessagesAsync(sessionId, page, pageSize)
            .Returns(cosmosPage);

        // Act
        var result = await _chatService.GetSessionMessagesAsync(sessionId, page, pageSize);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Messages, Has.Count.EqualTo(1));
        var message = result.Messages.First();
        Assert.That(message.SenderUserId, Is.EqualTo(userId)); // Should be string, not converted
        Assert.That(message.SenderDisplayName, Is.EqualTo("Test User"));
    }

    [Test]
    public async Task EditMessageAsync_StringUserId_DoesNotThrowGuidParseException()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var userId = "test-user-string-id-456"; // String user ID from Identity
        
        var editDto = new EditMessageDto
        {
            Content = "Updated content",
            Attachments = new List<MessageAttachmentDto>()
        };

        var existingMessage = new ChatMessage
        {
            Id = messageId,
            SessionId = sessionId,
            SenderUserId = userId, // String matches
            Preview = "Old preview",
            MessageLength = 100
        };

        var existingCosmosDoc = new CosmosMessageDocument
        {
            id = messageId.ToString(),
            sessionId = sessionId,
            senderUserId = userId,
            senderDisplayName = "Test User",
            sentAt = DateTime.UtcNow.AddMinutes(-10),
            body = new MessageBody
            {
                content = "Original content",
                messageType = "text"
            },
            attachments = new List<MessageAttachment>(),
            outboxId = Guid.NewGuid(),
            metadata = new MessageMetadata
            {
                version = 1,
                editHistory = new List<EditHistory>()
            }
        };

        _sqlRepository.GetMessageByIdAsync(messageId)
            .Returns(existingMessage);
        
        _cosmosRepository.GetMessageByIdAsync(sessionId, messageId)
            .Returns(existingCosmosDoc);

        // Act - should not throw Guid.Parse exception
        var result = await _chatService.EditMessageAsync(sessionId, messageId, editDto, userId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Status, Is.EqualTo("Accepted"));
        Assert.That(result.MessageId, Is.EqualTo(messageId));
        
        // Verify the repository was called to save the updated message
        await _sqlRepository.Received(1).SaveMessageWithOutboxAsync(
            Arg.Any<ChatMessage>(), 
            Arg.Any<ChatOutbox>(), 
            Arg.Any<CancellationToken>());
    }
}