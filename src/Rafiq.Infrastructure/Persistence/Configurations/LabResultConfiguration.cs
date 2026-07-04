using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Infrastructure.Persistence.Configurations;

public sealed class LabResultConfiguration : IEntityTypeConfiguration<LabResult>
{
    public void Configure(EntityTypeBuilder<LabResult> builder)
    {
        builder.ToTable("LabResults");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TestName)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.Value)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Unit)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.NormalRange)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasMaxLength(50);   // H, L, High, Low, Positive, Negative

        builder.HasIndex(x => x.LabReportId);
    }
}
