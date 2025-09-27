using System.ComponentModel.DataAnnotations;

namespace Blazor.Chat.App.Data.Db;

/// <summary>
/// Represents a user's participation in a chat session
/// </summary>
public class ChatParticipant
{
    /// <summary>
    /// Unique identifier for this participant record
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The session this participant belongs to
    /// </summary>
    [Required]
    public Guid SessionId { get; set; }

    /// <summary>
    /// The user ID of this participant
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// When this user joined the session
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this user left the session (null if still active)
    /// </summary>
    public DateTime? LeftAt { get; set; }

    /// <summary>
    /// Whether this participant has muted the session
    /// </summary>
    public bool IsMuted { get; set; }

    /// <summary>
    /// The role of this participant in the session
    /// </summary>
    public ChatParticipantRole Role { get; set; } = ChatParticipantRole.Member;

    /// <summary>
    /// ID of the last message this participant has read
    /// </summary>
    public Guid? LastReadMessageId { get; set; }

    /// <summary>
    /// Timestamp when the participant last read messages
    /// </summary>
    public DateTime? LastReadAt { get; set; }

    /// <summary>
    /// Navigation property to the chat session
    /// </summary>
    public virtual ChatSession Session { get; set; } = null!;

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public virtual ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Navigation property to the last read message
    /// </summary>
    public virtual ChatMessage? LastReadMessage { get; set; }
}