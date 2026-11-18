using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CollectionApp.Api.Swagger
{
    /// <summary>
    /// Adds the bearer security requirement to the operations that require an authenticated user
    /// </summary>
    public class BearerSecurityOperationFilter : IOperationFilter
    {
        public const string SchemeName = "bearerAuth";

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
            if (!metadata.OfType<IAuthorizeData>().Any() || metadata.OfType<IAllowAnonymous>().Any())
            {
                return;
            }

            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(SchemeName, context.Document)] = [],
            });
        }
    }
}
