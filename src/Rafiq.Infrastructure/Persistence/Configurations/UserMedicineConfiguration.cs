using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Infrastructure.Persistence.Configurations;

public sealed class UserMedicineConfiguration : IEntityTypeConfiguration<UserMedicine>
{
    public void Configure(EntityTypeBuilder<UserMedicine> builder)
    {
        builder.ToTable("UserMedicines");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MedicineName)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.Dosage)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Frequency)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Duration)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.ImagePath)
            .HasMaxLength(500);

        builder.Property(x => x.Source)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Relationship with UserHealthProfile
        builder.HasOne(x => x.UserHealthProfile)
            .WithMany()
            .HasForeignKey(x => x.UserHealthProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserHealthProfileId);
        builder.HasIndex(x => x.CreatedAt);
    }
}
