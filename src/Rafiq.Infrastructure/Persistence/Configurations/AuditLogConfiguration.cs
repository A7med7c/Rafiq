using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities;

namespace Rafiq.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.EntityType).IsRequired().HasMaxLength(100);
        builder.Property(x => x.IpAddress).HasMaxLength(45);
        builder.HasIndex(x => x.ActorUserId);
        builder.HasIndex(x => x.PatientProfileId);
        builder.HasIndex(x => x.Timestamp).IsDescending();
        builder.HasIndex(x => x.Action);
        builder.HasIndex(x => new { x.EntityType, x.EntityId });

        builder.HasOne(x => x.PatientProfile).WithMany().HasForeignKey(x => x.PatientProfileId).OnDelete(DeleteBehavior.Restrict);
    }
}
