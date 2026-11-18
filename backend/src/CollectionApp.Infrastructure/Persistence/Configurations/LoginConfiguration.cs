using CollectionApp.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollectionApp.Infrastructure.Persistence.Configurations
{
    public class LoginConfiguration : IEntityTypeConfiguration<Login>
    {
        public void Configure(EntityTypeBuilder<Login> builder)
        {
            builder.ToTable("logins");

            builder.HasKey(l => l.Id);
            builder.Property(l => l.Id).HasColumnName("id").ValueGeneratedOnAdd();
            builder.Property(l => l.UserId).HasColumnName("userId").IsRequired();
            builder.Property(l => l.DateLogin).HasColumnName("dateLogin").HasColumnType("datetime(6)").IsRequired();
            builder.Property(l => l.NbrTries).HasColumnName("nbrTries").IsRequired().HasDefaultValue(0);

            // One login state per user (1 : 0..1)
            builder.HasOne(l => l.User)
                .WithOne(u => u.Login)
                .HasForeignKey<Login>(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(l => l.UserId).IsUnique().HasDatabaseName("IX_logins_userId");
        }
    }
}
