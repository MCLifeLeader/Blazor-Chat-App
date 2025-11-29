using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Blazor.Chat.App.ApiService.Controllers;

/// <summary>
/// API controller for application information and metadata
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class InfoController : ControllerBase
{
    private readonly ILogger<InfoController> _logger;

    /// <summary>
    /// Initializes a new instance of the InfoController class
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public InfoController(ILogger<InfoController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get the application version
    /// </summary>
    /// <returns>Application version string</returns>
    [HttpGet("Version")]
    [Produces("text/plain")]
    [AllowAnonymous]
    public IActionResult GetVersion()
    {
        try
        {
            var version = GetType().Assembly.GetName().Version?.ToString() ?? "Unknown";
            _logger.LogDebug("Version requested: {Version}", version);
            return Content(version, "text/plain");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving version information");
            return Content("Error", "text/plain");
        }
    }
}
