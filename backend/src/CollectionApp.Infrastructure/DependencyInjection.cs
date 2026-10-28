using CollectionApp.Application.Health;
using CollectionApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CollectionApp.Infrastructure
{
    public static class DependencyInjection
    {
        public const string CONNECTION_STRING_NAME = "CollectionApp";

        /// <summary>
        /// Add infrastructure module (persistence and external resources).
        /// Migrations are never applied here: they are run explicitly with `dotnet ef database update`.
        /// </summary>
        /// <param name="services">An IServiceCollection</param>
        /// <param name="configuration">The application configuration</param>
        /// <returns>An IServiceCollection</returns>
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString(CONNECTION_STRING_NAME);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"Connection string 'ConnectionStrings:{CONNECTION_STRING_NAME}' is missing. " +
                    "Set it with 'dotnet user-secrets set \"ConnectionStrings:CollectionApp\" \"...\"'.");
            }

            services.AddDbContext<CollectionAppDbContext>(options => options.UseMySQL(connectionString));

            services.AddScoped<IDatabaseHealthChecker, DatabaseHealthChecker>();

            return services;
        }
    }
}
