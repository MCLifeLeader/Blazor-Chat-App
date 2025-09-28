namespace Blazor.Chat.App.ApiService.Models.Settings;

public class AppSettings
{
    public Logging Logging { get; set; }
    public string AllowedHosts { get; set; }
    public Cosmosdb CosmosDb { get; set; }
    public Outboxprocessor OutboxProcessor { get; set; }
}

public class Logging
{
    public Loglevel LogLevel { get; set; }
}

public class Loglevel
{
    public string Default { get; set; }
    public string MicrosoftAspNetCore { get; set; }
}

public class Cosmosdb
{
    public string Endpoint { get; set; }
    public string DatabaseName { get; set; }
    public string ContainerName { get; set; }
    public string PartitionKey { get; set; }
}

public class Outboxprocessor
{
    public int BatchSize { get; set; }
    public int ProcessingIntervalSeconds { get; set; }
    public int MaxRetryAttempts { get; set; }
    public int MaxConcurrency { get; set; }
}
