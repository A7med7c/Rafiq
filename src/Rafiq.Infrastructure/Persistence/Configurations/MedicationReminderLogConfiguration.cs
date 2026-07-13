using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Infrastructure.Persistence.Configurations;

internal sealed class MedicationReminderLogConfiguration : IEntityTypeConfiguration<MedicationReminderLog>
{
    public void Configure(EntityTypeBuilder<MedicationReminderLog> builder)
    {
        builder.ToTable("MedicationReminderLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReminderNumber)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.ScheduledTime)
            .IsRequired();

        builder.Property(x => x.ScheduledDate)
            .IsRequired();

        builder.Property(x => x.NextJobId)
            .HasMaxLength(100);

        builder.HasOne(x => x.MedicineReminder)
            .WithMany()
            .HasForeignKey(x => x.MedicineReminderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.UserHealthProfile)
            .WithMany()
            .HasForeignKey(x => x.UserHealthProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.UserHealthProfileId, x.ScheduledDate });

        // Enforces "no duplicate reminder logs" at the database level: two concurrent requests
        // (e.g. the daily sweep and a manual create racing) can both pass the app-level
        // ExistsForDateAsync check, but only one can win this unique index — the loser's
        // SaveChanges throws and MedicationSchedulingService treats that as "already scheduled".
        // Cancelled rows are excluded from the filter so an edit/delete can cancel a stage and
        // have a replacement created for the same reminder+date+stage without a conflict.
        builder.HasIndex(x => new { x.MedicineReminderId, x.ScheduledDate, x.ReminderNumber })
            .IsUnique()
            .HasFilter("[Status] <> N'Cancelled'");
    }
}
