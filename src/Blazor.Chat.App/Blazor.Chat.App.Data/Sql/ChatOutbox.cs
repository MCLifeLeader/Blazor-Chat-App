using System.ComponentModel.DataAnnotations;

namespace Blazor.Chat.App.Data.Sql;

/// <summary>
/// Outbox pattern implementation for reliable message delivery to Cosmos DB
/// </summary>
public class ChatOutbox
{
    /// <summary>
    /// Unique identifier for this outbox entry (also used as outboxId in Cosmos for idempotency)
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Type of message/operation (e.g., "message-created", "message-edited", "message-deleted")
    /// </summary>
    [Required]
    [StringLength(50)]
    public string MessageType { get; set; } = string.Empty;

    /// <summary>
    /// JSON payload containing the data to be sent to Cosmos DB
    /// </summary>
    [Required]
    public string PayloadJson { get; set; } = string.Empty;

    /// <summary>
    /// When this outbox entry was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this entry was successfully processed (null if still pending)
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Number of processing attempts made
    /// </summary>
    public int Attempts { get; set; } = 0;

    /// <summary>
    /// Last error message if processing failed
    /// </summary>
    [StringLength(1000)]
    public string? LastError { get; set; }

    /// <summary>
    /// Current status of this outbox entry
    /// </summary>
    public ChatOutboxStatus Status { get; set; } = ChatOutboxStatus.Pending;

    /// <summary>
    /// Next time this entry should be retried (for exponential backoff)
    /// </summary>
    public DateTime? NextRetryAt { get; set; }

    /// <summary>
    /// Session ID for partitioning/routing (extracted from payload for indexing)
    /// </summary>
    public Guid? SessionId { get; set; }

    /// <summary>
    /// Message ID for reference (extracted from payload for indexing)
    /// </summary>
    public Guid? MessageId { get; set; }

    /// <summary>
    /// Navigation property to related chat messages (if applicable)
    /// </summary>
    public virtual ICollection<ChatMessage> RelatedMessages { get; set; } = new List<ChatMessage>();
}