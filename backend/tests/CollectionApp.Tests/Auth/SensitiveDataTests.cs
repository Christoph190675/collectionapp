using System.Net.Http.Json;
using CollectionApp.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CollectionApp.Tests.Auth;

/// <summary>
/// No password nor token readable in clear, neither in the logs nor in the database (SC-004, research R3).
/// </summary>
[Collection("MySql")]
public class SensitiveDataTests(MySqlContainerFixture mySql)
{
    [Fact]
    public async Task SC004_NoPasswordNorTokenInLogsOrDatabase()
    {
        var connectionString = await mySql.CreateDatabaseAsync(migrate: true);
        await using var factory = new ApiFactory(connectionString);
        var email = AuthTestData.NewEmail();
        var (client, session) = await factory.CreateAuthenticatedClientAsync(email);
        await AuthTestData.PostLoginAsync(factory.CreateClient(), email, "Wrong2026");
        var renewed = (await (await client.PostAsJsonAsync(AuthTestData.RefreshUrl, new { refreshToken = session.RefreshToken }))
            .Content.ReadFromJsonAsync<AuthSession>(AuthTestData.Json))!;
        await client.PostAsJsonAsync(AuthTestData.LogoutUrl, new { refreshToken = renewed.RefreshToken });

        var secrets = new[] { AuthTestData.ValidPassword, "Wrong2026", session.AccessToken, session.RefreshToken, renewed.RefreshToken };
        var logs = string.Join("\n", factory.Logs.GetSnapshot().Select(l => l.Message + " " + l.Exception));
        foreach (var secret in secrets)
        {
            Assert.DoesNotContain(secret, logs);
        }

        await using var db = MySqlContainerFixture.CreateDbContext(connectionString);
        var hashes = await db.RefreshTokens.Select(t => t.TokenHash).ToListAsync();
        Assert.Equal(2, hashes.Count);
        Assert.All(hashes, hash => Assert.Matches("^[0-9a-f]{64}$", hash));
        Assert.DoesNotContain(session.RefreshToken, hashes);
        Assert.DoesNotContain(renewed.RefreshToken, hashes);
    }
}
