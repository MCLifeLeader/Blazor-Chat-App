using Blazor.Chat.App.Data.Sql;
using Blazor.Chat.App.Data.Sql.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace Blazor.Chat.App.Data.Tests.Repositories;

/// <summary>
/// Unit tests for OutboxRepository using EF Core InMemory database.
/// Tests outbox pattern operations for reliable message delivery to Cosmos DB.
/// </summary>
[TestFixture]
public class OutboxRepositoryTests
{
    private ApplicationDbContext _context = null!;
    private OutboxRepository _repository = null!;
    private ILogger<OutboxRepository> _logger = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);
        _logger = Substitute.For<ILogger<OutboxRepository>>();
        _repository = new OutboxRepository(_context, _logger);
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
        var repository = new OutboxRepository(_context, _logger);

        Assert.That(repository, Is.Not.Null);
        Assert.That(repository, Is.InstanceOf<IOutboxRepository>());
    }

    [Test]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new OutboxRepository(null!, _logger));
    }

    [Test]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new OutboxRepository(_context, null!));
    }

    #endregion

    #region CreateOutboxEntry Tests

    [Test]
    public async Task CreateOutboxEntryAsync_WithValidEntry_CreatesEntry()
    {
        // Arrange
        var outboxEntry = new ChatOutbox
        {
            MessageType = "message-created",
            PayloadJson = "{\"content\": \"test\"}",
            SessionId = Guid.NewGuid(),
            MessageId = Guid.NewGuid()
        };

        // Act
        var result = await _repository.CreateOutboxEntryAsync(outboxEntry);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(result.MessageType, Is.EqualTo("message-created"));
        Assert.That(result.Status, Is.EqualTo(ChatOutboxStatus.Pending));

        var savedEntry = await _context.ChatOutbox.FindAsync(result.Id);
        Assert.That(savedEntry, Is.Not.Null);
        Assert.That(savedEntry!.PayloadJson, Is.EqualTo("{\"content\": \"test\"}"));
    }

    #endregion

    #region GetPendingEntries Tests

    [Test]
    public async Task GetPendingEntriesAsync_WithPendingEntries_ReturnsEntries()
    {
        // Arrange
        var pending1 = new ChatOutbox
        {
            MessageType = "message-1",
            PayloadJson = "{}",
            Status = ChatOutboxStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        var pending2 = new ChatOutbox
        {
            MessageType = "message-2",
            PayloadJson = "{}",
            Status = ChatOutboxStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        var completed = new ChatOutbox
        {
            MessageType = "message-3",
            PayloadJson = "{}",
            Status = ChatOutboxStatus.Completed
        };

        _context.ChatOutbox.AddRange(pending1, pending2, completed);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetPendingEntriesAsync();

        // Assert
        var entryList = results.ToList();
        Assert.That(entryList, Has.Count.EqualTo(2));
        Assert.That(entryList[0].MessageType, Is.EqualTo("message-1")); // Oldest first
        Assert.That(entryList[1].MessageType, Is.EqualTo("message-2"));
    }

    [Test]
    public async Task GetPendingEntriesAsync_WithMaxCount_ReturnsLimitedResults()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            var entry = new ChatOutbox
            {
                MessageType = $"message-{i}",
                PayloadJson = "{}",
                Status = ChatOutboxStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            };
            _context.ChatOutbox.Add(entry);
        }
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetPendingEntriesAsync(maxCount: 5);

        // Assert
        var entryList = results.ToList();
        Assert.That(entryList, Has.Count.EqualTo(5));
    }

    [Test]
    public async Task GetPendingEntriesAsync_WithNoPendingEntries_ReturnsEmpty()
    {
        // Arrange - Add only completed entries
        var completed = new ChatOutbox
        {
            MessageType = "message-1",
            PayloadJson = "{}",
            Status = ChatOutboxStatus.Completed
        };
        _context.ChatOutbox.Add(completed);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetPendingEntriesAsync();

        // Assert
        Assert.That(results, Is.Empty);
    }

    #endregion

    #region MarkAsProcessing Tests

    [Test]
    public async Task MarkAsProcessingAsync_WithPendingEntry_MarksAsProcessingAndIncrementsAttempts()
    {
        // Arrange
        var entry = new ChatOutbox
        {
            MessageType = "test",
            PayloadJson = "{}",
            Status = ChatOutboxStatus.Pending,
            Attempts = 0
        };
        _context.ChatOutbox.Add(entry);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.MarkAsProcessingAsync(entry.Id);

        // Assert
        Assert.That(result, Is.True);

        var updatedEntry = await _context.ChatOutbox.FindAsync(entry.Id);
        Assert.That(updatedEntry, Is.Not.Null);
        Assert.That(updatedEntry!.Status, Is.EqualTo(ChatOutboxStatus.Processing));
        Assert.That(updatedEntry.Attempts, Is.EqualTo(1));
    }

    [Test]
    public async Task MarkAsProcessingAsync_WithNonExistentEntry_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.MarkAsProcessingAsync(nonExistentId);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task MarkAsProcessingAsync_WithNonPendingEntry_ReturnsFalse()
    {
        // Arrange
        var entry = new ChatOutbox
        {
            MessageType = "test",
            PayloadJson = "{}",
            Status = ChatOutboxStatus.Completed
        };
        _context.ChatOutbox.Add(entry);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.MarkAsProcessingAsync(entry.Id);

        // Assert
        Assert.That(result, Is.False);

        var updatedEntry = await _context.ChatOutbox.FindAsync(entry.Id);
        Assert.That(updatedEntry!.Status, Is.EqualTo(ChatOutboxStatus.Completed)); // Unchanged
    }

    #endregion

    #region MarkAsCompleted Tests

    [Test]
    public async Task MarkAsCompletedAsync_WithExistingEntry_MarksAsCompleted()
    {
        // Arrange
        var entry = new ChatOutbox
        {
            MessageType = "test",
            PayloadJson = "{}",
            Status = ChatOutboxStatus.Processing,
            LastError = "Previous error"
        };
        _context.ChatOutbox.Add(entry);
        await _context.SaveChangesAsync();

        // Act
        await _repository.MarkAsCompletedAsync(entry.Id);

        // Assert
        var updatedEntry = await _context.ChatOutbox.FindAsync(entry.Id);
        Assert.That(updatedEntry, Is.Not.Null);
        Assert.That(updatedEntry!.Status, Is.EqualTo(ChatOutboxStatus.Completed));
        Assert.That(updatedEntry.ProcessedAt, Is.Not.Null);
        Assert.That(updatedEntry.LastError, Is.Null);
        Assert.That(updatedEntry.ProcessedAt!.Value, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public async Task MarkAsCompletedAsync_WithNonExistentEntry_DoesNotThrow()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert - Should not throw
        Assert.DoesNotThrowAsync(async () => await _repository.MarkAsCompletedAsync(nonExistentId));
    }

    #endregion

    #region MarkAsFailed Tests

    [Test]
    public async Task MarkAsFailedAsync_WithExistingEntry_MarksAsFailedAndSetsError()
    {
        // Arrange
        var entry = new ChatOutbox
        {
            MessageType = "test",
            PayloadJson = "{}",
            Status = ChatOutboxStatus.Processing
        };
        _context.ChatOutbox.Add(entry);
        await _context.SaveChangesAsync();

        var errorMessage = "Connection timeout";
        var nextRetryAt = DateTime.UtcNow.AddMinutes(5);

        // Act
        await _repository.MarkAsFailedAsync(entry.Id, errorMessage, nextRetryAt);

        // Assert
        var updatedEntry = await _context.ChatOutbox.FindAsync(entry.Id);
        Assert.That(updatedEntry, Is.Not.Null);
        Assert.That(updatedEntry!.Status, Is.EqualTo(ChatOutboxStatus.Pending)); // Back to pending for retry
        Assert.That(updatedEntry.LastError, Is.EqualTo(errorMessage));
        Assert.That(updatedEntry.NextRetryAt, Is.EqualTo(nextRetryAt));
    }

    [Test]
    public async Task MarkAsFailedAsync_WithLongErrorMessage_TruncatesTo1000Characters()
    {
        // Arrange
        var entry = new ChatOutbox
        {
            MessageType = "test",
            PayloadJson = "{}",
            Status = ChatOutboxStatus.Processing
        };
        _context.ChatOutbox.Add(entry);
        await _context.SaveChangesAsync();

        var longError = new string('X', 1500);

        // Act
        await _repository.MarkAsFailedAsync(entry.Id, longError);

        // Assert
        var updatedEntry = await _context.ChatOutbox.FindAsync(entry.Id);
        Assert.That(updatedEntry!.LastError, Has.Length.EqualTo(1000));
    }

    [Test]
    public async Task MarkAsFailedAsync_WithNonExistentEntry_DoesNotThrow()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert - Should not throw
        Assert.DoesNotThrowAsync(async () => 
            await _repository.MarkAsFailedAsync(nonExistentId, "Error"));
    }

    #endregion

    #region MarkAsDeadLetter Tests

    [Test]
    public async Task MarkAsDeadLetterAsync_WithExistingEntry_MarksAsDeadLetter()
    {
        // Arrange
        var entry = new ChatOutbox
        {
            MessageType = "test",
            PayloadJson = "{}",
            Status = ChatOutboxStatus.Processing,
            Attempts = 5
        };
        _context.ChatOutbox.Add(entry);
        await _context.SaveChangesAsync();

        var finalError = "Max retries exceeded";

        // Act
        await _repository.MarkAsDeadLetterAsync(entry.Id, finalError);

        // Assert
        var updatedEntry = await _context.ChatOutbox.FindAsync(entry.Id);
        Assert.That(updatedEntry, Is.Not.Null);
        Assert.That(updatedEntry!.Status, Is.EqualTo(ChatOutboxStatus.DeadLetter));
        Assert.That(updatedEntry.LastError, Is.EqualTo(finalError));
        Assert.That(updatedEntry.Attempts, Is.EqualTo(5)); // Unchanged
    }

    [Test]
    public async Task MarkAsDeadLetterAsync_WithLongErrorMessage_TruncatesTo1000Characters()
    {
        // Arrange
        var entry = new ChatOutbox
        {
            MessageType = "test",
            PayloadJson = "{}",
            Status = ChatOutboxStatus.Processing
        };
        _context.ChatOutbox.Add(entry);
        await _context.SaveChangesAsync();

        var longError = new string('Y', 1500);

        // Act
        await _repository.MarkAsDeadLetterAsync(entry.Id, longError);

        // Assert
        var updatedEntry = await _context.ChatOutbox.FindAsync(entry.Id);
        Assert.That(updatedEntry!.LastError, Has.Length.EqualTo(1000));
    }

    #endregion

    #region GetEntriesReadyForRetry Tests

    [Test]
    public async Task GetEntriesReadyForRetryAsync_WithReadyEntries_ReturnsEntries()
    {
        // Arrange
        var readyNow = new ChatOutbox
        {
            MessageType = "ready-now",
            PayloadJson = "{}",
            Status = ChatOutboxStatus.Pending,
            NextRetryAt = DateTime.UtcNow.AddMinutes(-1),
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        var readyNoRetryTime = new ChatOutbox
        {
            MessageType = "ready-no-retry",
            PayloadJson = "{}",
            Status = ChatOutboxStatus.Pending,
            NextRetryAt = null,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        var notReadyYet = new ChatOutbox
        {
            MessageType = "not-ready",
            PayloadJson = "{}",
            Status = ChatOutboxStatus.Pending,
            NextRetryAt = DateTime.UtcNow.AddMinutes(10)
        };

        _context.ChatOutbox.AddRange(readyNow, readyNoRetryTime, notReadyYet);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetEntriesReadyForRetryAsync();

        // Assert
        var entryList = results.ToList();
        Assert.That(entryList, Has.Count.EqualTo(2));
        Assert.That(entryList.Any(e => e.MessageType == "ready-now"), Is.True);
        Assert.That(entryList.Any(e => e.MessageType == "ready-no-retry"), Is.True);
        Assert.That(entryList.Any(e => e.MessageType == "not-ready"), Is.False);
    }

    [Test]
    public async Task GetEntriesReadyForRetryAsync_WithMaxCount_ReturnsLimitedResults()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            var entry = new ChatOutbox
            {
                MessageType = $"message-{i}",
                PayloadJson = "{}",
                Status = ChatOutboxStatus.Pending,
                NextRetryAt = null,
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            };
            _context.ChatOutbox.Add(entry);
        }
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetEntriesReadyForRetryAsync(maxCount: 3);

        // Assert
        var entryList = results.ToList();
        Assert.That(entryList, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task GetEntriesReadyForRetryAsync_OnlyReturnsPendingStatus()
    {
        // Arrange
        var pending = new ChatOutbox
        {
            MessageType = "pending",
            PayloadJson = "{}",
            Status = ChatOutboxStatus.Pending
        };

        var processing = new ChatOutbox
        {
            MessageType = "processing",
            PayloadJson = "{}",
            Status = ChatOutboxStatus.Processing
        };

        _context.ChatOutbox.AddRange(pending, processing);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetEntriesReadyForRetryAsync();

        // Assert
        var entryList = results.ToList();
        Assert.That(entryList, Has.Count.EqualTo(1));
        Assert.That(entryList[0].MessageType, Is.EqualTo("pending"));
    }

    #endregion

    #region GetStatistics Tests

    [Test]
    public async Task GetStatisticsAsync_WithVariousEntries_ReturnsCorrectCounts()
    {
        // Arrange
        var entries = new[]
        {
            new ChatOutbox { MessageType = "1", PayloadJson = "{}", Status = ChatOutboxStatus.Pending, CreatedAt = DateTime.UtcNow.AddMinutes(-30) },
            new ChatOutbox { MessageType = "2", PayloadJson = "{}", Status = ChatOutboxStatus.Pending, CreatedAt = DateTime.UtcNow.AddMinutes(-20) },
            new ChatOutbox { MessageType = "3", PayloadJson = "{}", Status = ChatOutboxStatus.Processing },
            new ChatOutbox { MessageType = "4", PayloadJson = "{}", Status = ChatOutboxStatus.Completed },
            new ChatOutbox { MessageType = "5", PayloadJson = "{}", Status = ChatOutboxStatus.Completed },
            new ChatOutbox { MessageType = "6", PayloadJson = "{}", Status = ChatOutboxStatus.Completed },
            new ChatOutbox { MessageType = "7", PayloadJson = "{}", Status = ChatOutboxStatus.DeadLetter }
        };

        _context.ChatOutbox.AddRange(entries);
        await _context.SaveChangesAsync();

        // Act
        var stats = await _repository.GetStatisticsAsync();

        // Assert
        Assert.That(stats, Is.Not.Null);
        Assert.That(stats.PendingCount, Is.EqualTo(2));
        Assert.That(stats.ProcessingCount, Is.EqualTo(1));
        Assert.That(stats.CompletedCount, Is.EqualTo(3));
        Assert.That(stats.DeadLetterCount, Is.EqualTo(1));
        Assert.That(stats.OldestPendingAt, Is.Not.Null);
        Assert.That(stats.OldestPendingAt!.Value, Is.EqualTo(DateTime.UtcNow.AddMinutes(-30)).Within(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public async Task GetStatisticsAsync_WithNoEntries_ReturnsZeroCounts()
    {
        // Act
        var stats = await _repository.GetStatisticsAsync();

        // Assert
        Assert.That(stats, Is.Not.Null);
        Assert.That(stats.PendingCount, Is.EqualTo(0));
        Assert.That(stats.ProcessingCount, Is.EqualTo(0));
        Assert.That(stats.CompletedCount, Is.EqualTo(0));
        Assert.That(stats.DeadLetterCount, Is.EqualTo(0));
        Assert.That(stats.OldestPendingAt, Is.Null);
    }

    [Test]
    public async Task GetStatisticsAsync_WithOnlyNonPending_OldestPendingIsNull()
    {
        // Arrange
        var entries = new[]
        {
            new ChatOutbox { MessageType = "1", PayloadJson = "{}", Status = ChatOutboxStatus.Completed },
            new ChatOutbox { MessageType = "2", PayloadJson = "{}", Status = ChatOutboxStatus.DeadLetter }
        };

        _context.ChatOutbox.AddRange(entries);
        await _context.SaveChangesAsync();

        // Act
        var stats = await _repository.GetStatisticsAsync();

        // Assert
        Assert.That(stats.OldestPendingAt, Is.Null);
        Assert.That(stats.CompletedCount, Is.EqualTo(1));
        Assert.That(stats.DeadLetterCount, Is.EqualTo(1));
    }

    #endregion
}
