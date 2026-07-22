using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities.Chat;

namespace Rafiq.Infrastructure.Persistence.Configurations;

public class MessageReactionConfiguration : IEntityTypeConfiguration<MessageReaction>
{
    public void Configure(EntityTypeBuilder<MessageReaction> builder)
    {
        builder.ToTable("MessageReactions");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.UserId).IsRequired();

        builder.Property(r => r.ReactionType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(r => r.Feedback)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2(7)");

        builder.HasOne(r => r.AiMessage)
            .WithMany()
            .HasForeignKey(r => r.AiMessageId)
            .OnDelete(DeleteBehavior.Cascade);

        // One reaction type per user per message
        builder.HasIndex(r => new { r.AiMessageId, r.UserId, r.ReactionType })
            .IsUnique();
    }
}
