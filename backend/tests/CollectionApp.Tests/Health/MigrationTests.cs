using System.Net;
using CollectionApp.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CollectionApp.Tests.Health;

[Collection("MySql")]
public class MigrationTests(MySqlContainerFixture mySql)
{
    [Fact]
    public async Task US5_AC2_MigrateBlankDatabase_AppliesInitialCreate_AndHealthIsOk()
    {
        var connectionString = await mySql.CreateDatabaseAsync(migrate: false);
        await using (var dbContext = MySqlContainerFixture.CreateDbContext(connectionString))
        {
            await dbContext.Database.MigrateAsync();

            Assert.Contains(await dbContext.Database.GetAppliedMigrationsAsync(), m => m.EndsWith("_InitialCreate"));
        }

        await using var factory = new ApiFactory(connectionString);
        var response = await factory.CreateClient().GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await HealthEndpointTests.ReadHealthBodyAsync(response);
        Assert.Equal("ok", body.GetProperty("database").GetString());
    }

    [Fact]
    public async Task US5_AC3_MigrateTwice_IsIdempotent()
    {
        var connectionString = await mySql.CreateDatabaseAsync(migrate: true);
        await using var dbContext = MySqlContainerFixture.CreateDbContext(connectionString);
        var appliedBefore = (await dbContext.Database.GetAppliedMigrationsAsync()).ToList();

        var exception = await Record.ExceptionAsync(() => dbContext.Database.MigrateAsync());

        Assert.Null(exception);
        Assert.Equal(appliedBefore, await dbContext.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await dbContext.Database.GetPendingMigrationsAsync());
    }
}
