namespace Blazor.Chat.App.ServiceDefaults.Repositories
{
    /// <summary>
    /// Session snapshot document for quick session information
    /// </summary>
    public record CosmosSessionSnapshot
    {
        public string id { get; init; } = string.Empty; // "session-{sessionId}-snapshot"
        public Guid sessionId { get; init; }
        public int messagesCount { get; init; }
        public DateTime lastUpdatedAt { get; init; }
        public DateTime lastActivityAt { get; init; }
        public List<Guid> recentParticipants { get; init; } = new();
        public CosmosMessageDocument? lastMessage { get; init; }
        public string documentType { get; init; } = "session-snapshot";
    }
}