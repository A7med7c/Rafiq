using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities.Chat;
using Rafiq.Domain.Enums;

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

        builder.Property(m => m.Status)
            .IsRequired()
            .HasConversion<int>()
            // HasDefaultValue must receive the enum value; EF applies the conversion
            // and emits DEFAULT 2 in the migration SQL.
            .HasDefaultValue(AiMessageStatus.Completed);

        builder.Property(m => m.ErrorMessage)
            .HasColumnType("nvarchar(1000)")
            .HasDefaultValue(null);

        // An AiMessage belongs to an AiConversation
        builder.HasOne(m => m.AiConversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.AiConversationId)
            .OnDelete(DeleteBehavior.Cascade); // If conversation is specifically deleted (hard delete or cascading), clean up messages

        builder.HasIndex(m => new { m.AiConversationId, m.SequenceNumber })
            .IsUnique();
    }
}
