namespace Blazor.Chat.App.ServiceDefaults.Repositories
{
    /// <summary>
    /// Cosmos DB message document structure
    /// </summary>
    public record CosmosMessageDocument
    {
        public string id { get; init; } = string.Empty; // Cosmos DB id (messageId)
        public Guid sessionId { get; init; }
        public Guid senderUserId { get; init; }
        public string senderDisplayName { get; init; } = string.Empty;
        public DateTime sentAt { get; init; }
        public MessageBody body { get; init; } = new();
        public List<MessageAttachment> attachments { get; init; } = new();
        public Guid outboxId { get; init; }
        public MessageMetadata metadata { get; init; } = new();
        public bool isDeleted { get; init; }
        public string documentType { get; init; } = "message"; // For document type discrimination
    }
}