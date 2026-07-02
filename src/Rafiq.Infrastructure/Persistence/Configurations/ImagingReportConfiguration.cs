using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Infrastructure.Persistence.Configurations
{
    public sealed class ImagingReportConfiguration
    : IEntityTypeConfiguration<ImagingReport>
    {
        public void Configure(EntityTypeBuilder<ImagingReport> builder)
        {
            builder.ToTable("ImagingReports");

            builder.Property(x => x.ImagingType)
                .HasMaxLength(100);

            builder.Property(x => x.BodyPart)
                .HasMaxLength(100);
        }
    }

}