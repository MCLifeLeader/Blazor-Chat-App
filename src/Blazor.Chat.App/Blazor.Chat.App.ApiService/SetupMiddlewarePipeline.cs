using Blazor.Chat.App.ApiService.Models.Settings;
using Blazor.Chat.App.Data.Cosmos.Configuration;
using Blazor.Chat.App.ServiceDefaults;
using Microsoft.Azure.Cosmos;
using Scalar.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Blazor.Chat.App.ApiService;

/// <summary>
///
/// </summary>
public static class SetupMiddlewarePipeline
{
    private static readonly string _swaggerName = "Chat API Service";

    #region Main Middleware Pipeline

    /// <summary>
    /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="appSettings"></param>
    /// <returns></returns>
    public static WebApplication SetupMiddleware(this WebApplication app, AppSettings? appSettings)
    {
        // Initialize Cosmos DB
        InitializeCosmosDbAsync(app.Services).GetAwaiter().GetResult();

        // Map Swagger-generated OpenAPI document at /openapi/{documentName}.json for consistent endpoint
        app.UseSwagger(c =>
        {
            c.RouteTemplate = "openapi/{documentName}.json";
        });

        app.UseSwaggerUI(c =>
        {
            c.EnableTryItOutByDefault();
            c.DocExpansion(DocExpansion.None);
            c.EnableFilter();
            c.DisplayRequestDuration();
            c.EnableDeepLinking();
            c.SwaggerEndpoint("/openapi/v1.json", $"{_swaggerName} v1");
            c.InjectStylesheet("/css/SwaggerDark.css");
            c.DocumentTitle = $"{_swaggerName} Swagger UI";
        });

        // Configure Scalar to use the Swagger-generated OpenAPI document
        app.MapScalarApiReference(options =>
        {
            options
                .WithTitle(_swaggerName)
                .WithOpenApiRoutePattern("/openapi/{documentName}.json")
                .AddDocument("v1", $"{_swaggerName} v1");
        });


        // Configure the HTTP request pipeline.
        app.UseExceptionHandler();

        // Add authentication and authorization middleware
        app.UseAuthentication();
        app.UseAuthorization();

        // Map controllers
        app.MapControllers();

        string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

        // Explicitly map weather endpoint that returns JSON
        app.MapGet("/weatherforecast", () =>
        {
            var forecast = Enumerable.Range(1, 5).Select(index =>
                    new WeatherForecast
                    (
                        DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                        Random.Shared.Next(-20, 55),
                        summaries[Random.Shared.Next(summaries.Length)]
                    ))
                .ToArray();
            return Results.Json(forecast);
        })
            .WithName("GetWeatherForecast");

        // Serve index.html as the default file for the root URL
        app.UseDefaultFiles();
        app.UseStaticFiles();

        // Fallback to index.html ONLY when no other endpoint matched.
        // This avoids intercepting API routes like /weatherforecast.
        app.MapFallbackToFile("index.html");

        app.MapDefaultEndpoints();

        return app;
    }

    #endregion


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