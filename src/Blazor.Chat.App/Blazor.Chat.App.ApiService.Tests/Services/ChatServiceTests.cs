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
}