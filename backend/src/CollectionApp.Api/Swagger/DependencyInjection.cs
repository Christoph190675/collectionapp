using CollectionApp.Domain.Utils;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi;

namespace CollectionApp.Api.Swagger
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Add Swagger to Service Collection
        /// </summary>
        /// <param name="services">An IServiceCollection</param>
        /// <param name="configuration">An IConfiguration</param>
        public static void AddSwagger(this IServiceCollection services, IConfiguration configuration)
        {
            if (configuration["swagger"]?.ToBool() ?? false)
            {
                services.AddSwaggerGen(c =>
                {
                    // Access tokens issued by POST /api/auth/login (contracts/auth.openapi.yaml)
                    c.AddSecurityDefinition(BearerSecurityOperationFilter.SchemeName, new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        In = ParameterLocation.Header,
                        Name = HeaderNames.Authorization,
                        Description = "Access token JWT, valable 15 minutes",
                    });
                    // Only on [Authorize] endpoints
                    c.OperationFilter<BearerSecurityOperationFilter>();
                });
                services.ConfigureOptions<CollectionAppSwaggerOptions>();
            }
        }
    
    
        /// <summary>
        /// Add swagger to Application Builder
        /// </summary>
        /// <param name="app">An IApplicationBuilder</param>
        /// <param name="configuration">An IConfiguration</param>
        /// <param name="apiVersionProvider">An IApiVersionDescriptionProvider</param>
        public static void AddSwagger(this IApplicationBuilder app, IConfiguration configuration, IApiVersionDescriptionProvider apiVersionProvider)
        {
            if (configuration["swagger"]?.ToBool() ?? false)
            {
                app.UseSwagger();

                app.UseSwaggerUI(c =>
                {
                    foreach (var description in apiVersionProvider.ApiVersionDescriptions)
                    {
                        c.SwaggerEndpoint(
                            $"/swagger/{description.GroupName}/swagger.json",
                            description.GroupName.ToUpperInvariant());
                    }
                    c.RoutePrefix = "swagger";
                }
                );
            }
        }
    }
}