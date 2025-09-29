namespace Blazor.Chat.App.ApiService.Models;

/// <summary>
/// Response DTO for message operations that return acceptance
/// </summary>
public record MessageOperationResponseDto
{
    /// <summary>
    /// Unique identifier of the message
    /// </summary>
    public Guid MessageId { get; init; }

    /// <summary>
    /// Unique identifier of the outbox entry for eventual consistency
    /// </summary>
    public Guid OutboxId { get; init; }

    /// <summary>
    /// Status of the operation (e.g., "Accepted", "Failed")
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp when the operation was processed
    /// </summary>
    public DateTime Timestamp { get; init; }
}