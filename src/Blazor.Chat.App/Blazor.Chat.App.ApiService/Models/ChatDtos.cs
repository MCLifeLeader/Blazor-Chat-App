using System.ComponentModel.DataAnnotations;

namespace Blazor.Chat.App.ApiService.Models
{
    /// <summary>
    /// DTO for creating a new chat session
    /// </summary>
    public record CreateSessionDto
    {
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Title { get; init; } = string.Empty;

        public bool IsGroup { get; init; }

        [StringLength(50)]
        public string? TenantId { get; init; }

        public List<string> InitialParticipantIds { get; init; } = new();
    }

    /// <summary>
    /// DTO for chat session information
    /// </summary>
    public record ChatSessionDto
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public bool IsGroup { get; init; }
        public string CreatedByUserId { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public DateTime LastActivityAt { get; init; }
        public string? TenantId { get; init; }
        public string State { get; init; } = string.Empty;
        public int ParticipantCount { get; init; }
        public int MessageCount { get; init; }
    }

    /// <summary>
    /// DTO for adding a new message to a chat session
    /// </summary>
    public record AddMessageDto
    {
        [Required]
        [StringLength(10000, MinimumLength = 1)]
        public string Content { get; init; } = string.Empty;

        public Guid? ReplyToMessageId { get; init; }

        public List<MessageAttachmentDto> Attachments { get; init; } = new();

        [StringLength(100)]
        public string? MessageType { get; init; } = "text";
    }

    /// <summary>
    /// DTO for message attachment information
    /// </summary>
    public record MessageAttachmentDto
    {
        [Required]
        public string FileName { get; init; } = string.Empty;

        [Required]
        public string ContentType { get; init; } = string.Empty;

        public long Size { get; init; }

        [Required]
        public string Url { get; init; } = string.Empty;
    }

    /// <summary>
    /// DTO for chat message information
    /// </summary>
    public record ChatMessageDto
    {
        public Guid Id { get; init; }
        public Guid SessionId { get; init; }
        public string SenderUserId { get; init; } = string.Empty;
        public string SenderDisplayName { get; init; } = string.Empty;
        public DateTime SentAt { get; init; }
        public string Content { get; init; } = string.Empty;
        public string Preview { get; init; } = string.Empty;
        public int MessageLength { get; init; }
        public string Status { get; init; } = string.Empty;
        public Guid? ReplyToMessageId { get; init; }
        public DateTime? EditedAt { get; init; }
        public int Version { get; init; }
        public List<MessageAttachmentDto> Attachments { get; init; } = new();
        public string MessageType { get; init; } = "text";
    }

    /// <summary>
    /// DTO for paginated list of messages
    /// </summary>
    public record ChatMessagesPageDto
    {
        public List<ChatMessageDto> Messages { get; init; } = new();
        public int TotalCount { get; init; }
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
        public bool HasNextPage { get; init; }
        public bool HasPreviousPage { get; init; }
    }

    /// <summary>
    /// DTO for adding a participant to a chat session
    /// </summary>
    public record AddParticipantDto
    {
        [Required]
        public string UserId { get; init; } = string.Empty;

        public string Role { get; init; } = "Member";
    }

    /// <summary>
    /// DTO for chat participant information
    /// </summary>
    public record ChatParticipantDto
    {
        public Guid Id { get; init; }
        public Guid SessionId { get; init; }
        public string UserId { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public DateTime JoinedAt { get; init; }
        public DateTime? LeftAt { get; init; }
        public bool IsMuted { get; init; }
        public string Role { get; init; } = string.Empty;
        public Guid? LastReadMessageId { get; init; }
        public DateTime? LastReadAt { get; init; }
    }

    /// <summary>
    /// DTO for editing a message
    /// </summary>
    public record EditMessageDto
    {
        [Required]
        [StringLength(10000, MinimumLength = 1)]
        public string Content { get; init; } = string.Empty;

        public List<MessageAttachmentDto> Attachments { get; init; } = new();
    }

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