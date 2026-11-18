using System.Net;
using System.Net.Http.Json;
using CollectionApp.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CollectionApp.Tests.Auth;

/// <summary>
/// POST /api/auth/logout (User Story 3, FR-014).
/// </summary>
[Collection("MySql")]
public class LogoutTests(MySqlContainerFixture mySql)
{
    private static Task<HttpResponseMessage> PostLogoutAsync(HttpClient client, string refreshToken) =>
        client.PostAsJsonAsync(AuthTestData.LogoutUrl, new { refreshToken });

    private static Task<HttpResponseMessage> PostRefreshAsync(HttpClient client, string refreshToken) =>
        client.PostAsJsonAsync(AuthTestData.RefreshUrl, new { refreshToken });

    [Fact]
    public async Task US3_AC1_Logout_Returns204AndRevokesToken()
    {
        var connectionString = await mySql.CreateDatabaseAsync(migrate: true);
        await using var factory = new ApiFactory(connectionString);
        var (client, session) = await factory.CreateAuthenticatedClientAsync();

        var response = await PostLogoutAsync(client, session.RefreshToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var db = MySqlContainerFixture.CreateDbContext(connectionString);
        var token = await db.RefreshTokens.SingleAsync(t => t.UserId == session.User.Id);
        Assert.NotNull(token.RevokedAt);
    }

    [Fact]
    public async Task US3_AC2_RefreshAfterLogout_Returns401()
    {
        await using var factory = new ApiFactory(await mySql.CreateDatabaseAsync(migrate: true));
        var (client, session) = await factory.CreateAuthenticatedClientAsync();
        await PostLogoutAsync(client, session.RefreshToken);

        Assert.Equal(HttpStatusCode.Unauthorized, (await PostRefreshAsync(client, session.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task US3_AC3_AccessTokenRejected15MinutesAfterLogout()
    {
        await using var factory = new ApiFactory(await mySql.CreateDatabaseAsync(migrate: true));
        var (client, session) = await factory.CreateAuthenticatedClientAsync();
        await PostLogoutAsync(client, session.RefreshToken);

        // 15 min of validity + 30 s of clock skew (research R2)
        factory.Clock.Advance(TimeSpan.FromMinutes(15).Add(TimeSpan.FromSeconds(31)));

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(AuthTestData.MeUrl)).StatusCode);
    }

    [Fact]
    public async Task Logout_Twice_Returns204()
    {
        await using var factory = new ApiFactory(await mySql.CreateDatabaseAsync(migrate: true));
        var (client, session) = await factory.CreateAuthenticatedClientAsync();

        await PostLogoutAsync(client, session.RefreshToken);

        Assert.Equal(HttpStatusCode.NoContent, (await PostLogoutAsync(client, session.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Logout_OtherUsersToken_Returns204WithoutRevoking()
    {
        await using var factory = new ApiFactory(await mySql.CreateDatabaseAsync(migrate: true));
        var (intruder, _) = await factory.CreateAuthenticatedClientAsync();
        var (owner, ownerSession) = await factory.CreateAuthenticatedClientAsync();

        Assert.Equal(HttpStatusCode.NoContent, (await PostLogoutAsync(intruder, ownerSession.RefreshToken)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await PostRefreshAsync(owner, ownerSession.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Assumption_LogoutOnlyRevokesCurrentDevice()
    {
        await using var factory = new ApiFactory(await mySql.CreateDatabaseAsync(migrate: true));
        var email = AuthTestData.NewEmail();
        var (phone, phoneSession) = await factory.CreateAuthenticatedClientAsync(email);
        var tablet = await AuthTestData.LoginAsync(factory.CreateClient(), email);

        await PostLogoutAsync(phone, phoneSession.RefreshToken);

        Assert.Equal(HttpStatusCode.OK, (await PostRefreshAsync(factory.CreateClient(), tablet.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutToken_Returns401()
    {
        await using var factory = new ApiFactory(await mySql.CreateDatabaseAsync(migrate: true));

        Assert.Equal(HttpStatusCode.Unauthorized, (await PostLogoutAsync(factory.CreateClient(), "x")).StatusCode);
    }
}
