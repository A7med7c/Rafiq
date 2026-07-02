using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Infrastructure.Persistence.Configurations
{
    public sealed class LabReportConfiguration
     : IEntityTypeConfiguration<LabReport>
    {
        public void Configure(EntityTypeBuilder<LabReport> builder)
        {
            builder.ToTable("LabReports");

            builder.Property(x => x.LabName)
                .HasMaxLength(200);

            builder.HasMany(x => x.Results)
                .WithOne(x => x.LabReport)
                .HasForeignKey(x => x.LabReportId);
        }
    }
}