namespace Blazor.Chat.App.ApiService.Models.Settings
{
    public class Cosmosdb
    {
        /// <summary>
        /// Configuration section name.
        /// </summary>
        public string SectionName = "CosmosDb";

        /// <summary>
        /// Cosmos DB endpoint URI (e.g. https://{account}.documents.azure.com:443/).
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// Primary key for the Cosmos account. When empty, code will attempt managed identity.
        /// </summary>
        public string PrimaryKey { get; set; } = string.Empty;

        /// <summary>
        /// Database name to use/create.
        /// </summary>
        public string DatabaseName { get; set; } = string.Empty;

        /// <summary>
        /// Container (collection) name to use/create.
        /// </summary>
        public string ContainerName { get; set; } = string.Empty;

        /// <summary>
        /// Partition key path (e.g. "/sessionId").
        /// </summary>
        public string PartitionKey { get; set; } = string.Empty;

        /// <summary>
        /// RU/s to provision when creating database/containers (used in initialization).
        /// </summary>
        public int RequestUnits { get; set; } = 400;

        /// <summary>
        /// Maximum number of retry attempts when rate limited.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 9;

        /// <summary>
        /// Maximum wait time (seconds) when retrying on rate limit.
        /// </summary>
        public int MaxRetryWaitTimeSeconds { get; set; } = 30;
    }
}