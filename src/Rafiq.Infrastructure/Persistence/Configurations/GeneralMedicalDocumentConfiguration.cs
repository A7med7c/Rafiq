using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Infrastructure.Persistence.Identity;

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

        builder.HasOne<ApplicationUser>()
            .WithMany(x => x.GeneralDocuments)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserId);
    }
}