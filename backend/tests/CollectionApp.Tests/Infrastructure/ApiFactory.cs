using CollectionApp.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace CollectionApp.Tests.Infrastructure;

/// <summary>
/// Hosts the API in memory, in the "Testing" environment: neither appsettings.Development.json nor
/// user-secrets are loaded, so the connection string and CORS origins come only from this factory.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public static readonly string[] DevOrigins = ["http://localhost:8100", "https://localhost", "capacitor://localhost"];

    private readonly string _connectionString;
    private readonly string[] _allowedOrigins;
    private readonly bool _swagger;

    public ApiFactory(string connectionString, string[]? allowedOrigins = null, bool swagger = false)
    {
        _connectionString = connectionString;
        _allowedOrigins = allowedOrigins ?? DevOrigins;
        _swagger = swagger;
    }

    public FakeLogCollector Logs => Services.GetRequiredService<FakeLogCollector>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:CollectionApp"] = _connectionString,
            };
            if (_swagger)
            {
                settings["swagger"] = "true";
                settings["EndPoints:AuthenticationAPI"] = "http://localhost";
            }
            for (var i = 0; i < _allowedOrigins.Length; i++)
            {
                settings[$"Cors:AllowedOrigins:{i}"] = _allowedOrigins[i];
            }
            configuration.AddInMemoryCollection(settings);
        });

        builder.ConfigureLogging(logging => logging.AddFakeLogging());
    }
}
