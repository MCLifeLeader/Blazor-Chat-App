using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Blazor.Chat.App.ApiService.Helpers;

public class AdditionalPropertiesDocumentFilter : IDocumentFilter
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="openApiDoc"></param>
    /// <param name="context"></param>
    public void Apply(OpenApiDocument openApiDoc, DocumentFilterContext context)
    {
        foreach (var schema in context.SchemaRepository.Schemas
                     .Where(schema => schema.Value.AdditionalProperties is null))
        {
            schema.Value.AdditionalPropertiesAllowed = true;
        }
    }
}