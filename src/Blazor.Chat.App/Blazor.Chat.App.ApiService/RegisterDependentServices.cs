using Asp.Versioning;
using Azure.Identity;
using Blazor.Chat.App.ApiService.Helpers;
using Blazor.Chat.App.ApiService.HostedServices;
using Blazor.Chat.App.ApiService.Models.Settings;
using Blazor.Chat.App.ApiService.Services;
using Blazor.Chat.App.Data.Cosmos.Configuration;
using Blazor.Chat.App.Data.Cosmos.Repositories;
using Blazor.Chat.App.Data.Db;
using Blazor.Chat.App.Data.Sql;
using Blazor.Chat.App.Data.Sql.Repositories;
using Blazor.Chat.App.ServiceDefaults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using System.Net.Http; // For HttpClientHandler and HttpClient

namespace Blazor.Chat.App.ApiService;

/// <summary>
/// Extension methods for registering chat-related services
/// </summary>
public static class RegisterDependentServices
{

    private static AppSettings? _appSettings;

    public static WebApplicationBuilder RegisterServices(this WebApplicationBuilder builder, out AppSettings? appSettings)
    {
        // Bind strongly-typed AppSettings from configuration and register via Options pattern
        // This ensures configuration is available early and avoids static state issues at runtime.
        _appSettings = new AppSettings();
        builder.Configuration.Bind(_appSettings);
        builder.Services.Configure<AppSettings>(builder.Configuration);

        // Add service defaults & Aspire client integrations.
        builder.AddServiceDefaults();

        // Add chat services
        builder.Services.AddChatServices(builder.Configuration);

        // Add MVC controllers
        builder.Services.AddControllers();

        // Add authentication and authorization
        builder.Services.AddAuthentication()
            .AddBearerToken(IdentityConstants.BearerScheme);
        builder.Services.AddAuthorizationBuilder();

        // Add services to the container.
        builder.Services.AddProblemDetails();

        builder.Services.AddApiVersioning(c =>
        {
            c.DefaultApiVersion = new ApiVersion(1, 0);
            c.AssumeDefaultVersionWhenUnspecified = true;
            c.ReportApiVersions = true;
        });

        // Learn more about configuring OpenAPI at https://aka.ms/aspnetcore/openapi
        builder.Services.AddOpenApi("v1", options =>
        {
            // Add document transformer for metadata
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "Chat API Service",
                    Version = $"{new ApiInfo().GetAssemblyVersion()}",
                    Description = $"Chat API Service documentation, © 2023 - {DateTime.UtcNow:yyyy} - Build Version: {typeof(ApiInfo).Assembly.GetName().Version}",
                    TermsOfService = new Uri("https://example.com/Terms-Of-Use"),
                    Contact = new OpenApiContact
                    {
                        Name = "Support Services",
                        Email = "Support@example.com",
                        Url = new Uri("https://example.com/")
                    },
                    License = new OpenApiLicense
                    {
                        Name = "Internal Only",
                        Url = new Uri("https://example.com/")
                    }
                };
                return Task.CompletedTask;
            });

            // Add document transformer for Bearer security scheme
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Name = "Authorization",
                    Description = "Bearer Authentication with JWT Token",
                    Type = SecuritySchemeType.Http
                };
                return Task.CompletedTask;
            });

            // Add document transformer for security requirement
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                var securitySchemeRef = new OpenApiSecuritySchemeReference("Bearer", document);
                var securityRequirement = new OpenApiSecurityRequirement
                {
                    { securitySchemeRef, [] }
                };

                document.Security ??= [];
                document.Security.Add(securityRequirement);
                return Task.CompletedTask;
            });

            // Add operation transformer for default responses
            options.AddOperationTransformer<DefaultResponsesOperationTransformer>();
        });

        appSettings = _appSettings;

        return builder;
    }


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
        // Entity Framework DbContext
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Bind CosmosDbOptions from configuration and register via Options pattern with validation
        services
            .AddOptions<CosmosDbOptions>()
            .Bind(configuration.GetSection(CosmosDbOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Endpoint), "CosmosDb.Endpoint is required.")
            .Validate(o =>
            {
                // Validate that Endpoint is an absolute URI. Accept HTTPS everywhere.
                // Accept HTTP only for known local emulator hosts so local development works with emulator endpoints.
                if (!Uri.TryCreate(o.Endpoint, UriKind.Absolute, out var uri))
                {
                    return false;
                }

                if (uri.Scheme == Uri.UriSchemeHttps)
                {
                    return true;
                }

                if (uri.Scheme == Uri.UriSchemeHttp)
                {
                    var host = uri.Host;
                    if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(host, "host.docker.internal", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }, "CosmosDb.Endpoint must be a valid https URI or a local emulator http URI.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.DatabaseName), "CosmosDb.DatabaseName is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.ContainerName), "CosmosDb.ContainerName is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.PartitionKey) && o.PartitionKey.StartsWith('/'), "CosmosDb.PartitionKey must start with '/'.")
            .ValidateOnStart();

        // Cosmos DB Client
        services.AddSingleton<CosmosClient>(serviceProvider =>
        {
            var cosmosOptions = serviceProvider.GetRequiredService<IOptions<CosmosDbOptions>>().Value;

            // Parse the endpoint and, if running in a container, use host.docker.internal when configured for localhost.
            var endpointUri = new Uri(cosmosOptions.Endpoint);
            var runningInContainer = string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase);
            if (runningInContainer && string.Equals(endpointUri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                // Redirect to the host machine from inside the container.
                endpointUri = new UriBuilder(endpointUri) { Host = "host.docker.internal" }.Uri;
            }

            var isLocalEmulator = string.Equals(endpointUri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
                                 || string.Equals(endpointUri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
                                 || string.Equals(endpointUri.Host, "host.docker.internal", StringComparison.OrdinalIgnoreCase);

            var clientOptions = new CosmosClientOptions
            {
                MaxRetryAttemptsOnRateLimitedRequests = cosmosOptions.MaxRetryAttempts,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(cosmosOptions.MaxRetryWaitTimeSeconds),
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
                    IgnoreNullValues = true
                },
                // Gateway is the most compatible option with the emulator
                ConnectionMode = ConnectionMode.Gateway,
                // Limit network calls to the provided endpoint (useful for emulator)
                LimitToEndpoint = true
            };

            // When talking to the local Cosmos DB Emulator, the TLS certificate is self-signed. In development we
            // allow the connection by relaxing cert validation ONLY for localhost/host.docker.internal endpoints.
            if (isLocalEmulator)
            {
                clientOptions.HttpClientFactory = () =>
                {
                    var handler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (request, cert, chain, errors) => true
                    };
                    return new HttpClient(handler, disposeHandler: true);
                };
            }

            // Use Managed Identity in production, primary key in development
            if (!string.IsNullOrEmpty(cosmosOptions.PrimaryKey))
            {
                return new CosmosClient(endpointUri.ToString(), cosmosOptions.PrimaryKey, clientOptions);
            }

            return new CosmosClient(endpointUri.ToString(), new DefaultAzureCredential(), clientOptions);
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
}