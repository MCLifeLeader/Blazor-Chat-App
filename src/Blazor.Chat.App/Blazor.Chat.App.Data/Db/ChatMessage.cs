using System.ComponentModel.DataAnnotations;

namespace Blazor.Chat.App.Data.Db
{
    /// <summary>
    /// Represents a message in a chat session - stores metadata, while full content goes to Cosmos DB
    /// </summary>
    public class ChatMessage
    {
        /// <summary>
        /// Unique identifier for this message
        /// </summary>
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The session this message belongs to
        /// </summary>
        [Required]
        public Guid SessionId { get; set; }

        /// <summary>
        /// The user who sent this message
        /// </summary>
        [Required]
        public string SenderUserId { get; set; } = string.Empty;

        /// <summary>
        /// When the message was sent
        /// </summary>
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Preview/excerpt of the message content for quick display
        /// </summary>
        [StringLength(500)]
        public string Preview { get; set; } = string.Empty;

        /// <summary>
        /// Length of the full message content (stored in Cosmos DB)
        /// </summary>
        public int MessageLength { get; set; }

        /// <summary>
        /// Current status of the message
        /// </summary>
        public ChatMessageStatus MessageStatus { get; set; } = ChatMessageStatus.Pending;

        /// <summary>
        /// Link to the outbox entry for this message (for idempotency)
        /// </summary>
        [Required]
        public Guid OutboxId { get; set; }

        /// <summary>
        /// If this message is a reply, the ID of the message being replied to
        /// </summary>
        public Guid? ReplyToMessageId { get; set; }

        /// <summary>
        /// If this message has been edited, when the last edit occurred
        /// </summary>
        public DateTime? EditedAt { get; set; }

        /// <summary>
        /// Version number for message edits (starts at 1)
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// Navigation property to the chat session
        /// </summary>
        public virtual ChatSession Session { get; set; } = null!;

        /// <summary>
        /// Navigation property to the sender
        /// </summary>
        public virtual ApplicationUser SenderUser { get; set; } = null!;

        /// <summary>
        /// Navigation property to the outbox entry
        /// </summary>
        public virtual ChatOutbox OutboxEntry { get; set; } = null!;

        /// <summary>
        /// Navigation property to the message being replied to
        /// </summary>
        public virtual ChatMessage? ReplyToMessage { get; set; }

        /// <summary>
        /// Navigation property to replies to this message
        /// </summary>
        public virtual ICollection<ChatMessage> Replies { get; set; } = new List<ChatMessage>();
    }

    /// <summary>
    /// Enumeration of possible message statuses
    /// </summary>
    public enum ChatMessageStatus
    {
        Pending = 0,      // Waiting to be processed by outbox
        Sent = 1,         // Successfully sent to Cosmos DB
        Failed = 2,       // Failed to send after retries
        Edited = 3,       // Message has been edited
        Deleted = 4       // Message has been deleted
    }
}