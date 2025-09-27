using System.ComponentModel.DataAnnotations;

namespace Blazor.Chat.App.ApiService.Models;

/// <summary>
/// DTO for message attachment information
/// </summary>
public record MessageAttachmentDto
{
    /// <summary>
    /// Original filename of the attachment
    /// </summary>
    [Required]
    public string FileName { get; init; } = string.Empty;

    /// <summary>
    /// MIME content type of the attachment
    /// </summary>
    [Required]
    public string ContentType { get; init; } = string.Empty;

    /// <summary>
    /// Size of the attachment in bytes
    /// </summary>
    public long Size { get; init; }

    /// <summary>
    /// URL where the attachment can be accessed
    /// </summary>
    [Required]
    public string Url { get; init; } = string.Empty;
}