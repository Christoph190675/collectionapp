using System.Net;
using System.Net.Http.Json;
using CollectionApp.Tests.Infrastructure;

namespace CollectionApp.Tests.Auth;

/// <summary>
/// 20 POST /api/auth/* per minute and per IP (research R12). Only test class with rate limiting on.
/// </summary>
[Collection("MySql")]
public class RateLimitTests(MySqlContainerFixture mySql)
{
    [Fact]
    public async Task R12_TwentyFirstLoginInAMinute_Returns429()
    {
        await using var factory = new ApiFactory(await mySql.CreateDatabaseAsync(migrate: true), rateLimiting: true);
        var client = factory.CreateClient();

        for (var i = 0; i < 20; i++)
        {
            var response = await client.PostAsJsonAsync(AuthTestData.LoginUrl, new { email = AuthTestData.NewEmail(), password = "Wrong2026" });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        var blocked = await client.PostAsJsonAsync(AuthTestData.LoginUrl, new { email = AuthTestData.NewEmail(), password = "Wrong2026" });

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
    }

    [Fact]
    public async Task R12_HealthIsNotLimited()
    {
        await using var factory = new ApiFactory(await mySql.CreateDatabaseAsync(migrate: true), rateLimiting: true);
        var client = factory.CreateClient();

        for (var i = 0; i < 25; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/health")).StatusCode);
        }
    }
}
