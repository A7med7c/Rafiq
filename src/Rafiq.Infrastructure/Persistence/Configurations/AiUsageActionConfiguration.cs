using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities.Ai;

namespace Rafiq.Infrastructure.Persistence.Configurations;

public sealed class AiUsageActionConfiguration : IEntityTypeConfiguration<AiUsageAction>
{
    public void Configure(EntityTypeBuilder<AiUsageAction> builder)
    {
        builder.ToTable("AiUsageActions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TargetUserId)
            .IsRequired();

        builder.Property(x => x.AdminId)
            .IsRequired();

        builder.Property(x => x.AdminName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.ActionType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2(7)");

        builder.HasIndex(x => x.TargetUserId);
        builder.HasIndex(x => x.CreatedAt);
    }
}
