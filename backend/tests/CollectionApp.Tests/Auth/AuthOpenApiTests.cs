using System.Net;
using System.Text.Json;
using CollectionApp.Tests.Infrastructure;

namespace CollectionApp.Tests.Auth;

/// <summary>
/// Constitution III: the /api/auth endpoints are documented in OpenAPI, consistent with contracts/auth.openapi.yaml.
/// </summary>
public class AuthOpenApiTests
{
    private static async Task<JsonElement> GetDocumentAsync()
    {
        await using var factory = new ApiFactory(MySqlContainerFixture.UnreachableConnectionString, swagger: true);
        var response = await factory.CreateClient().GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }

    [Theory]
    [InlineData("/api/auth/register", "post", new[] { "201", "400", "409" })]
    [InlineData("/api/auth/login", "post", new[] { "200", "400", "401", "423" })]
    [InlineData("/api/auth/refresh", "post", new[] { "200", "400", "401" })]
    [InlineData("/api/auth/logout", "post", new[] { "204", "401" })]
    [InlineData("/api/auth/me", "get", new[] { "200", "401" })]
    public async Task Endpoint_IsDocumentedWithItsResponses(string path, string method, string[] statuses)
    {
        var document = await GetDocumentAsync();

        var responses = document.GetProperty("paths").GetProperty(path).GetProperty(method).GetProperty("responses");
        Assert.Equal(statuses.Order(), responses.EnumerateObject().Select(p => p.Name).Order());
    }

    [Theory]
    [InlineData("/api/auth/register", "post", false)]
    [InlineData("/api/auth/login", "post", false)]
    [InlineData("/api/auth/refresh", "post", false)]
    [InlineData("/api/auth/logout", "post", true)]
    [InlineData("/api/auth/me", "get", true)]
    public async Task BearerSecurity_OnlyOnReservedEndpoints(string path, string method, bool secured)
    {
        var document = await GetDocumentAsync();

        var operation = document.GetProperty("paths").GetProperty(path).GetProperty(method);
        var schemes = operation.TryGetProperty("security", out var security)
            ? security.EnumerateArray().SelectMany(s => s.EnumerateObject().Select(p => p.Name)).ToList()
            : [];
        Assert.Equal(secured ? ["bearerAuth"] : [], schemes);
    }

    [Fact]
    public async Task Schemas_NeverExposeThePassword()
    {
        var document = await GetDocumentAsync();

        var schemas = document.GetProperty("components").GetProperty("schemas");
        foreach (var name in new[] { "UserDto", "AuthSessionDto" })
        {
            var properties = schemas.GetProperty(name).GetProperty("properties").EnumerateObject().Select(p => p.Name).ToList();
            Assert.DoesNotContain(properties, p => p.Contains("password", StringComparison.OrdinalIgnoreCase));
        }
        Assert.Equal(["claims", "email", "firstName", "id", "lastName"],
            schemas.GetProperty("UserDto").GetProperty("properties").EnumerateObject().Select(p => p.Name).Order());
    }
}
