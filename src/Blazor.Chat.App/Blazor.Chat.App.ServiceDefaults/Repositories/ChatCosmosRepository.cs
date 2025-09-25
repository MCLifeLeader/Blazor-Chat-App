using Azure;
using Blazor.Chat.App.ServiceDefaults.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace Blazor.Chat.App.ServiceDefaults.Repositories
{
    /// <summary>
    /// Cosmos DB implementation of chat repository for fast message reads and storage
    /// </summary>
    public class ChatCosmosRepository : IChatCosmosRepository
    {
        private readonly CosmosClient _cosmosClient;
        private readonly Container _container;
        private readonly ILogger<ChatCosmosRepository> _logger;
        private readonly CosmosDbOptions _options;

        public ChatCosmosRepository(
            CosmosClient cosmosClient,
            IOptions<CosmosDbOptions> options,
            ILogger<ChatCosmosRepository> logger)
        {
            _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var database = _cosmosClient.GetDatabase(_options.DatabaseName);
            _container = database.GetContainer(_options.ContainerName);
        }

        public async Task UpsertMessageAsync(
            CosmosMessageDocument message,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _container.UpsertItemAsync(
                    message,
                    new PartitionKey(message.sessionId.ToString()),
                    cancellationToken: cancellationToken);

                _logger.LogDebug("Upserted message {MessageId} to Cosmos DB with RU cost {RU}",
                    message.id, response.RequestCharge);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                // This is expected for idempotent upserts - document already exists with same content
                _logger.LogDebug("Message {MessageId} already exists in Cosmos DB (idempotent upsert)", message.id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upsert message {MessageId} to Cosmos DB", message.id);
                throw;
            }
        }

        public async Task<CosmosMessagePage> GetSessionMessagesAsync(
            Guid sessionId,
            int page = 1,
            int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var query = new QueryDefinition(
                    "SELECT * FROM c WHERE c.sessionId = @sessionId AND c.documentType = 'message' AND c.isDeleted != true ORDER BY c.sentAt DESC OFFSET @offset LIMIT @limit")
                    .WithParameter("@sessionId", sessionId)
                    .WithParameter("@offset", (page - 1) * pageSize)
                    .WithParameter("@limit", pageSize);

                var iterator = _container.GetItemQueryIterator<CosmosMessageDocument>(
                    query,
                    requestOptions: new QueryRequestOptions
                    {
                        PartitionKey = new PartitionKey(sessionId.ToString()),
                        MaxItemCount = pageSize
                    });

                var messages = new List<CosmosMessageDocument>();
                double totalRU = 0;

                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync(cancellationToken);
                    messages.AddRange(response.Resource);
                    totalRU += response.RequestCharge;
                }

                // Check if there are more results by querying one more item
                var hasMoreQuery = new QueryDefinition(
                    "SELECT VALUE COUNT(1) FROM c WHERE c.sessionId = @sessionId AND c.documentType = 'message' AND c.isDeleted != true")
                    .WithParameter("@sessionId", sessionId);

                var countIterator = _container.GetItemQueryIterator<int>(
                    hasMoreQuery,
                    requestOptions: new QueryRequestOptions
                    {
                        PartitionKey = new PartitionKey(sessionId.ToString())
                    });

                var totalCount = 0;
                if (countIterator.HasMoreResults)
                {
                    var countResponse = await countIterator.ReadNextAsync(cancellationToken);
                    totalCount = countResponse.Resource.FirstOrDefault();
                    totalRU += countResponse.RequestCharge;
                }

                _logger.LogDebug("Retrieved {Count} messages for session {SessionId} with RU cost {RU}",
                    messages.Count, sessionId, totalRU);

                return new CosmosMessagePage
                {
                    Messages = messages,
                    Count = messages.Count,
                    HasMoreResults = (page * pageSize) < totalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve messages for session {SessionId}", sessionId);
                throw;
            }
        }

        public async Task<CosmosMessageDocument?> GetMessageByIdAsync(
            Guid sessionId,
            Guid messageId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _container.ReadItemAsync<CosmosMessageDocument>(
                    messageId.ToString(),
                    new PartitionKey(sessionId.ToString()),
                    cancellationToken: cancellationToken);

                _logger.LogDebug("Retrieved message {MessageId} with RU cost {RU}",
                    messageId, response.RequestCharge);

                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogDebug("Message {MessageId} not found in session {SessionId}", messageId, sessionId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve message {MessageId} from session {SessionId}",
                    messageId, sessionId);
                throw;
            }
        }

        public async Task UpsertEditedMessageAsync(
            CosmosMessageDocument editedMessage,
            CancellationToken cancellationToken = default)
        {
            await UpsertMessageAsync(editedMessage, cancellationToken);
        }

        public async Task MarkMessageAsDeletedAsync(
            Guid sessionId,
            Guid messageId,
            Guid outboxId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Soft delete by setting isDeleted flag
                var existingMessage = await GetMessageByIdAsync(sessionId, messageId, cancellationToken);
                if (existingMessage is not null)
                {
                    var deletedMessage = existingMessage with 
                    { 
                        isDeleted = true,
                        outboxId = outboxId,
                        metadata = existingMessage.metadata with 
                        { 
                            editedAt = DateTime.UtcNow 
                        }
                    };

                    await UpsertMessageAsync(deletedMessage, cancellationToken);
                    _logger.LogInformation("Marked message {MessageId} as deleted", messageId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark message {MessageId} as deleted", messageId);
                throw;
            }
        }

        public async Task UpsertSessionSnapshotAsync(
            CosmosSessionSnapshot snapshot,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _container.UpsertItemAsync(
                    snapshot,
                    new PartitionKey(snapshot.sessionId.ToString()),
                    cancellationToken: cancellationToken);

                _logger.LogDebug("Upserted session snapshot {SessionId} with RU cost {RU}",
                    snapshot.sessionId, response.RequestCharge);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upsert session snapshot {SessionId}", snapshot.sessionId);
                throw;
            }
        }

        public async Task<CosmosSessionSnapshot?> GetSessionSnapshotAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var snapshotId = $"session-{sessionId}-snapshot";
                var response = await _container.ReadItemAsync<CosmosSessionSnapshot>(
                    snapshotId,
                    new PartitionKey(sessionId.ToString()),
                    cancellationToken: cancellationToken);

                _logger.LogDebug("Retrieved session snapshot {SessionId} with RU cost {RU}",
                    sessionId, response.RequestCharge);

                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogDebug("Session snapshot not found for session {SessionId}", sessionId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve session snapshot {SessionId}", sessionId);
                throw;
            }
        }

        public async Task BulkUpsertMessagesAsync(
            IEnumerable<CosmosMessageDocument> messages,
            CancellationToken cancellationToken = default)
        {
            if (!messages.Any())
                return;

            try
            {
                var tasks = messages.Select(async message =>
                {
                    try
                    {
                        await _container.UpsertItemAsync(
                            message,
                            new PartitionKey(message.sessionId.ToString()),
                            cancellationToken: cancellationToken);
                        return (Success: true, MessageId: message.id, Error: (string?)null);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to upsert message {MessageId} in bulk operation", message.id);
                        return (Success: false, MessageId: message.id, Error: ex.Message);
                    }
                });

                var results = await Task.WhenAll(tasks);
                var successful = results.Count(r => r.Success);
                var failed = results.Count(r => !r.Success);

                _logger.LogInformation("Bulk upsert completed: {Successful} successful, {Failed} failed",
                    successful, failed);

                if (failed > 0)
                {
                    var failedMessages = results.Where(r => !r.Success);
                    foreach (var failure in failedMessages)
                    {
                        _logger.LogWarning("Failed to upsert message {MessageId}: {Error}",
                            failure.MessageId, failure.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk upsert operation failed");
                throw;
            }
        }
    }
}