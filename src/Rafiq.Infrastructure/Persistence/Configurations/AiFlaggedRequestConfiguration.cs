using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities.Ai;

namespace Rafiq.Infrastructure.Persistence.Configurations;

public sealed class AiFlaggedRequestConfiguration : IEntityTypeConfiguration<AiFlaggedRequest>
{
    public void Configure(EntityTypeBuilder<AiFlaggedRequest> builder)
    {
        builder.ToTable("AiFlaggedRequests");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.RequestType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.UserRequest)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.AiResponse)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.Classification)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2(7)");

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
    }
}
