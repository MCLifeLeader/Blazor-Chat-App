using Blazor.Chat.App.Data.Cosmos.Configuration;
using Blazor.Chat.App.Data.Cosmos.Repositories;
using Blazor.Chat.App.Data.Sql;
using Blazor.Chat.App.Data.Sql.Repositories;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Blazor.Chat.App.ApiService.HostedServices;

/// <summary>
/// Background service that processes outbox entries to sync data with Cosmos DB
/// </summary>
public class OutboxProcessorHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly OutboxProcessorOptions _options;
    private readonly ILogger<OutboxProcessorHostedService> _logger;

    /// <summary>
    /// Initializes a new instance of the OutboxProcessorHostedService class
    /// </summary>
    /// <param name="serviceProvider">Service provider for scoped services</param>
    /// <param name="options">Configuration options</param>
    /// <param name="logger">Logger instance</param>
    public OutboxProcessorHostedService(
        IServiceProvider serviceProvider,
        IOptions<OutboxProcessorOptions> options,
        ILogger<OutboxProcessorHostedService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Main execution loop for processing outbox entries
    /// </summary>
    /// <param name="stoppingToken">Cancellation token</param>
    /// <returns>Task representing the background work</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessor started with interval {Interval}ms", _options.ProcessingIntervalMs);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxEntriesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox entries");
            }

            await Task.Delay(_options.ProcessingIntervalMs, stoppingToken);
        }

        _logger.LogInformation("OutboxProcessor stopped");
    }

    /// <summary>
    /// Process pending outbox entries
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the processing work</returns>
    private async Task ProcessOutboxEntriesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var cosmosRepository = scope.ServiceProvider.GetRequiredService<IChatCosmosRepository>();

        var pendingEntries = await outboxRepository.GetPendingEntriesAsync(_options.BatchSize, cancellationToken);
        
        if (!pendingEntries.Any())
        {
            return;
        }

        _logger.LogDebug("Processing {Count} outbox entries", pendingEntries.Count());

        var processingTasks = pendingEntries.Select(entry => 
            ProcessSingleEntryAsync(entry, outboxRepository, cosmosRepository, cancellationToken));

        await Task.WhenAll(processingTasks);
    }

    /// <summary>
    /// Process a single outbox entry
    /// </summary>
    /// <param name="entry">Outbox entry to process</param>
    /// <param name="outboxRepository">Outbox repository</param>
    /// <param name="cosmosRepository">Cosmos repository</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the processing work</returns>
    private async Task ProcessSingleEntryAsync(
        ChatOutbox entry,
        IOutboxRepository outboxRepository,
        IChatCosmosRepository cosmosRepository,
        CancellationToken cancellationToken)
    {
        try
        {
            // Mark as processing to prevent duplicate processing
            var marked = await outboxRepository.MarkAsProcessingAsync(entry.Id, cancellationToken);
            if (!marked)
            {
                _logger.LogDebug("Entry {OutboxId} already being processed", entry.Id);
                return;
            }

            // Process based on message type
            await ProcessOutboxEntryByTypeAsync(entry, cosmosRepository, cancellationToken);

            // Mark as completed
            await outboxRepository.MarkAsCompletedAsync(entry.Id, cancellationToken);
            
            _logger.LogDebug("Successfully processed outbox entry {OutboxId} of type {MessageType}", 
                entry.Id, entry.MessageType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing outbox entry {OutboxId}", entry.Id);
            
            // Increment attempts and potentially mark as dead letter
            await HandleProcessingErrorAsync(entry, outboxRepository, ex.Message, cancellationToken);
        }
    }

    /// <summary>
    /// Process outbox entry based on its message type
    /// </summary>
    /// <param name="entry">Outbox entry</param>
    /// <param name="cosmosRepository">Cosmos repository</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the processing work</returns>
    private async Task ProcessOutboxEntryByTypeAsync(
        ChatOutbox entry,
        IChatCosmosRepository cosmosRepository,
        CancellationToken cancellationToken)
    {
        switch (entry.MessageType.ToLowerInvariant())
        {
            case "message-created":
            case "message-edited":
                await ProcessMessageEventAsync(entry, cosmosRepository, cancellationToken);
                break;
                
            case "message-deleted":
                await ProcessMessageDeletionAsync(entry, cosmosRepository, cancellationToken);
                break;
                
            case "session-snapshot":
                await ProcessSessionSnapshotAsync(entry, cosmosRepository, cancellationToken);
                break;
                
            default:
                _logger.LogWarning("Unknown message type {MessageType} for outbox entry {OutboxId}", 
                    entry.MessageType, entry.Id);
                break;
        }
    }

    /// <summary>
    /// Process message creation or edit events
    /// </summary>
    /// <param name="entry">Outbox entry</param>
    /// <param name="cosmosRepository">Cosmos repository</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the processing work</returns>
    private async Task ProcessMessageEventAsync(
        ChatOutbox entry,
        IChatCosmosRepository cosmosRepository,
        CancellationToken cancellationToken)
    {
        var messageDocument = JsonSerializer.Deserialize<CosmosMessageDocument>(entry.PayloadJson);
        if (messageDocument == null)
        {
            throw new InvalidOperationException($"Failed to deserialize message payload for outbox entry {entry.Id}");
        }

        // Ensure outbox ID is set for idempotency
        messageDocument = messageDocument with { outboxId = entry.Id };

        await cosmosRepository.UpsertMessageAsync(messageDocument, cancellationToken);
    }

    /// <summary>
    /// Process message deletion events
    /// </summary>
    /// <param name="entry">Outbox entry</param>
    /// <param name="cosmosRepository">Cosmos repository</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the processing work</returns>
    private async Task ProcessMessageDeletionAsync(
        ChatOutbox entry,
        IChatCosmosRepository cosmosRepository,
        CancellationToken cancellationToken)
    {
        var deleteData = JsonSerializer.Deserialize<MessageDeleteData>(entry.PayloadJson);
        if (deleteData == null)
        {
            throw new InvalidOperationException($"Failed to deserialize deletion payload for outbox entry {entry.Id}");
        }

        await cosmosRepository.DeleteMessageAsync(deleteData.MessageId, deleteData.SessionId, cancellationToken);
    }

    /// <summary>
    /// Process session snapshot events
    /// </summary>
    /// <param name="entry">Outbox entry</param>
    /// <param name="cosmosRepository">Cosmos repository</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the processing work</returns>
    private async Task ProcessSessionSnapshotAsync(
        ChatOutbox entry,
        IChatCosmosRepository cosmosRepository,
        CancellationToken cancellationToken)
    {
        var snapshotData = JsonSerializer.Deserialize<SessionSnapshotData>(entry.PayloadJson);
        if (snapshotData == null)
        {
            throw new InvalidOperationException($"Failed to deserialize snapshot payload for outbox entry {entry.Id}");
        }

        await cosmosRepository.UpsertSessionSnapshotAsync(snapshotData, cancellationToken);
    }

    /// <summary>
    /// Handle processing errors with retry logic
    /// </summary>
    /// <param name="entry">Failed outbox entry</param>
    /// <param name="outboxRepository">Outbox repository</param>
    /// <param name="errorMessage">Error message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the error handling work</returns>
    private async Task HandleProcessingErrorAsync(
        ChatOutbox entry,
        IOutboxRepository outboxRepository,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        var newAttempts = entry.Attempts + 1;
        
        if (newAttempts >= _options.MaxRetryAttempts)
        {
            _logger.LogWarning("Moving outbox entry {OutboxId} to dead letter after {Attempts} attempts", 
                entry.Id, newAttempts);
            
            await outboxRepository.MarkAsDeadLetterAsync(entry.Id, errorMessage, cancellationToken);
        }
        else
        {
            _logger.LogDebug("Marking outbox entry {OutboxId} as failed, attempt {Attempts}", 
                entry.Id, newAttempts);
            
            // Calculate next retry time with exponential backoff
            var nextRetry = DateTime.UtcNow.AddSeconds(Math.Pow(2, newAttempts) * 30);
            await outboxRepository.MarkAsFailedAsync(entry.Id, errorMessage, nextRetry, cancellationToken);
        }
    }

    /// <summary>
    /// Data structure for message deletion payloads
    /// </summary>
    private record MessageDeleteData(Guid MessageId, Guid SessionId);

    /// <summary>
    /// Data structure for session snapshot payloads
    /// </summary>
    private record SessionSnapshotData(
        Guid SessionId, 
        int MessageCount, 
        DateTime LastUpdatedAt, 
        object SnapshotData);
}