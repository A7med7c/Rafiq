using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Infrastructure.Persistence.Configurations;

public sealed class PrescriptionMedicineConfiguration : IEntityTypeConfiguration<PrescriptionMedicine>
{
    public void Configure(EntityTypeBuilder<PrescriptionMedicine> builder)
    {
        builder.ToTable("PrescriptionMedicines");

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

        builder.HasIndex(x => x.PrescriptionId);
    }
}
