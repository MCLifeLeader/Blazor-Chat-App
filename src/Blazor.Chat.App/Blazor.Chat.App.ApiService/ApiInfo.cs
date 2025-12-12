namespace Blazor.Chat.App.ApiService;

/// <summary>
/// 
/// </summary>
internal class ApiInfo
{
    public Version? GetAssemblyVersion()
    {
        return GetType().Assembly.GetName().Version;
    }
}