namespace Blazor.Chat.App.ServiceDefaults.Configuration;

/// <summary>
/// Configuration options for Cosmos DB connection and settings
/// </summary>
public class CosmosDbOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "CosmosDb";

    /// <summary>
    /// Cosmos DB account endpoint URL
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Primary key for authentication (only for development - prefer Managed Identity in production)
    /// </summary>
    public string? PrimaryKey { get; set; }

    /// <summary>
    /// Database name for chat data
    /// </summary>
    public string DatabaseName { get; set; } = "ChatDatabase";

    /// <summary>
    /// Container name for chat items
    /// </summary>
    public string ContainerName { get; set; } = "ChatItems";

    /// <summary>
    /// Partition key path
    /// </summary>
    public string PartitionKey { get; set; } = "/sessionId";

    /// <summary>
    /// Request units for container throughput (null for autoscale)
    /// </summary>
    public int? RequestUnits { get; set; } = 400;

    /// <summary>
    /// Maximum retry attempts for Cosmos operations
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Maximum retry wait time in seconds
    /// </summary>
    public int MaxRetryWaitTimeSeconds { get; set; } = 30;
}