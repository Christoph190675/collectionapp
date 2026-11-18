using System.Net;
using CollectionApp.Tests.Infrastructure;

namespace CollectionApp.Tests.Auth;

/// <summary>
/// Lock after 5 consecutive failures, for 15 minutes (FR-010, SC-003), with a simulated clock.
/// </summary>
[Collection("MySql")]
public class LockoutTests(MySqlContainerFixture mySql)
{
    private static async Task FailAsync(HttpClient client, string email, int times)
    {
        for (var i = 0; i < times; i++)
        {
            var response = await AuthTestData.PostLoginAsync(client, email, "Wrong2026");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    private async Task<(ApiFactory Factory, HttpClient Client, string Email)> CreateUserAsync()
    {
        var factory = new ApiFactory(await mySql.CreateDatabaseAsync(migrate: true));
        var client = factory.CreateClient();
        var email = AuthTestData.NewEmail();
        await AuthTestData.RegisterAsync(client, email);
        return (factory, client, email);
    }

    [Fact]
    public async Task US2_AC4_FiveFailures_ReturnsLockedEvenWithGoodPassword()
    {
        var (factory, client, email) = await CreateUserAsync();
        await using var disposeFactory = factory;
        await FailAsync(client, email, 5);

        var response = await AuthTestData.PostLoginAsync(client, email, AuthTestData.ValidPassword);

        Assert.Equal((HttpStatusCode)423, response.StatusCode);
        Assert.Equal(TimeSpan.FromMinutes(15), response.Headers.RetryAfter?.Delta);
        var error = await AuthTestData.ReadErrorAsync(response);
        Assert.StartsWith("Compte temporairement bloqué", error.GetProperty("message").GetString());
    }

    [Fact]
    public async Task US2_AC4_AttemptsDuringLock_DoNotExtendLock()
    {
        var (factory, client, email) = await CreateUserAsync();
        await using var disposeFactory = factory;
        await FailAsync(client, email, 5);

        factory.Clock.Advance(TimeSpan.FromMinutes(5));
        Assert.Equal((HttpStatusCode)423, (await AuthTestData.PostLoginAsync(client, email, "Wrong2026")).StatusCode);
        factory.Clock.Advance(TimeSpan.FromMinutes(5));
        var tenMinutes = await AuthTestData.PostLoginAsync(client, email, AuthTestData.ValidPassword);
        Assert.Equal((HttpStatusCode)423, tenMinutes.StatusCode);
        Assert.Equal(TimeSpan.FromMinutes(5), tenMinutes.Headers.RetryAfter?.Delta);

        factory.Clock.Advance(TimeSpan.FromMinutes(5));
        Assert.Equal(HttpStatusCode.OK, (await AuthTestData.PostLoginAsync(client, email, AuthTestData.ValidPassword)).StatusCode);
    }

    [Fact]
    public async Task US2_AC5_After15Minutes_LoginSucceeds()
    {
        var (factory, client, email) = await CreateUserAsync();
        await using var disposeFactory = factory;
        await FailAsync(client, email, 5);

        factory.Clock.Advance(TimeSpan.FromMinutes(15).Add(TimeSpan.FromSeconds(1)));

        Assert.Equal(HttpStatusCode.OK, (await AuthTestData.PostLoginAsync(client, email, AuthTestData.ValidPassword)).StatusCode);
    }

    [Fact]
    public async Task Assumption_AfterLockExpiry_CounterRestartsAtZero()
    {
        var (factory, client, email) = await CreateUserAsync();
        await using var disposeFactory = factory;
        await FailAsync(client, email, 5);
        factory.Clock.Advance(TimeSpan.FromMinutes(16));

        await FailAsync(client, email, 4);

        Assert.Equal(HttpStatusCode.OK, (await AuthTestData.PostLoginAsync(client, email, AuthTestData.ValidPassword)).StatusCode);
    }
}
