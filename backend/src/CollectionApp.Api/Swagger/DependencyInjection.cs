using CollectionApp.Domain.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
                    // Include 'SecurityScheme'
                    var jwtSecurityScheme = new OpenApiSecurityScheme
                    {
                        Flows = new OpenApiOAuthFlows
                        {
                            ClientCredentials = new OpenApiOAuthFlow
                            {
                                TokenUrl = new Uri(configuration["EndPoints:AuthenticationAPI"] + "/identity/oauth/token"),
                            },
                            Password = new OpenApiOAuthFlow
                            {
                                TokenUrl = new Uri(configuration["EndPoints:AuthenticationAPI"] + "/identity/oauth/token"),
                            }
                        },
                        In = ParameterLocation.Header,
                        Name = HeaderNames.Authorization,
                        Type = SecuritySchemeType.OAuth2
                    };

                    c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, jwtSecurityScheme);   
                    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference("bearer", document)] = new List<string>()
                    });
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