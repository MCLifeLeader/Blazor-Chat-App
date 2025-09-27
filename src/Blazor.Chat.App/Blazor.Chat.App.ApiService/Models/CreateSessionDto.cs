using System.ComponentModel.DataAnnotations;

namespace Blazor.Chat.App.ApiService.Models;

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