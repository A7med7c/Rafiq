using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities.Chat;
using Rafiq.Domain.Enums;

namespace Rafiq.Infrastructure.Persistence.Configurations;

public class AiConversationConfiguration : IEntityTypeConfiguration<AiConversation>
{
    public void Configure(EntityTypeBuilder<AiConversation> builder)
    {
        builder.ToTable("AiConversations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(c => c.Source)
            .HasConversion<int>()
            .HasDefaultValue(AiConversationSource.Chat)
            .IsRequired();

        // A conversation is owned by exactly one UserHealthProfile
        builder.HasOne(c => c.UserHealthProfile)
            .WithMany()
            .HasForeignKey(c => c.UserHealthProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        // Composite index to support sorting the conversation list by recent activity
        builder.HasIndex(c => new { c.UserId, c.LastMessageAt });

        builder.HasIndex(c => c.UserHealthProfileId);
    }
}
