using System.Net;
using System.Net.Http.Json;
using CollectionApp.Tests.Infrastructure;

namespace CollectionApp.Tests.Auth;

/// <summary>
/// POST /api/auth/refresh: rotation and absolute end of session (research R3, FR-013).
/// </summary>
[Collection("MySql")]
public class RefreshTests(MySqlContainerFixture mySql)
{
    private static Task<HttpResponseMessage> PostRefreshAsync(HttpClient client, string refreshToken) =>
        client.PostAsJsonAsync(AuthTestData.RefreshUrl, new { refreshToken });

    [Fact]
    public async Task Refresh_ValidToken_ReturnsNewPairAndRevokesOld()
    {
        await using var factory = new ApiFactory(await mySql.CreateDatabaseAsync(migrate: true));
        var (client, session) = await factory.CreateAuthenticatedClientAsync();
        factory.Clock.Advance(TimeSpan.FromMinutes(1));

        var response = await PostRefreshAsync(client, session.RefreshToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var renewed = (await response.Content.ReadFromJsonAsync<AuthSession>(AuthTestData.Json))!;
        Assert.NotEqual(session.RefreshToken, renewed.RefreshToken);
        Assert.NotEqual(session.AccessToken, renewed.AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await PostRefreshAsync(client, session.RefreshToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await PostRefreshAsync(client, renewed.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task FR013_Refresh_KeepsAbsoluteExpiry()
    {
        await using var factory = new ApiFactory(await mySql.CreateDatabaseAsync(migrate: true));
        var (client, session) = await factory.CreateAuthenticatedClientAsync();
        factory.Clock.Advance(TimeSpan.FromDays(3));

        var renewed = (await (await PostRefreshAsync(client, session.RefreshToken)).Content
            .ReadFromJsonAsync<AuthSession>(AuthTestData.Json))!;

        Assert.Equal(session.RefreshTokenExpiresAt, renewed.RefreshTokenExpiresAt, TimeSpan.FromMilliseconds(1));
        Assert.Equal(DateTimeKind.Utc, renewed.RefreshTokenExpiresAt.Kind);
    }

    [Fact]
    public async Task FR013_After7Days_RefreshReturns401()
    {
        await using var factory = new ApiFactory(await mySql.CreateDatabaseAsync(migrate: true));
        var (client, session) = await factory.CreateAuthenticatedClientAsync();

        factory.Clock.Advance(TimeSpan.FromDays(7));

        Assert.Equal(HttpStatusCode.Unauthorized, (await PostRefreshAsync(client, session.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Refresh_UnknownToken_Returns401()
    {
        await using var factory = new ApiFactory(await mySql.CreateDatabaseAsync(migrate: true));

        var response = await PostRefreshAsync(factory.CreateClient(), "unknown-token");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
