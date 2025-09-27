namespace Blazor.Chat.App.ServiceDefaults.Repositories
{
    /// <summary>
    /// Edit history entry
    /// </summary>
    public record EditHistory
    {
        public DateTime editedAt { get; init; }
        public string previousContent { get; init; } = string.Empty;
        public Guid editedByUserId { get; init; }
    }
}