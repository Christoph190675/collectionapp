using System.Net.Http.Headers;
using CollectionApp.Api;
using CollectionApp.Tests.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

namespace CollectionApp.Tests.Infrastructure;

/// <summary>
/// Hosts the API in memory, in the "Testing" environment: neither appsettings.Development.json nor
/// user-secrets are loaded, so the connection string, CORS origins and JWT key come only from this factory.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public static readonly string[] DevOrigins = ["http://localhost:8100", "https://localhost", "capacitor://localhost"];

    /// <summary>Fixed test key (>= 32 bytes), never used outside the tests</summary>
    public const string TestSigningKey = "test-signing-key-for-integration-tests-only-0123456789";

    private readonly string _connectionString;
    private readonly string[] _allowedOrigins;
    private readonly bool _swagger;
    private readonly bool _rateLimiting;

    /// <param name="rateLimiting">
    /// Off by default: test classes sending more than 20 POST /api/auth/* per minute would get 429 (R12)
    /// </param>
    public ApiFactory(string connectionString, string[]? allowedOrigins = null, bool swagger = false, bool rateLimiting = false)
    {
        _connectionString = connectionString;
        _allowedOrigins = allowedOrigins ?? DevOrigins;
        _swagger = swagger;
        _rateLimiting = rateLimiting;
    }

    public FakeLogCollector Logs => Services.GetRequiredService<FakeLogCollector>();

    /// <summary>
    /// Clock of the API: advance it to test the lock (15 min) and token expiry without waiting
    /// </summary>
    public FakeTimeProvider Clock { get; } = new(DateTimeOffset.UtcNow);

    /// <summary>
    /// Register then log a new user in, and return a client sending its access token
    /// </summary>
    public async Task<(HttpClient Client, AuthSession Session)> CreateAuthenticatedClientAsync(
        string? email = null, string password = AuthTestData.ValidPassword)
    {
        var client = CreateClient();
        email ??= AuthTestData.NewEmail();
        await AuthTestData.RegisterAsync(client, email, password);
        var session = await AuthTestData.LoginAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return (client, session);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:CollectionApp"] = _connectionString,
                ["Jwt:SigningKey"] = TestSigningKey,
                ["IpRateLimiting:EnableEndpointRateLimiting"] = _rateLimiting ? "true" : "false",
            };
            if (_swagger)
            {
                settings["swagger"] = "true";
            }
            for (var i = 0; i < _allowedOrigins.Length; i++)
            {
                settings[$"Cors:AllowedOrigins:{i}"] = _allowedOrigins[i];
            }
            configuration.AddInMemoryCollection(settings);
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });

        builder.ConfigureLogging(logging => logging.AddFakeLogging());
    }
}
