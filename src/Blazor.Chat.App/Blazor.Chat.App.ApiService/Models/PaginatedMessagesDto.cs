namespace Blazor.Chat.App.ApiService.Models
{
    /// <summary>
    /// DTO for paginated message results
    /// </summary>
    public record PaginatedMessagesDto
    {
        /// <summary>
        /// List of messages for the current page
        /// </summary>
        public List<ChatMessageDto> Messages { get; init; } = new();

        /// <summary>
        /// Current page number (1-based)
        /// </summary>
        public int Page { get; init; }

        /// <summary>
        /// Number of messages per page
        /// </summary>
        public int PageSize { get; init; }

        /// <summary>
        /// Total number of messages in the session
        /// </summary>
        public int TotalCount { get; init; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages { get; init; }

        /// <summary>
        /// Whether there are more pages after this one
        /// </summary>
        public bool HasNextPage { get; init; }

        /// <summary>
        /// Whether there are pages before this one
        /// </summary>
        public bool HasPreviousPage { get; init; }
    }
}