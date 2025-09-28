using Azure.Identity;
using Blazor.Chat.App.ApiService.HostedServices;
using Blazor.Chat.App.ApiService.Services;
using Blazor.Chat.App.Data.Db;
using Blazor.Chat.App.Data.Repositories;
using Blazor.Chat.App.ServiceDefaults.Configuration;
using Blazor.Chat.App.ServiceDefaults.Repositories;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;

namespace Blazor.Chat.App.ApiService;

/// <summary>
/// Extension methods for registering chat-related services
/// </summary>
public static class RegisterDependentServices
{
    /// <summary>
    /// Register all chat-related services and dependencies
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddChatServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Configuration options
        services.Configure<CosmosDbOptions>(configuration.GetSection(CosmosDbOptions.SectionName));
        services.Configure<OutboxProcessorOptions>(configuration.GetSection(OutboxProcessorOptions.SectionName));

        // Entity Framework DbContext
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Cosmos DB Client
        services.AddSingleton<CosmosClient>(serviceProvider =>
        {
            var cosmosOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CosmosDbOptions>>().Value;
            
            var clientOptions = new CosmosClientOptions
            {
                MaxRetryAttemptsOnRateLimitedRequests = cosmosOptions.MaxRetryAttempts,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(cosmosOptions.MaxRetryWaitTimeSeconds),
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
                    IgnoreNullValues = true
                }
            };

            // Use Managed Identity in production, connection string in development
            if (!string.IsNullOrEmpty(cosmosOptions.PrimaryKey))
            {
                return new CosmosClient(cosmosOptions.Endpoint, cosmosOptions.PrimaryKey, clientOptions);
            }
            else
            {
                return new CosmosClient(cosmosOptions.Endpoint, new DefaultAzureCredential(), clientOptions);
            }
        });

        // Repository registrations
        services.AddScoped<ISqlChatRepository, SqlChatRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IChatCosmosRepository, ChatCosmosRepository>();

        // Service registrations
        services.AddScoped<IChatService, ChatService>();

        // Hosted services
        services.AddHostedService<OutboxProcessorHostedService>();

        return services;
    }

    /// <summary>
    /// Initialize Cosmos DB database and container if they don't exist
    /// </summary>
    /// <param name="serviceProvider">Service provider</param>
    /// <returns>Task representing the initialization work</returns>
    public static async Task InitializeCosmosDbAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var cosmosClient = scope.ServiceProvider.GetRequiredService<CosmosClient>();
        var cosmosOptions = scope.ServiceProvider
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<CosmosDbOptions>>().Value;
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<CosmosClient>>();

        try
        {
            // Create database if it doesn't exist
            var databaseResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync(
                cosmosOptions.DatabaseName,
                cosmosOptions.RequestUnits);

            if (databaseResponse.StatusCode == System.Net.HttpStatusCode.Created)
            {
                logger.LogInformation("Created Cosmos DB database: {DatabaseName}", cosmosOptions.DatabaseName);
            }

            // Create container if it doesn't exist
            var containerProperties = new ContainerProperties(
                cosmosOptions.ContainerName, 
                cosmosOptions.PartitionKey);

            var containerResponse = await databaseResponse.Database.CreateContainerIfNotExistsAsync(
                containerProperties,
                cosmosOptions.RequestUnits);

            if (containerResponse.StatusCode == System.Net.HttpStatusCode.Created)
            {
                logger.LogInformation("Created Cosmos DB container: {ContainerName}", cosmosOptions.ContainerName);
            }

            logger.LogInformation("Cosmos DB initialization completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize Cosmos DB");
            throw;
        }
    }
}