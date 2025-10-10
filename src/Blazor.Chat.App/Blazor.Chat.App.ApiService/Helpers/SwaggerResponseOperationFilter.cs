using Blazor.Chat.App.ApiService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Net;

namespace Blazor.Chat.App.ApiService.Helpers;

public class SwaggerResponseOperationFilter : IOperationFilter
{
    /// <summary>
    /// Applies the specified operation. Adds 500 ServerError to Swagger documentation for all endpoints
    /// </summary>
    /// <param name="operation">The operation to apply the filter to.</param>
    /// <param name="context">The context for the operation filter.</param>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // ensure we are filtering on controllers
        if (context?.MethodInfo?.DeclaringType?.BaseType?.BaseType == typeof(ControllerBase) ||
            context?.MethodInfo?.ReflectedType?.BaseType == typeof(Controller))
        {
            HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
            // Allow override of response codes by checking for existing status code key
            if (!operation.Responses.ContainsKey($"{(int)statusCode}"))
            {
                operation.Responses.Add($"{(int)statusCode}", new OpenApiResponse
                {
                    Description = $"{statusCode} - See Error Results for Details",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        {
                            "application/json", new OpenApiMediaType
                            {
                                Schema = context.SchemaGenerator.GenerateSchema(typeof(ErrorResult), context.SchemaRepository)
                            }
                        }
                    }
                });
            }

            statusCode = HttpStatusCode.BadRequest;
            // Allow override of response codes by checking for existing status code key
            if (!operation.Responses.ContainsKey($"{(int)statusCode}"))
            {
                operation.Responses.Add(((int)statusCode).ToString(), new OpenApiResponse
                {
                    Description = $"{statusCode} - See Error Results for Details",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        {
                            "application/json", new OpenApiMediaType
                            {
                                Schema = context.SchemaGenerator.GenerateSchema(typeof(ErrorResult), context.SchemaRepository)
                            }
                        }
                    }
                });
            }

            statusCode = HttpStatusCode.NotFound;
            // Allow override of response codes by checking for existing status code key
            if (!operation.Responses.ContainsKey($"{(int)statusCode}"))
            {
                operation.Responses.Add(((int)statusCode).ToString(), new OpenApiResponse
                {
                    Description = $"{statusCode} - See Error Results for Details",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        {
                            "application/json", new OpenApiMediaType
                            {
                                Schema = context.SchemaGenerator.GenerateSchema(typeof(ErrorResult), context.SchemaRepository)
                            }
                        }
                    }
                });
            }

            statusCode = HttpStatusCode.Unauthorized;
            // Allow override of response codes by checking for existing status code key
            if (!operation.Responses.ContainsKey($"{(int)statusCode}"))
            {
                operation.Responses.Add(((int)statusCode).ToString(), new OpenApiResponse
                {
                    Description = $"{statusCode}",
                });
            }
        }
    }
}