using System.Diagnostics;
using System.Net;
using System.Text.Json;
using CollectionApp.Infrastructure.Persistence;
using CollectionApp.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CollectionApp.Tests.Health;

[Collection("MySql")]
public class HealthEndpointTests(MySqlContainerFixture mySql)
{
    private const string HealthUrl = "/api/health";

    /// <summary>
    /// Asserts the body matches the contract (exactly api, database, checkedAt) and returns it.
    /// </summary>
    internal static async Task<JsonElement> ReadHealthBodyAsync(HttpResponseMessage response)
    {
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(["api", "checkedAt", "database"], body.EnumerateObject().Select(p => p.Name).Order());
        Assert.Equal("ok", body.GetProperty("api").GetString());
        var checkedAt = body.GetProperty("checkedAt").GetString();
        Assert.NotNull(checkedAt);
        Assert.EndsWith("Z", checkedAt);
        Assert.True(DateTime.TryParse(checkedAt, out _));
        return body;
    }

    [Fact]
    public async Task US1_AC1_AllHealthy_Returns200WithOk()
    {
        var connectionString = await mySql.CreateDatabaseAsync(migrate: true);
        await using var factory = new ApiFactory(connectionString);

        var response = await factory.CreateClient().GetAsync(HealthUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadHealthBodyAsync(response);
        Assert.Equal("ok", body.GetProperty("database").GetString());
    }

    [Fact]
    public async Task US2_AC1_DatabaseStopped_Returns503WithDatabaseKo()
    {
        await using var factory = new ApiFactory(MySqlContainerFixture.UnreachableConnectionString);

        var response = await factory.CreateClient().GetAsync(HealthUrl);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Exception", raw);
        Assert.DoesNotContain("127.0.0.1", raw);
        Assert.DoesNotContain("Port", raw, StringComparison.OrdinalIgnoreCase);
        var body = await ReadHealthBodyAsync(response);
        Assert.Equal("ko", body.GetProperty("database").GetString());
        Assert.Contains(factory.Logs.GetSnapshot(), log =>
            log.Level == LogLevel.Warning && log.Category == typeof(DatabaseHealthChecker).FullName);
    }

    [Fact]
    public async Task US5_AC1_BlankDatabase_Returns503WithDatabaseKo()
    {
        var connectionString = await mySql.CreateDatabaseAsync(migrate: false);
        await using var factory = new ApiFactory(connectionString);

        var response = await factory.CreateClient().GetAsync(HealthUrl);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await ReadHealthBodyAsync(response);
        Assert.Equal("ko", body.GetProperty("database").GetString());
        Assert.Contains(factory.Logs.GetSnapshot(), log =>
            log.Level == LogLevel.Warning && log.Message.Contains("InitialCreate"));
    }

    [Fact]
    public async Task FR014_ApiNeverAppliesMigrations()
    {
        var connectionString = await mySql.CreateDatabaseAsync(migrate: false);
        await using (var factory = new ApiFactory(connectionString))
        {
            var client = factory.CreateClient();
            await client.GetAsync(HealthUrl);
            await client.GetAsync(HealthUrl);
        }

        await using var dbContext = MySqlContainerFixture.CreateDbContext(connectionString);
        Assert.Contains(await dbContext.Database.GetPendingMigrationsAsync(), m => m.EndsWith("_InitialCreate"));
        Assert.Empty(await dbContext.Database.GetAppliedMigrationsAsync());
    }

    [SkippableFact]
    public async Task Edge_SlowDatabase_RespondsKoWithin3Seconds()
    {
        // Non-routable address: the TCP connection hangs instead of being refused
        await using var factory = new ApiFactory(
            "Server=10.255.255.1;Port=3306;Database=x;User=x;Password=x;Connection Timeout=30");
        var client = factory.CreateClient();
        await client.GetAsync("/swagger/index.html"); // warm up the host so only the check is timed

        var stopwatch = Stopwatch.StartNew();
        var response = await client.GetAsync(HealthUrl);
        stopwatch.Stop();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await ReadHealthBodyAsync(response);
        Assert.Equal("ko", body.GetProperty("database").GetString());
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(3), $"Health check took {stopwatch.Elapsed}");
        Skip.If(stopwatch.Elapsed < TimeSpan.FromSeconds(1.5),
            "adresse non routable rejetée immédiatement : le délai n'a pas pu être exercé");
    }
}
