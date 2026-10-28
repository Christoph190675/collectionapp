using System.Net;
using System.Text.Json;
using CollectionApp.Tests.Infrastructure;

namespace CollectionApp.Tests.Health;

/// <summary>
/// Constitution III: the endpoint is documented in OpenAPI, consistent with contracts/health.openapi.yaml.
/// </summary>
public class HealthOpenApiTests
{
    [Fact]
    public async Task HealthEndpoint_IsDocumented_With200And503()
    {
        await using var factory = new ApiFactory(MySqlContainerFixture.UnreachableConnectionString, swagger: true);

        var response = await factory.CreateClient().GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var responses = document.GetProperty("paths").GetProperty("/api/health").GetProperty("get").GetProperty("responses");
        foreach (var status in new[] { "200", "503" })
        {
            var schemaRef = responses.GetProperty(status).GetProperty("content").GetProperty("application/json")
                .GetProperty("schema").GetProperty("$ref").GetString();
            Assert.EndsWith("/HealthStatusDto", schemaRef);
        }

        var properties = document.GetProperty("components").GetProperty("schemas").GetProperty("HealthStatusDto")
            .GetProperty("properties");
        Assert.Equal(["api", "checkedAt", "database"], properties.EnumerateObject().Select(p => p.Name).Order());
    }
}
