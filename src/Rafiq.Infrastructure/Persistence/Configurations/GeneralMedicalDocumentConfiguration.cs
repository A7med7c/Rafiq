using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class GeneralDocumentConfiguration
    : IEntityTypeConfiguration<GeneralDocument>
{
    public void Configure(EntityTypeBuilder<GeneralDocument> builder)
    {
        builder.ToTable("GeneralDocuments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(3000);

        builder.Property(x => x.AiSummary)
            .HasMaxLength(4000);

        builder.Property(x => x.ImagePath)
            .IsRequired();

        builder.Property(x => x.DocumentType)
            .HasMaxLength(100);

        builder.Property(x => x.DoctorName)
            .HasMaxLength(200);

        builder.Property(x => x.HospitalOrClinic)
            .HasMaxLength(200);

        builder.Property(x => x.DocumentDate)
            .HasMaxLength(50);

        builder.Property(x => x.OcrText)
            .HasMaxLength(10000);

        builder.Property(x => x.AnalysisStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.FailureReason)
            .HasMaxLength(500);

        builder.HasOne(x => x.UserHealthProfile)
            .WithMany()
            .HasForeignKey(x => x.UserHealthProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserHealthProfileId);

        // Used by the stuck-document recovery job
        builder.HasIndex(x => new { x.AnalysisStatus, x.UpdatedAt });
        
        builder.Property(x => x.FileHash)
            .HasMaxLength(64)
            .IsRequired(false);
            
        builder.HasIndex(x => new { x.UserHealthProfileId, x.FileHash });
    }
}