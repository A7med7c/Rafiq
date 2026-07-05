using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Infrastructure.Persistence.Identity;

namespace Rafiq.Infrastructure.Persistence.Configurations;

public sealed class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.ToTable("Prescriptions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DoctorName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.PatientName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.PrescriptionDate)
            .IsRequired();

        builder.Property(x => x.ImagePath)
            .HasMaxLength(500)
            .IsRequired();

        // Relationship with Identity User
        builder.HasOne<ApplicationUser>()
            .WithMany(u => u.Prescriptions)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship with PrescriptionMedicines
        builder.HasMany(x => x.Medicines)
            .WithOne(x => x.Prescription)
            .HasForeignKey(x => x.PrescriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.CreatedAt);
    }
}