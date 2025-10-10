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
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace Blazor.Chat.App.ApiService;

/// <summary>
/// Extension methods for registering chat-related services
/// </summary>
public static class RegisterDependentServices
{

    private static AppSettings? _appSettings;

    public static WebApplicationBuilder RegisterServices(this WebApplicationBuilder builder, out AppSettings? appSettings)
    {
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

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        builder.Services.AddApiVersioning(c =>
        {
            c.DefaultApiVersion = new ApiVersion(1, 0);
            c.AssumeDefaultVersionWhenUnspecified = true;
            c.ReportApiVersions = true;
        });

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new ApiInfo().GetApiVersion("v1"));
            //c.SwaggerDoc("v2", new ApiInfo().GetApiVersion("v2"));
            c.OperationFilter<SwaggerResponseOperationFilter>();
            c.DocumentFilter<AdditionalPropertiesDocumentFilter>();

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Name = "Authorization",
                Description = "Bearer Authentication with JWT Token",
                Type = SecuritySchemeType.Http
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Id = "Bearer",
                            Type = ReferenceType.SecurityScheme
                        }
                    },
                    new List<string>()
                }
            });

            // Add informative documentation on API Route Endpoints for auto documentation on Swagger page.
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            c.IncludeXmlComments(xmlPath);
        });

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info.Version = $"{new ApiInfo().GetAssemblyVersion()}";
                document.Info.Title = "Chat API Service";
                document.Info.Description =
                    "Documentation of all implemented endpoints, grouped by their route's base resource for Chat API Service.";
                document.Info.TermsOfService = new Uri("https://example.com/Terms-Of-Use");
                document.Info.Contact = new OpenApiContact
                {
                    Name = "Support Services",
                    Email = "Support@example.com",
                    Url = new Uri("https://example.com/")
                };
                document.Info.License = new OpenApiLicense
                {
                    Name = "Internal Only",
                    Url = new Uri("https://example.com/")
                };
                return Task.CompletedTask;
            });
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

        // Cosmos DB Client
        services.AddSingleton<CosmosClient>(serviceProvider =>
        {
            var clientOptions = new CosmosClientOptions
            {
                MaxRetryAttemptsOnRateLimitedRequests = _appSettings.CosmosDb.MaxRetryAttempts,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(_appSettings.CosmosDb.MaxRetryWaitTimeSeconds),
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
                    IgnoreNullValues = true
                }
            };

            // Use Managed Identity in production, connection string in development
            if (!string.IsNullOrEmpty(_appSettings.CosmosDb.PrimaryKey))
            {
                return new CosmosClient(_appSettings.CosmosDb.Endpoint, _appSettings.CosmosDb.PrimaryKey, clientOptions);
            }

            return new CosmosClient(_appSettings.CosmosDb.Endpoint, new DefaultAzureCredential(), clientOptions);
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