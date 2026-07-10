using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Infrastructure.Persistence.Configurations;

public sealed class ImagingReportConfiguration : IEntityTypeConfiguration<ImagingReport>
{
    public void Configure(EntityTypeBuilder<ImagingReport> builder)
    {
        builder.ToTable("ImagingReports");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ImagingType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.BodyPart)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Findings)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(x => x.Impression)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(x => x.DoctorName)
            .HasMaxLength(200);

        builder.Property(x => x.ReportDate)
            .IsRequired();

        builder.Property(x => x.ImageUrl)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.OCRText)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        // Relationship with UserHealthProfile
        builder.HasOne(x => x.UserHealthProfile)
            .WithMany()
            .HasForeignKey(x => x.UserHealthProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserHealthProfileId);
        builder.HasIndex(x => x.CreatedAt);
    }
}