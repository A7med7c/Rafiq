using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rafiq.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueFilteredIndexOnMedicationReminderLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MedicationReminderLogs_MedicineReminderId_ScheduledDate_ReminderNumber",
                table: "MedicationReminderLogs");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationReminderLogs_MedicineReminderId_ScheduledDate_ReminderNumber",
                table: "MedicationReminderLogs",
                columns: new[] { "MedicineReminderId", "ScheduledDate", "ReminderNumber" },
                unique: true,
                filter: "[Status] <> N'Cancelled'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MedicationReminderLogs_MedicineReminderId_ScheduledDate_ReminderNumber",
                table: "MedicationReminderLogs");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationReminderLogs_MedicineReminderId_ScheduledDate_ReminderNumber",
                table: "MedicationReminderLogs",
                columns: new[] { "MedicineReminderId", "ScheduledDate", "ReminderNumber" });
        }
    }
}
