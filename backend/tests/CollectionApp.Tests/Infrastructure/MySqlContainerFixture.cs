using CollectionApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Testcontainers.MySql;

namespace CollectionApp.Tests.Infrastructure;

/// <summary>
/// Starts a single MySQL container shared by every test of the "MySql" collection.
/// Each test creates its own database so tests stay independent.
/// </summary>
public sealed class MySqlContainerFixture : IAsyncLifetime
{
    /// <summary>Nothing listens on port 1: the connection is refused immediately.</summary>
    public const string UnreachableConnectionString =
        "Server=127.0.0.1;Port=1;Database=x;User=x;Password=x;Connection Timeout=2";

    private readonly MySqlContainer _container = new MySqlBuilder("mysql:8.4")
        .WithUsername("root")
        .WithPassword(Guid.NewGuid().ToString("N"))
        .Build();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    /// <summary>
    /// Create a new empty database, optionally migrated (by the test, never by the API).
    /// </summary>
    /// <returns>The connection string of the new database</returns>
    public async Task<string> CreateDatabaseAsync(bool migrate)
    {
        var databaseName = $"test_{Guid.NewGuid():N}";

        await using (var connection = new MySqlConnection(_container.GetConnectionString()))
        {
            await connection.OpenAsync();
            await using var command = new MySqlCommand($"CREATE DATABASE `{databaseName}`", connection);
            await command.ExecuteNonQueryAsync();
        }

        var connectionString = new MySqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Database = databaseName,
            ConnectionTimeout = 2,
        }.ConnectionString;

        if (migrate)
        {
            await using var dbContext = CreateDbContext(connectionString);
            await dbContext.Database.MigrateAsync();
        }

        return connectionString;
    }

    public static CollectionAppDbContext CreateDbContext(string connectionString) =>
        new(new DbContextOptionsBuilder<CollectionAppDbContext>().UseMySQL(connectionString).Options);
}

[CollectionDefinition("MySql")]
public sealed class MySqlCollection : ICollectionFixture<MySqlContainerFixture>
{
}
