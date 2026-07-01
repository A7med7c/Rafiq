using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities;

namespace Rafiq.Infrastructure.Persistence.Configurations;

public sealed class PatientProfileConfiguration : IEntityTypeConfiguration<PatientProfile>
{
    public void Configure(EntityTypeBuilder<PatientProfile> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FullName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.DateOfBirth).IsRequired();
        builder.Property(x => x.Gender).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.BloodType).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Allergies).HasMaxLength(2000);
        builder.Property(x => x.ChronicConditions).HasMaxLength(2000);
        builder.Property(x => x.EmergencyContactName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.EmergencyContactPhone).IsRequired().HasMaxLength(20);

        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasFilter("[UserId] IS NOT NULL AND [IsDeleted] = 0");
        builder.HasIndex(x => x.FullName);
    }
}
