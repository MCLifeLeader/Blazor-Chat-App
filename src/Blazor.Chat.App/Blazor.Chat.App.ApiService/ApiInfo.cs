using Microsoft.OpenApi.Models;

namespace Blazor.Chat.App.ApiService;

/// <summary>
/// 
/// </summary>
internal class ApiInfo
{
    /// <summary>
    /// </summary>
    /// <param name="version"></param>
    /// <returns></returns>
    public OpenApiInfo GetApiVersion(string version)
    {
        return new OpenApiInfo
        {
            Title = $"Chat API Service {version}",
            Version = $"{version}",
            Description = $"Chat API Service documentation, &copy; 2023 - {DateTime.UtcNow:yyyy} - Chat API Service - " +
                          $"Build Version: {GetType().Assembly.GetName().Version}"
        };
    }

    public Version? GetAssemblyVersion()
    {
        return GetType().Assembly.GetName().Version;
    }
}