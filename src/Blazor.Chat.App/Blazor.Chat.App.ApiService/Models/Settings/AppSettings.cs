namespace Blazor.Chat.App.ApiService.Models.Settings;

public class AppSettings
{
    public Logging Logging { get; set; }
    public string AllowedHosts { get; set; }
    public Cosmosdb CosmosDb { get; set; }
    public Outboxprocessor OutboxProcessor { get; set; }
}