using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using System.Net;

namespace Blazor.Chat.App.ApiService.Helpers;

/// <summary>
/// Adds default error responses (400, 401, 404, 500) to controller operations.
/// </summary>
public sealed class DefaultResponsesOperationTransformer : IOpenApiOperationTransformer
{
    /// <summary>
    /// Transforms the operation by adding default error responses for controller endpoints.
    /// </summary>
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        // Only apply to controller-based endpoints
        var declaringType = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
            .FirstOrDefault()?.ControllerTypeInfo;

        if (declaringType is null)
        {
            return Task.CompletedTask;
        }

        // Check if it's a controller-based action
        var isController = typeof(ControllerBase).IsAssignableFrom(declaringType);
        if (!isController)
        {
            return Task.CompletedTask;
        }

        // Add default error responses
        AddResponseIfNotExists(operation, HttpStatusCode.InternalServerError, "500 - See Error Results for Details");
        AddResponseIfNotExists(operation, HttpStatusCode.BadRequest, "400 - See Error Results for Details");
        AddResponseIfNotExists(operation, HttpStatusCode.NotFound, "404 - See Error Results for Details");
        AddResponseIfNotExists(operation, HttpStatusCode.Unauthorized, "401");

        return Task.CompletedTask;
    }

    private static void AddResponseIfNotExists(
        OpenApiOperation operation,
        HttpStatusCode statusCode,
        string description)
    {
        var statusCodeString = ((int)statusCode).ToString();

        if (operation.Responses?.ContainsKey(statusCodeString) == true)
        {
            return;
        }

        operation.Responses ??= [];

        var response = new OpenApiResponse
        {
            Description = description
        };

        operation.Responses.Add(statusCodeString, response);
    }
}
