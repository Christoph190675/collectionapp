using System.Net;
using CollectionApp.Tests.Infrastructure;

namespace CollectionApp.Tests.Auth;

/// <summary>
/// Public and reserved endpoints (User Story 4).
/// </summary>
[Collection("MySql")]
public class ProtectedEndpointTests(MySqlContainerFixture mySql)
{
    [Fact]
    public async Task US4_AC2_HealthStaysAnonymous()
    {
        await using var factory = new ApiFactory(await mySql.CreateDatabaseAsync(migrate: true));

        Assert.Equal(HttpStatusCode.OK, (await factory.CreateClient().GetAsync("/api/health")).StatusCode);
    }

    [Fact]
    public async Task US4_AC3_ExpiredAccessToken_Returns401()
    {
        await using var factory = new ApiFactory(await mySql.CreateDatabaseAsync(migrate: true));
        var (client, _) = await factory.CreateAuthenticatedClientAsync();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(AuthTestData.MeUrl)).StatusCode);

        factory.Clock.Advance(TimeSpan.FromMinutes(15).Add(TimeSpan.FromSeconds(31)));

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(AuthTestData.MeUrl)).StatusCode);
    }

    [Fact]
    public async Task AccessTokenStillValidWithinClockSkew()
    {
        await using var factory = new ApiFactory(await mySql.CreateDatabaseAsync(migrate: true));
        var (client, _) = await factory.CreateAuthenticatedClientAsync();

        factory.Clock.Advance(TimeSpan.FromMinutes(15).Add(TimeSpan.FromSeconds(20)));

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(AuthTestData.MeUrl)).StatusCode);
    }
}
