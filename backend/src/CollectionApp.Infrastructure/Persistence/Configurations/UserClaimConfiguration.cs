using CollectionApp.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollectionApp.Infrastructure.Persistence.Configurations
{
    public class UserClaimConfiguration : IEntityTypeConfiguration<UserClaim>
    {
        public void Configure(EntityTypeBuilder<UserClaim> builder)
        {
            builder.ToTable("userClaims");

            builder.HasKey(uc => new { uc.UserId, uc.ClaimId });
            builder.Property(uc => uc.UserId).HasColumnName("userId");
            builder.Property(uc => uc.ClaimId).HasColumnName("claimId");

            builder.HasOne(uc => uc.User)
                .WithMany(u => u.UserClaims)
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(uc => uc.Claim)
                .WithMany(c => c.UserClaims)
                .HasForeignKey(uc => uc.ClaimId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(uc => uc.ClaimId).HasDatabaseName("IX_userClaims_claimId");
        }
    }
}
