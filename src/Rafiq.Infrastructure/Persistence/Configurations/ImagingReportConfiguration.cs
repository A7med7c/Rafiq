using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Infrastructure.Persistence.Identity;

namespace Rafiq.Infrastructure.Persistence.Configurations
{
    public sealed class ImagingReportConfiguration
    : IEntityTypeConfiguration<ImagingReport>
    {
        public void Configure(EntityTypeBuilder<ImagingReport> builder)
        {
            builder.ToTable("ImagingReports");

            builder.HasKey(x => x.ReportId);

            builder.Property(x => x.ReportId)
                .ValueGeneratedNever();

            builder.Property(x => x.ImagingType)
                .IsRequired();

            builder.Property(x => x.BodyPart)
                .IsRequired();

            builder.Property(x => x.Findings)
                .IsRequired();

            builder.Property(x => x.Impression)
                .IsRequired();

            builder.Property(x => x.AiSummary)
                .IsRequired();

            builder.Property(x => x.DoctorName)
                .IsRequired();

            builder.Property(x => x.ReportImagePath)
                .IsRequired()
                .HasMaxLength(500);

            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

}
