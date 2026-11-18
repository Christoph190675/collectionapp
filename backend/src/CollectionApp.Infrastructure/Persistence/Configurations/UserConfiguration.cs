using CollectionApp.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollectionApp.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public const string EmailIndexName = "IX_users_email";

        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");

            builder.HasKey(u => u.Id);
            builder.Property(u => u.Id).HasColumnName("id").ValueGeneratedOnAdd();
            builder.Property(u => u.LastName).HasColumnName("lastName").HasColumnType("varchar(100)").IsRequired();
            builder.Property(u => u.FirstName).HasColumnName("firstName").HasColumnType("varchar(100)").IsRequired();
            builder.Property(u => u.Email).HasColumnName("email").HasColumnType("varchar(254)").IsRequired();
            builder.Property(u => u.PasswordHash).HasColumnName("passwordHash").HasColumnType("varchar(255)").IsRequired();

            builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName(EmailIndexName);
        }
    }
}
