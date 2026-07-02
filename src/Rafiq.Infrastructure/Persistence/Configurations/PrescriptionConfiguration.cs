using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Infrastructure.Persistence.Configurations
{
    public sealed class PrescriptionConfiguration
        : IEntityTypeConfiguration<Prescription>
    {
        public void Configure(EntityTypeBuilder<Prescription> builder)
        {
            builder.ToTable("Prescriptions");

            builder.Property(x => x.DoctorName)
                .HasMaxLength(150);

            builder.HasMany(x => x.Medicines)
                .WithOne(x => x.Prescription)
                .HasForeignKey(x => x.PrescriptionId);
        }
    }
}