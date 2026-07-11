using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities.Chat;

namespace Rafiq.Infrastructure.Persistence.Configurations;

public class AiMessageConfiguration : IEntityTypeConfiguration<AiMessage>
{
    public void Configure(EntityTypeBuilder<AiMessage> builder)
    {
        builder.ToTable("AiMessages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Content)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(m => m.Role)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(m => m.SequenceNumber)
            .IsRequired();

        // An AiMessage belongs to an AiConversation
        builder.HasOne(m => m.AiConversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.AiConversationId)
            .OnDelete(DeleteBehavior.Cascade); // If conversation is specifically deleted (hard delete or cascading), clean up messages

        builder.HasIndex(m => new { m.AiConversationId, m.SequenceNumber })
            .IsUnique();
    }
}
