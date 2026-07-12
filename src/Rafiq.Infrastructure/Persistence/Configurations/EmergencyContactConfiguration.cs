using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities.User;
using Rafiq.Infrastructure.Persistence.Identity;

namespace Rafiq.Infrastructure.Persistence.Configurations;

public sealed class EmergencyContactConfiguration : IEntityTypeConfiguration<EmergencyContact>
{
    public void Configure(EntityTypeBuilder<EmergencyContact> builder)
    {
        builder.ToTable("EmergencyContacts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.PhoneNumber)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Relation)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany(u => u.EmergencyContacts)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserId);
    }
}
