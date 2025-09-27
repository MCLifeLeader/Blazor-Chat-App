namespace Blazor.Chat.App.ApiService.Models
{
    /// <summary>
    /// Response DTO for message operations that return acceptance
    /// </summary>
    public record MessageOperationResponseDto
    {
        public Guid MessageId { get; init; }
        public Guid OutboxId { get; init; }
        public string Status { get; init; } = string.Empty;
        public DateTime Timestamp { get; init; }
    }
}