namespace Blazor.Chat.App.ServiceDefaults.Repositories
{
    /// <summary>
    /// Message attachment structure
    /// </summary>
    public record MessageAttachment
    {
        public string fileName { get; init; } = string.Empty;
        public string contentType { get; init; } = string.Empty;
        public long size { get; init; }
        public string url { get; init; } = string.Empty;
    }
}