using Blazor.Chat.App.ApiService;
using Blazor.Chat.App.ApiService.Models.Settings;

await WebApplication.CreateBuilder(args)
    .RegisterServices(out AppSettings? appSettings)
    .Build()
    .SetupMiddleware(appSettings)
    .RunAsync();