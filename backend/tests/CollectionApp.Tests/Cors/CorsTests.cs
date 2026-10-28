using CollectionApp.Tests.Infrastructure;

namespace CollectionApp.Tests.Cors;

[Collection("MySql")]
public class CorsTests(MySqlContainerFixture mySql)
{
    private const string HealthUrl = "/api/health";
    private const string AllowOriginHeader = "Access-Control-Allow-Origin";

    private static async Task<string?> GetAllowedOriginAsync(HttpClient client, string origin)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, HealthUrl);
        request.Headers.Add("Origin", origin);
        var response = await client.SendAsync(request);
        return response.Headers.TryGetValues(AllowOriginHeader, out var values) ? values.Single() : null;
    }

    private static async Task<string?> PreflightAllowedOriginAsync(HttpClient client, string origin)
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, HealthUrl);
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        var response = await client.SendAsync(request);
        return response.Headers.TryGetValues(AllowOriginHeader, out var values) ? values.Single() : null;
    }

    [Theory]
    [InlineData("http://localhost:8100")]
    [InlineData("https://localhost")]
    [InlineData("capacitor://localhost")]
    public async Task US4_AC1_ConfiguredOrigin_IsAllowed(string origin)
    {
        await using var factory = new ApiFactory(await mySql.CreateDatabaseAsync(migrate: true));
        var client = factory.CreateClient();

        Assert.Equal(origin, await GetAllowedOriginAsync(client, origin));
        Assert.Equal(origin, await PreflightAllowedOriginAsync(client, origin));
    }

    [Theory]
    [InlineData("http://evil.example")]
    [InlineData("http://localhost:8100.evil.com")]
    [InlineData("http://localhost:81000")]
    [InlineData("https://localhost:4443")]
    public async Task US4_AC2_UnknownOrigin_IsRejected(string origin)
    {
        await using var factory = new ApiFactory(await mySql.CreateDatabaseAsync(migrate: true));
        var client = factory.CreateClient();

        Assert.Null(await GetAllowedOriginAsync(client, origin));
        Assert.Null(await PreflightAllowedOriginAsync(client, origin));
    }

    [Fact]
    public async Task FR012_OriginsComeFromConfiguration()
    {
        await using var factory = new ApiFactory(await mySql.CreateDatabaseAsync(migrate: true), ["http://custom.test"]);
        var client = factory.CreateClient();

        Assert.Equal("http://custom.test", await GetAllowedOriginAsync(client, "http://custom.test"));
        foreach (var devOrigin in ApiFactory.DevOrigins)
        {
            Assert.Null(await GetAllowedOriginAsync(client, devOrigin));
        }
    }
}
