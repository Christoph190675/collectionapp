using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CollectionApp.Api.Swagger
{
    public class CollectionAppSwaggerOptions : IConfigureNamedOptions<SwaggerGenOptions>
    {

        private readonly IApiVersionDescriptionProvider _provider;
        private string _title;

        public CollectionAppSwaggerOptions(IApiVersionDescriptionProvider provider)
        {
            _title = "CollectionApp API";
            _provider = provider;
        }

        public void Configure(SwaggerGenOptions options)
        {
            // add swagger document for every API version discovered
            foreach (var description in _provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(
                    description.GroupName,
                    CreateVersionInfo(description));
            }
        }

        public void Configure(string name, SwaggerGenOptions options)
        {
            _title = name;
            Configure(options);
        }

        private OpenApiInfo CreateVersionInfo( ApiVersionDescription description)
        {
            var info = new OpenApiInfo()
            {
                Title = _title,
                Version = description.ApiVersion.ToString()
            };

            if (description.IsDeprecated)
            {
                info.Description += " - deprecated.";
            }

            return info;
        }
    }
}