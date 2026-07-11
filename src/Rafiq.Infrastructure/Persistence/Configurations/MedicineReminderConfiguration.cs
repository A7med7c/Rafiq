using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Infrastructure.Persistence.Configurations;

public class MedicineReminderConfiguration : IEntityTypeConfiguration<MedicineReminder>
{
    public void Configure(EntityTypeBuilder<MedicineReminder> builder)
    {
        builder.ToTable("MedicineReminders");

        builder.HasKey(mr => mr.Id);

        builder.Property(mr => mr.RepeatType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(mr => mr.UserMedicine)
            .WithMany(um => um.Reminders)
            .HasForeignKey(mr => mr.UserMedicineId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
