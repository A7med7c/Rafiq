using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities.User;
using Rafiq.Infrastructure.Persistence.Identity;

namespace Rafiq.Infrastructure.Persistence.Configurations;

public sealed class HealthProfileAccessConfiguration
    : IEntityTypeConfiguration<HealthProfileAccess>
{
    public void Configure(EntityTypeBuilder<HealthProfileAccess> builder)
    {
        builder.ToTable("HealthProfileAccesses");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Role)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Origin)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.StatusChangedAt)
            .IsRequired();

        builder.HasOne(x => x.UserHealthProfile)
            .WithMany()
            .HasForeignKey(x => x.UserHealthProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // Two FKs to ApplicationUser from the same entity: use Restrict so deleting a
        // user never cascades away access history, and to avoid multiple cascade paths.
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.GranteeUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.InvitedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.UserHealthProfileId);

        builder.HasIndex(x => x.GranteeUserId);

        // The same user must not have more than one Pending (1) or Active (2)
        // access record for the same health profile at the same time.
        builder.HasIndex(x => new
        {
            x.UserHealthProfileId,
            x.GranteeUserId
        })
        .IsUnique()
        .HasFilter("[Status] IN (1, 2)");
    }
}
