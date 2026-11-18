using System.Text;
using CollectionApp.Application.Auth;
using CollectionApp.Application.Health;
using CollectionApp.Domain.Users;
using CollectionApp.Infrastructure.Persistence;
using CollectionApp.Infrastructure.Persistence.Repositories;
using CollectionApp.Infrastructure.Security;
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

            // Users & authentication
            var authOptions = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
            if (Encoding.UTF8.GetByteCount(authOptions.SigningKey ?? "") < AuthOptions.MinSigningKeyBytes)
            {
                throw new InvalidOperationException(
                    $"'{AuthOptions.SectionName}:SigningKey' is missing or shorter than {AuthOptions.MinSigningKeyBytes} bytes. " +
                    "Set it with 'dotnet user-secrets set \"Jwt:SigningKey\" \"...\"'.");
            }
            services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
            services.AddSingleton<ITokenService, JwtTokenService>();

            return services;
        }
    }
}
