using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Infrastructure.Persistence.Configurations;

public sealed class MedicalReportConfiguration
    : IEntityTypeConfiguration<MedicalReport>
{
    public void Configure(EntityTypeBuilder<MedicalReport> builder)
    {
        builder.ToTable("MedicalReports");

        builder.Property(x => x.DoctorName)
            .HasMaxLength(150);

        builder.Property(x => x.ReportTitle)
            .HasMaxLength(200);
    }
}