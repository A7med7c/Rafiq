using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities;

namespace Rafiq.Infrastructure.Persistence.Configurations;

public sealed class AppReviewConfiguration : IEntityTypeConfiguration<AppReview>
{
    public void Configure(EntityTypeBuilder<AppReview> builder)
    {
        builder.ToTable("AppReviews");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Stars).IsRequired();
        builder.Property(x => x.Comment).HasColumnType("nvarchar(max)");
        builder.Property(x => x.IsVisible).IsRequired();

        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.Category).HasConversion<int>().IsRequired();

        builder.Property(x => x.AdminNotes).HasColumnType("nvarchar(max)");
        builder.Property(x => x.AdminReply).HasColumnType("nvarchar(max)");
        builder.Property(x => x.DeviceInfo).HasMaxLength(500);
        builder.Property(x => x.AppVersion).HasMaxLength(50);
        builder.Property(x => x.AppLanguage).HasMaxLength(10);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAt);
    }
}
