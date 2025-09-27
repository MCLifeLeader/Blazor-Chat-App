using Asp.Versioning;
using Blazor.Chat.App.ApiService;
using Blazor.Chat.App.ApiService.Helpers;
using Blazor.Chat.App.ServiceDefaults;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

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


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.EnableTryItOutByDefault();
    c.DocExpansion(DocExpansion.None);
    c.EnableFilter();
    c.DisplayRequestDuration();
    c.EnableDeepLinking();
    c.SwaggerEndpoint("/openapi/v1.json", $"Chat API Service v1");
    c.InjectStylesheet("/css/SwaggerDark.css");
    c.DocumentTitle = $"Chat API Service Swagger UI";
});

app.MapOpenApi();
app.MapScalarApiReference();


// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

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
        return forecast;
    })
    .WithName("GetWeatherForecast");

// Serve index.html as the default file for the root URL
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();

app.MapDefaultEndpoints();

app.Run();

namespace Blazor.Chat.App.ApiService
{
}