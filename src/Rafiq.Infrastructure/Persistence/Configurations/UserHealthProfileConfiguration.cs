using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities.User;
using Rafiq.Infrastructure.Persistence.Identity;

namespace Rafiq.Infrastructure.Persistence.Configurations;

public sealed class UserHealthProfileConfiguration
    : IEntityTypeConfiguration<UserHealthProfile>
{
    public void Configure(EntityTypeBuilder<UserHealthProfile> builder)
    {
        builder.ToTable("UserHealthProfiles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Gender)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.BloodType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Height)
            .HasPrecision(5, 2);

        builder.Property(x => x.Weight)
            .HasPrecision(5, 2);

        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<UserHealthProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // A registered user can have at most one Self Profile.
        // Multiple Managed Profiles (UserId is null) are allowed.
        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasFilter("[UserId] IS NOT NULL");
    }
}