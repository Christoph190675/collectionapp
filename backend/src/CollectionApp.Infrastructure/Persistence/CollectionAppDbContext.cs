using CollectionApp.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CollectionApp.Infrastructure.Persistence
{
    /// <summary>
    /// EF Core context of the application. Entity mappings live in Persistence/Configurations.
    /// </summary>
    public class CollectionAppDbContext : DbContext
    {
        public CollectionAppDbContext(DbContextOptions<CollectionAppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<ClaimValue> ClaimValues => Set<ClaimValue>();
        public DbSet<UserClaim> UserClaims => Set<UserClaim>();
        public DbSet<Login> Logins => Set<Login>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(CollectionAppDbContext).Assembly);
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            // Every date is stored in UTC: give it back as UTC (MySQL datetime has no time zone)
            configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
            configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
        }

        private sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
            value => value,
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}
