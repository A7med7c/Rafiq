using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities.User;

namespace Rafiq.Infrastructure.Persistence.Configurations;

public sealed class HealthSummaryCacheConfiguration : IEntityTypeConfiguration<HealthSummaryCache>
{
    public void Configure(EntityTypeBuilder<HealthSummaryCache> builder)
    {
        builder.ToTable("HealthSummaryCaches");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Language)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.SummaryJson)
            .IsRequired();

        builder.HasIndex(x => new { x.UserHealthProfileId, x.Language })
            .IsUnique();

        builder.HasOne<UserHealthProfile>()
            .WithMany()
            .HasForeignKey(x => x.UserHealthProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
