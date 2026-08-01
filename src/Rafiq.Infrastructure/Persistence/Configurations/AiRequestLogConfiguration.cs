using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities.Ai;

namespace Rafiq.Infrastructure.Persistence.Configurations;

public sealed class AiRequestLogConfiguration : IEntityTypeConfiguration<AiRequestLog>
{
    public void Configure(EntityTypeBuilder<AiRequestLog> builder)
    {
        builder.ToTable("AiRequestLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Feature)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.ModelName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Success)
            .IsRequired();

        builder.Property(x => x.DurationMs)
            .IsRequired();

        builder.Property(x => x.ErrorType)
            .HasMaxLength(200);

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2(7)");

        // No navigation properties / FKs — loose references survive user deletion
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => new { x.Feature, x.CreatedAt });
        builder.HasIndex(x => x.UserId);
    }
}
