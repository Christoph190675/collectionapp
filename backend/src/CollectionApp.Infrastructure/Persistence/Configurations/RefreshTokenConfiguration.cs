using CollectionApp.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollectionApp.Infrastructure.Persistence.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("refreshTokens");

            builder.HasKey(t => t.Id);
            builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedOnAdd();
            builder.Property(t => t.UserId).HasColumnName("userId").IsRequired();
            builder.Property(t => t.TokenHash).HasColumnName("tokenHash").HasColumnType("char(64)").IsRequired();
            builder.Property(t => t.CreatedAt).HasColumnName("createdAt").HasColumnType("datetime(6)").IsRequired();
            builder.Property(t => t.ExpiresAt).HasColumnName("expiresAt").HasColumnType("datetime(6)").IsRequired();
            builder.Property(t => t.RevokedAt).HasColumnName("revokedAt").HasColumnType("datetime(6)");

            builder.HasOne(t => t.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(t => t.UserId).HasDatabaseName("IX_refreshTokens_userId");
            builder.HasIndex(t => t.TokenHash).IsUnique().HasDatabaseName("IX_refreshTokens_tokenHash");
        }
    }
}
