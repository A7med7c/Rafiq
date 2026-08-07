using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Infrastructure.Persistence.Configurations;

public sealed class LabReportConfiguration : IEntityTypeConfiguration<LabReport>
{
    public void Configure(EntityTypeBuilder<LabReport> builder)
    {
        builder.ToTable("LabReports");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DoctorName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.LabName)
            .HasMaxLength(200)
            .IsRequired();

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

        // Relationship with LabResults
        builder.HasMany(x => x.Results)
            .WithOne(x => x.LabReport)
            .HasForeignKey(x => x.LabReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserHealthProfileId);
        builder.HasIndex(x => x.CreatedAt);
        
        builder.Property(x => x.FileHash)
            .HasMaxLength(64)
            .IsRequired(false);
            
        builder.HasIndex(x => new { x.UserHealthProfileId, x.FileHash });
    }
}