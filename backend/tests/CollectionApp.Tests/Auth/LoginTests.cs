using System.Net;
using CollectionApp.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CollectionApp.Tests.Auth;

/// <summary>
/// POST /api/auth/login (User Story 2).
/// </summary>
[Collection("MySql")]
public class LoginTests(MySqlContainerFixture mySql)
{
    [Fact]
    public async Task US2_AC1_ValidCredentials_Returns200WithSession()
    {
        var connectionString = await mySql.CreateDatabaseAsync(migrate: true);
        await using var factory = new ApiFactory(connectionString);
        var client = factory.CreateClient();
        var email = AuthTestData.NewEmail();
        await AuthTestData.RegisterAsync(client, email);
        var now = factory.Clock.GetUtcNow().UtcDateTime;

        var session = await AuthTestData.LoginAsync(client, email.ToUpperInvariant());

        Assert.False(string.IsNullOrEmpty(session.AccessToken));
        Assert.False(string.IsNullOrEmpty(session.RefreshToken));
        Assert.Equal(now.AddMinutes(15), session.AccessTokenExpiresAt, TimeSpan.FromSeconds(1));
        Assert.Equal(now.AddDays(7), session.RefreshTokenExpiresAt, TimeSpan.FromSeconds(1));
        Assert.Equal(email, session.User.Email);
    }

    [Fact]
    public async Task US2_AC2_WrongPassword_Returns401GenericMessage()
    {
        var connectionString = await mySql.CreateDatabaseAsync(migrate: true);
        await using var factory = new ApiFactory(connectionString);
        var client = factory.CreateClient();
        var email = AuthTestData.NewEmail();
        await AuthTestData.RegisterAsync(client, email);

        var response = await AuthTestData.PostLoginAsync(client, email, "Wrong2026");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await AuthTestData.ReadErrorAsync(response);
        Assert.Equal("E-mail ou mot de passe incorrect", error.GetProperty("message").GetString());
    }

    [Fact]
    public async Task US2_AC3_UnknownEmail_ReturnsSameResponseAsWrongPassword()
    {
        var connectionString = await mySql.CreateDatabaseAsync(migrate: true);
        await using var factory = new ApiFactory(connectionString);
        var client = factory.CreateClient();
        var email = AuthTestData.NewEmail();
        await AuthTestData.RegisterAsync(client, email);

        var wrongPassword = await AuthTestData.PostLoginAsync(client, email, "Wrong2026");
        var unknownEmail = await AuthTestData.PostLoginAsync(client, AuthTestData.NewEmail(), "Wrong2026");

        Assert.Equal(wrongPassword.StatusCode, unknownEmail.StatusCode);
        var expected = await AuthTestData.ReadErrorAsync(wrongPassword);
        var actual = await AuthTestData.ReadErrorAsync(unknownEmail);
        Assert.Equal(expected.GetProperty("code").GetString(), actual.GetProperty("code").GetString());
        Assert.Equal(expected.GetProperty("message").GetString(), actual.GetProperty("message").GetString());
        Assert.Equal(
            expected.EnumerateObject().Select(p => p.Name).Order(),
            actual.EnumerateObject().Select(p => p.Name).Order());
    }

    [Fact]
    public async Task US2_AC6_SuccessAfterFailures_ResetsCounter()
    {
        var connectionString = await mySql.CreateDatabaseAsync(migrate: true);
        await using var factory = new ApiFactory(connectionString);
        var client = factory.CreateClient();
        var email = AuthTestData.NewEmail();
        var user = await AuthTestData.RegisterAsync(client, email);
        for (var i = 0; i < 3; i++)
        {
            await AuthTestData.PostLoginAsync(client, email, "Wrong2026");
        }

        await AuthTestData.LoginAsync(client, email);

        await using var db = MySqlContainerFixture.CreateDbContext(connectionString);
        Assert.Equal(0, await db.Logins.Where(l => l.UserId == user.Id).Select(l => l.NbrTries).SingleAsync());
    }

    [Fact]
    public async Task FR009_Failure_UpdatesDateLoginAndCounter()
    {
        var connectionString = await mySql.CreateDatabaseAsync(migrate: true);
        await using var factory = new ApiFactory(connectionString);
        var client = factory.CreateClient();
        var email = AuthTestData.NewEmail();
        var user = await AuthTestData.RegisterAsync(client, email);

        await AuthTestData.PostLoginAsync(client, email, "Wrong2026");
        factory.Clock.Advance(TimeSpan.FromMinutes(1));
        await AuthTestData.PostLoginAsync(client, email, "Wrong2026");

        await using var db = MySqlContainerFixture.CreateDbContext(connectionString);
        var login = Assert.Single(await db.Logins.Where(l => l.UserId == user.Id).ToListAsync());
        Assert.Equal(2, login.NbrTries);
        Assert.Equal(factory.Clock.GetUtcNow().UtcDateTime, login.DateLogin, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task MissingFields_Returns400WithFieldErrors()
    {
        var connectionString = await mySql.CreateDatabaseAsync(migrate: true);
        await using var factory = new ApiFactory(connectionString);

        var response = await AuthTestData.PostLoginAsync(factory.CreateClient(), "", "");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await AuthTestData.ReadErrorAsync(response);
        Assert.Equal(["email", "password"], error.GetProperty("fields").EnumerateArray()
            .Select(f => f.GetProperty("field").GetString()).Distinct().Order());
    }
}
