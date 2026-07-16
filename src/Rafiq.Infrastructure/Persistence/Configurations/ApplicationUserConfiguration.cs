using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Infrastructure.Persistence.Identity;

namespace Rafiq.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2(7)");
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2(7)");
        builder.Property(x => x.DeletedAt).HasColumnType("datetime2(7)");
        builder.Property(x => x.ProfileImageUrl).HasMaxLength(500);

        builder.HasIndex(x => x.PhoneNumber)
            .IsUnique()
            .HasFilter("[PhoneNumber] IS NOT NULL AND [IsDeleted] = 0");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
