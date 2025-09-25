using Blazor.Chat.App.Data.Db;
using Blazor.Chat.App.ServiceDefaults.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Blazor.Chat.App.Data.Repositories
{
    /// <summary>
    /// SQL Server implementation of outbox repository for managing outbox pattern
    /// </summary>
    public class OutboxRepository : IOutboxRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OutboxRepository> _logger;

        public OutboxRepository(ApplicationDbContext context, ILogger<OutboxRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<ChatOutbox>> GetPendingEntriesAsync(
            int maxCount = 100, 
            CancellationToken cancellationToken = default)
        {
            return await _context.ChatOutbox
                .Where(o => o.Status == ChatOutboxStatus.Pending)
                .OrderBy(o => o.CreatedAt)
                .Take(maxCount)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> MarkAsProcessingAsync(
            Guid outboxId, 
            CancellationToken cancellationToken = default)
        {
            var entry = await _context.ChatOutbox.FindAsync(new object[] { outboxId }, cancellationToken);
            if (entry is null || entry.Status != ChatOutboxStatus.Pending)
            {
                _logger.LogWarning("Cannot mark outbox entry {OutboxId} as processing - not found or not pending", outboxId);
                return false;
            }

            entry.Status = ChatOutboxStatus.Processing;
            entry.Attempts++;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Marked outbox entry {OutboxId} as processing (attempt {Attempts})", outboxId, entry.Attempts);
            return true;
        }

        public async Task MarkAsCompletedAsync(
            Guid outboxId, 
            CancellationToken cancellationToken = default)
        {
            var entry = await _context.ChatOutbox.FindAsync(new object[] { outboxId }, cancellationToken);
            if (entry is not null)
            {
                entry.Status = ChatOutboxStatus.Completed;
                entry.ProcessedAt = DateTime.UtcNow;
                entry.LastError = null;
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Marked outbox entry {OutboxId} as completed", outboxId);
            }
        }

        public async Task MarkAsFailedAsync(
            Guid outboxId, 
            string errorMessage, 
            DateTime? nextRetryAt = null, 
            CancellationToken cancellationToken = default)
        {
            var entry = await _context.ChatOutbox.FindAsync(new object[] { outboxId }, cancellationToken);
            if (entry is not null)
            {
                entry.Status = ChatOutboxStatus.Pending; // Back to pending for retry
                entry.LastError = errorMessage.Length > 1000 ? errorMessage[..1000] : errorMessage;
                entry.NextRetryAt = nextRetryAt;
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogWarning("Marked outbox entry {OutboxId} as failed: {Error}. Next retry: {NextRetry}", 
                    outboxId, errorMessage, nextRetryAt);
            }
        }

        public async Task MarkAsDeadLetterAsync(
            Guid outboxId, 
            string finalError, 
            CancellationToken cancellationToken = default)
        {
            var entry = await _context.ChatOutbox.FindAsync(new object[] { outboxId }, cancellationToken);
            if (entry is not null)
            {
                entry.Status = ChatOutboxStatus.DeadLetter;
                entry.LastError = finalError.Length > 1000 ? finalError[..1000] : finalError;
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogError("Marked outbox entry {OutboxId} as dead letter after {Attempts} attempts: {Error}", 
                    outboxId, entry.Attempts, finalError);
            }
        }

        public async Task<IEnumerable<ChatOutbox>> GetEntriesReadyForRetryAsync(
            int maxCount = 100, 
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await _context.ChatOutbox
                .Where(o => o.Status == ChatOutboxStatus.Pending && 
                           (o.NextRetryAt == null || o.NextRetryAt <= now))
                .OrderBy(o => o.CreatedAt)
                .Take(maxCount)
                .ToListAsync(cancellationToken);
        }

        public async Task<OutboxStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var stats = await _context.ChatOutbox
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var oldestPending = await _context.ChatOutbox
                .Where(o => o.Status == ChatOutboxStatus.Pending)
                .OrderBy(o => o.CreatedAt)
                .Select(o => (DateTime?)o.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            return new OutboxStatistics
            {
                PendingCount = stats.FirstOrDefault(s => s.Status == ChatOutboxStatus.Pending)?.Count ?? 0,
                ProcessingCount = stats.FirstOrDefault(s => s.Status == ChatOutboxStatus.Processing)?.Count ?? 0,
                CompletedCount = stats.FirstOrDefault(s => s.Status == ChatOutboxStatus.Completed)?.Count ?? 0,
                DeadLetterCount = stats.FirstOrDefault(s => s.Status == ChatOutboxStatus.DeadLetter)?.Count ?? 0,
                OldestPendingAt = oldestPending
            };
        }
    }
}