using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Infrastructure.Persistence.Configurations;

public sealed class DocumentUploadSessionConfiguration : IEntityTypeConfiguration<DocumentUploadSession>
{
    public void Configure(EntityTypeBuilder<DocumentUploadSession> builder)
    {
        builder.ToTable("DocumentUploadSessions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ImageUrl)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.FileHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(x => x.ImageUrl).IsUnique();
        builder.HasIndex(x => x.ExpiresAt);
    }
}
