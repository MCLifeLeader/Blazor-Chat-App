using System.ComponentModel.DataAnnotations;

namespace Blazor.Chat.App.Data.Db;

/// <summary>
/// Represents a chat session/room where participants can exchange messages
/// </summary>
public class ChatSession
{
    /// <summary>
    /// Unique identifier for the chat session
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Display title/name of the chat session
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if this is a group chat (true) or direct message (false)
    /// </summary>
    public bool IsGroup { get; set; }

    /// <summary>
    /// User ID of the user who created this session
    /// </summary>
    [Required]
    public string CreatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// When the session was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp of the last activity in this session
    /// </summary>
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional tenant identifier for multi-tenant scenarios
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Current state of the session
    /// </summary>
    public ChatSessionState State { get; set; } = ChatSessionState.Active;

    /// <summary>
    /// Navigation property for participants in this session
    /// </summary>
    public virtual ICollection<ChatParticipant> Participants { get; set; } = new List<ChatParticipant>();

    /// <summary>
    /// Navigation property for messages in this session
    /// </summary>
    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

    /// <summary>
    /// Foreign key reference to the user who created this session
    /// </summary>
    public virtual ApplicationUser CreatedByUser { get; set; } = null!;
}