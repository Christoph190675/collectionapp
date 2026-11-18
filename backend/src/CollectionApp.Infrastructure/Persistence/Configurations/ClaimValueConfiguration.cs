using CollectionApp.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollectionApp.Infrastructure.Persistence.Configurations
{
    public class ClaimValueConfiguration : IEntityTypeConfiguration<ClaimValue>
    {
        public void Configure(EntityTypeBuilder<ClaimValue> builder)
        {
            builder.ToTable("claimValues");

            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(c => c.Name).HasColumnName("name").HasColumnType("varchar(50)").IsRequired();
            builder.Property(c => c.Description).HasColumnName("description").HasColumnType("varchar(255)").IsRequired();

            builder.HasIndex(c => c.Name).IsUnique().HasDatabaseName("IX_claimValues_name");

            // Reference data, versioned in the migration (research R9)
            builder.HasData(
                new ClaimValue { Id = ClaimValue.DefaultClaimId, Name = ClaimValue.DefaultClaimName, Description = "Droit de base de tout utilisateur" },
                new ClaimValue { Id = ClaimValue.LogReadId, Name = ClaimValue.LogReadName, Description = "Lecture des journaux" });
        }
    }
}
