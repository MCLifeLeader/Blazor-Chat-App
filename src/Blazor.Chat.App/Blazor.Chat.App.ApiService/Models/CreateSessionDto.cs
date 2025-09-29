using System.ComponentModel.DataAnnotations;

namespace Blazor.Chat.App.ApiService.Models;

/// <summary>
/// DTO for creating a new chat session
/// </summary>
public record CreateSessionDto
{
    /// <summary>
    /// The title/name of the chat session
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Indicates whether this is a group chat (true) or direct message (false)
    /// </summary>
    public bool IsGroup { get; init; }

    /// <summary>
    /// Optional tenant identifier for multi-tenant scenarios
    /// </summary>
    [StringLength(50)]
    public string? TenantId { get; init; }

    /// <summary>
    /// List of user IDs to add as initial participants in the session
    /// </summary>
    public List<string> InitialParticipantIds { get; init; } = new();
}