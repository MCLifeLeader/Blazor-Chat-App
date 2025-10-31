using Blazor.Chat.App.Data.Sql;
using Blazor.Chat.App.Data.Sql.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace Blazor.Chat.App.Data.Tests.Repositories;

/// <summary>
/// Unit tests for SqlChatRepository using EF Core InMemory database.
/// Tests repository operations without requiring a real SQL Server database.
/// </summary>
[TestFixture]
public class SqlChatRepositoryTests
{
    private ApplicationDbContext _context = null!;
    private SqlChatRepository _repository = null!;
    private ILogger<SqlChatRepository> _logger = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);
        _logger = Substitute.For<ILogger<SqlChatRepository>>();
        _repository = new SqlChatRepository(_context, _logger);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region Constructor Tests

    [Test]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        var repository = new SqlChatRepository(_context, _logger);

        Assert.That(repository, Is.Not.Null);
        Assert.That(repository, Is.InstanceOf<ISqlChatRepository>());
    }

    [Test]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlChatRepository(null!, _logger));
    }

    [Test]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlChatRepository(_context, null!));
    }

    #endregion

    #region Session CRUD Tests

    [Test]
    public async Task CreateSessionAsync_WithValidSession_ReturnsCreatedSession()
    {
        // Arrange
        var userId = "test-user-123";
        await SeedTestUserAsync(userId);

        var session = new ChatSession
        {
            Title = "Test Session",
            IsGroup = true,
            CreatedByUserId = userId,
            TenantId = "tenant-1"
        };

        // Act
        var result = await _repository.CreateSessionAsync(session);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(result.Title, Is.EqualTo("Test Session"));
        Assert.That(result.IsGroup, Is.True);
        Assert.That(result.CreatedByUserId, Is.EqualTo(userId));

        var savedSession = await _context.ChatSessions.FindAsync(result.Id);
        Assert.That(savedSession, Is.Not.Null);
        Assert.That(savedSession!.Title, Is.EqualTo("Test Session"));
    }

    [Test]
    public async Task GetSessionByIdAsync_WithExistingSession_ReturnsSession()
    {
        // Arrange
        var userId = "test-user-123";
        await SeedTestUserAsync(userId);

        var session = new ChatSession
        {
            Title = "Test Session",
            IsGroup = false,
            CreatedByUserId = userId
        };
        _context.ChatSessions.Add(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetSessionByIdAsync(session.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(session.Id));
        Assert.That(result.Title, Is.EqualTo("Test Session"));
    }

    [Test]
    public async Task GetSessionByIdAsync_WithNonExistingSession_ReturnsNull()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _repository.GetSessionByIdAsync(nonExistingId);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetSessionsByUserIdAsync_WithActiveParticipant_ReturnsSessions()
    {
        // Arrange
        var userId = "test-user-123";
        await SeedTestUserAsync(userId);

        var session1 = new ChatSession
        {
            Title = "Session 1",
            CreatedByUserId = userId,
            LastActivityAt = DateTime.UtcNow
        };

        var session2 = new ChatSession
        {
            Title = "Session 2",
            CreatedByUserId = userId,
            LastActivityAt = DateTime.UtcNow.AddHours(-1)
        };

        _context.ChatSessions.AddRange(session1, session2);
        await _context.SaveChangesAsync();

        _context.ChatParticipants.Add(new ChatParticipant
        {
            SessionId = session1.Id,
            UserId = userId,
            LeftAt = null
        });

        _context.ChatParticipants.Add(new ChatParticipant
        {
            SessionId = session2.Id,
            UserId = userId,
            LeftAt = null
        });

        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetSessionsByUserIdAsync(userId);

        // Assert
        var sessionList = results.ToList();
        Assert.That(sessionList, Has.Count.EqualTo(2));
        Assert.That(sessionList[0].Title, Is.EqualTo("Session 1")); // Most recent first
        Assert.That(sessionList[1].Title, Is.EqualTo("Session 2"));
    }

    [Test]
    public async Task GetSessionsByUserIdAsync_ExcludesLeftSessions()
    {
        // Arrange
        var userId = "test-user-123";
        await SeedTestUserAsync(userId);

        var activeSession = new ChatSession
        {
            Title = "Active Session",
            CreatedByUserId = userId
        };

        var leftSession = new ChatSession
        {
            Title = "Left Session",
            CreatedByUserId = userId
        };

        _context.ChatSessions.AddRange(activeSession, leftSession);
        await _context.SaveChangesAsync();

        _context.ChatParticipants.Add(new ChatParticipant
        {
            SessionId = activeSession.Id,
            UserId = userId,
            LeftAt = null
        });

        _context.ChatParticipants.Add(new ChatParticipant
        {
            SessionId = leftSession.Id,
            UserId = userId,
            LeftAt = DateTime.UtcNow.AddDays(-1)
        });

        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetSessionsByUserIdAsync(userId);

        // Assert
        var sessionList = results.ToList();
        Assert.That(sessionList, Has.Count.EqualTo(1));
        Assert.That(sessionList[0].Title, Is.EqualTo("Active Session"));
    }

    [Test]
    public async Task UpdateSessionAsync_WithValidSession_UpdatesSession()
    {
        // Arrange
        var userId = "test-user-123";
        await SeedTestUserAsync(userId);

        var session = new ChatSession
        {
            Title = "Original Title",
            CreatedByUserId = userId,
            State = ChatSessionState.Active
        };
        _context.ChatSessions.Add(session);
        await _context.SaveChangesAsync();

        session.Title = "Updated Title";
        session.State = ChatSessionState.Archived;

        // Act
        var result = await _repository.UpdateSessionAsync(session);

        // Assert
        Assert.That(result.Title, Is.EqualTo("Updated Title"));
        Assert.That(result.State, Is.EqualTo(ChatSessionState.Archived));

        var updatedSession = await _context.ChatSessions.FindAsync(session.Id);
        Assert.That(updatedSession!.Title, Is.EqualTo("Updated Title"));
    }

    #endregion

    #region Participant Tests

    [Test]
    public async Task AddParticipantAsync_WithValidParticipant_AddsParticipant()
    {
        // Arrange
        var userId = "test-user-123";
        await SeedTestUserAsync(userId);

        var session = await CreateTestSessionAsync(userId);
        var participant = new ChatParticipant
        {
            SessionId = session.Id,
            UserId = userId,
            Role = ChatParticipantRole.Admin
        };

        // Act
        var result = await _repository.AddParticipantAsync(participant);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(result.UserId, Is.EqualTo(userId));
        Assert.That(result.Role, Is.EqualTo(ChatParticipantRole.Admin));

        var savedParticipant = await _context.ChatParticipants.FindAsync(result.Id);
        Assert.That(savedParticipant, Is.Not.Null);
    }

    [Test]
    public async Task GetParticipantAsync_WithExistingParticipant_ReturnsParticipant()
    {
        // Arrange
        var userId = "test-user-123";
        await SeedTestUserAsync(userId);

        var session = await CreateTestSessionAsync(userId);
        var participant = new ChatParticipant
        {
            SessionId = session.Id,
            UserId = userId
        };
        _context.ChatParticipants.Add(participant);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetParticipantAsync(session.Id, userId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.SessionId, Is.EqualTo(session.Id));
        Assert.That(result.UserId, Is.EqualTo(userId));
    }

    [Test]
    public async Task GetSessionParticipantsAsync_ReturnsOnlyActiveParticipants()
    {
        // Arrange
        var user1 = "user-1";
        var user2 = "user-2";
        var user3 = "user-3";

        await SeedTestUserAsync(user1);
        await SeedTestUserAsync(user2);
        await SeedTestUserAsync(user3);

        var session = await CreateTestSessionAsync(user1);

        var participant1 = new ChatParticipant
        {
            SessionId = session.Id,
            UserId = user1,
            LeftAt = null
        };

        var participant2 = new ChatParticipant
        {
            SessionId = session.Id,
            UserId = user2,
            LeftAt = null
        };

        var participant3 = new ChatParticipant
        {
            SessionId = session.Id,
            UserId = user3,
            LeftAt = DateTime.UtcNow.AddDays(-1)
        };

        _context.ChatParticipants.AddRange(participant1, participant2, participant3);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetSessionParticipantsAsync(session.Id);

        // Assert
        var participantList = results.ToList();
        Assert.That(participantList, Has.Count.EqualTo(2));
        Assert.That(participantList.Any(p => p.UserId == user1), Is.True);
        Assert.That(participantList.Any(p => p.UserId == user2), Is.True);
        Assert.That(participantList.Any(p => p.UserId == user3), Is.False);
    }

    [Test]
    public async Task RemoveParticipantAsync_MarksParticipantAsLeft()
    {
        // Arrange
        var userId = "test-user-123";
        await SeedTestUserAsync(userId);

        var session = await CreateTestSessionAsync(userId);
        var participant = new ChatParticipant
        {
            SessionId = session.Id,
            UserId = userId
        };
        _context.ChatParticipants.Add(participant);
        await _context.SaveChangesAsync();

        // Act
        await _repository.RemoveParticipantAsync(participant.Id);

        // Assert
        var updatedParticipant = await _context.ChatParticipants.FindAsync(participant.Id);
        Assert.That(updatedParticipant, Is.Not.Null);
        Assert.That(updatedParticipant!.LeftAt, Is.Not.Null);
        Assert.That(updatedParticipant.LeftAt!.Value, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public async Task UpdateParticipantAsync_UpdatesParticipantProperties()
    {
        // Arrange
        var userId = "test-user-123";
        await SeedTestUserAsync(userId);

        var session = await CreateTestSessionAsync(userId);
        var participant = new ChatParticipant
        {
            SessionId = session.Id,
            UserId = userId,
            IsMuted = false,
            Role = ChatParticipantRole.Member
        };
        _context.ChatParticipants.Add(participant);
        await _context.SaveChangesAsync();

        participant.IsMuted = true;
        participant.Role = ChatParticipantRole.Admin;

        // Act
        var result = await _repository.UpdateParticipantAsync(participant);

        // Assert
        Assert.That(result.IsMuted, Is.True);
        Assert.That(result.Role, Is.EqualTo(ChatParticipantRole.Admin));

        var updatedParticipant = await _context.ChatParticipants.FindAsync(participant.Id);
        Assert.That(updatedParticipant!.IsMuted, Is.True);
        Assert.That(updatedParticipant.Role, Is.EqualTo(ChatParticipantRole.Admin));
    }

    #endregion

    #region Message Tests

    [Test]
    public async Task SaveMessageWithOutboxAsync_SavesBothInTransaction()
    {
        // Arrange
        var userId = "test-user-123";
        await SeedTestUserAsync(userId);

        var session = await CreateTestSessionAsync(userId);
        var messageId = Guid.NewGuid();

        var message = new ChatMessage
        {
            Id = messageId,
            SessionId = session.Id,
            SenderUserId = userId,
            Preview = "Test message",
            MessageLength = 12,
            MessageStatus = ChatMessageStatus.Pending
        };

        var outbox = new ChatOutbox
        {
            MessageId = messageId,
            MessageType = "ChatMessage",
            PayloadJson = "{}",
            Status = ChatOutboxStatus.Pending
        };

        // Act
        var result = await _repository.SaveMessageWithOutboxAsync(message, outbox);

        // Assert
        Assert.That(result.message, Is.Not.Null);
        Assert.That(result.outboxEntry, Is.Not.Null);
        Assert.That(result.message.Id, Is.EqualTo(messageId));
        Assert.That(result.outboxEntry.MessageId, Is.EqualTo(messageId));

        var savedMessage = await _context.ChatMessages.FindAsync(messageId);
        var savedOutbox = await _context.ChatOutbox.FirstOrDefaultAsync(o => o.MessageId == messageId);

        Assert.That(savedMessage, Is.Not.Null);
        Assert.That(savedOutbox, Is.Not.Null);
    }

    [Test]
    public async Task GetMessageByIdAsync_WithExistingMessage_ReturnsMessage()
    {
        // Arrange
        var userId = "test-user-123";
        await SeedTestUserAsync(userId);

        var session = await CreateTestSessionAsync(userId);
        var message = new ChatMessage
        {
            SessionId = session.Id,
            SenderUserId = userId,
            Preview = "Test message",
            MessageLength = 12
        };
        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetMessageByIdAsync(message.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(message.Id));
        Assert.That(result.Preview, Is.EqualTo("Test message"));
    }

    [Test]
    public async Task GetSessionMessagesAsync_ReturnsPaginatedMessages()
    {
        // Arrange
        var userId = "test-user-123";
        await SeedTestUserAsync(userId);

        var session = await CreateTestSessionAsync(userId);

        for (int i = 0; i < 10; i++)
        {
            var message = new ChatMessage
            {
                SessionId = session.Id,
                SenderUserId = userId,
                Preview = $"Message {i}",
                MessageLength = 10,
                SentAt = DateTime.UtcNow.AddMinutes(-i)
            };
            _context.ChatMessages.Add(message);
        }
        await _context.SaveChangesAsync();

        // Act - Get first page of 5 messages
        var results = await _repository.GetSessionMessagesAsync(session.Id, skip: 0, take: 5);

        // Assert
        var messageList = results.ToList();
        Assert.That(messageList, Has.Count.EqualTo(5));
    }

    [Test]
    public async Task GetSessionMessageCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var userId = "test-user-123";
        await SeedTestUserAsync(userId);

        var session = await CreateTestSessionAsync(userId);

        for (int i = 0; i < 7; i++)
        {
            var message = new ChatMessage
            {
                SessionId = session.Id,
                SenderUserId = userId,
                Preview = $"Message {i}",
                MessageLength = 10
            };
            _context.ChatMessages.Add(message);
        }

        // Add one with EditedAt set (to be excluded from count)
        var editedMessage = new ChatMessage
        {
            SessionId = session.Id,
            SenderUserId = userId,
            Preview = "Edited",
            MessageLength = 6,
            EditedAt = DateTime.UtcNow
        };
        _context.ChatMessages.Add(editedMessage);

        await _context.SaveChangesAsync();

        // Act
        var count = await _repository.GetSessionMessageCountAsync(session.Id);

        // Assert
        Assert.That(count, Is.EqualTo(8)); // All messages including edited one
    }

    [Test]
    public async Task UpdateMessageStatusAsync_UpdatesStatus()
    {
        // Arrange
        var userId = "test-user-123";
        await SeedTestUserAsync(userId);

        var session = await CreateTestSessionAsync(userId);
        var message = new ChatMessage
        {
            SessionId = session.Id,
            SenderUserId = userId,
            Preview = "Test",
            MessageLength = 4,
            MessageStatus = ChatMessageStatus.Pending
        };
        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.UpdateMessageStatusAsync(message.Id, ChatMessageStatus.Sent);

        // Assert
        Assert.That(result.MessageStatus, Is.EqualTo(ChatMessageStatus.Sent));

        var updatedMessage = await _context.ChatMessages.FindAsync(message.Id);
        Assert.That(updatedMessage!.MessageStatus, Is.EqualTo(ChatMessageStatus.Sent));
    }

    #endregion

    #region User Operations Tests

    [Test]
    public async Task IsUserParticipantAsync_WithActiveParticipant_ReturnsTrue()
    {
        // Arrange
        var userId = "test-user-123";
        await SeedTestUserAsync(userId);

        var session = await CreateTestSessionAsync(userId);
        var participant = new ChatParticipant
        {
            SessionId = session.Id,
            UserId = userId,
            LeftAt = null
        };
        _context.ChatParticipants.Add(participant);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.IsUserParticipantAsync(session.Id, userId);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task IsUserParticipantAsync_WithLeftParticipant_ReturnsFalse()
    {
        // Arrange
        var userId = "test-user-123";
        await SeedTestUserAsync(userId);

        var session = await CreateTestSessionAsync(userId);
        var participant = new ChatParticipant
        {
            SessionId = session.Id,
            UserId = userId,
            LeftAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.ChatParticipants.Add(participant);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.IsUserParticipantAsync(session.Id, userId);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task IsUserParticipantAsync_WithNonParticipant_ReturnsFalse()
    {
        // Arrange
        var userId = "test-user-123";
        await SeedTestUserAsync(userId);

        var session = await CreateTestSessionAsync(userId);

        // Act
        var result = await _repository.IsUserParticipantAsync(session.Id, "non-existent-user");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task GetSessionParticipantUserIdsAsync_ReturnsActiveUserIds()
    {
        // Arrange
        var user1 = "user-1";
        var user2 = "user-2";
        var user3 = "user-3";

        await SeedTestUserAsync(user1);
        await SeedTestUserAsync(user2);
        await SeedTestUserAsync(user3);

        var session = await CreateTestSessionAsync(user1);

        _context.ChatParticipants.AddRange(
            new ChatParticipant { SessionId = session.Id, UserId = user1, LeftAt = null },
            new ChatParticipant { SessionId = session.Id, UserId = user2, LeftAt = null },
            new ChatParticipant { SessionId = session.Id, UserId = user3, LeftAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetSessionParticipantUserIdsAsync(session.Id);

        // Assert
        var userIds = results.ToList();
        Assert.That(userIds, Has.Count.EqualTo(2));
        Assert.That(userIds, Contains.Item(user1));
        Assert.That(userIds, Contains.Item(user2));
        Assert.That(userIds, Does.Not.Contain(user3));
    }

    #endregion

    #region Helper Methods

    private async Task SeedTestUserAsync(string userId)
    {
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = $"user_{userId}",
            Email = $"{userId}@test.com"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    private async Task<ChatSession> CreateTestSessionAsync(string userId)
    {
        var session = new ChatSession
        {
            Title = "Test Session",
            CreatedByUserId = userId,
            IsGroup = false
        };
        _context.ChatSessions.Add(session);
        await _context.SaveChangesAsync();
        return session;
    }

    #endregion
}
