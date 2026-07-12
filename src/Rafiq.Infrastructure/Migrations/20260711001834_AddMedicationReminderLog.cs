using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rafiq.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicationReminderLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MedicationReminderLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicineReminderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserHealthProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ScheduledTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    ReminderNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    NextJobId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationReminderLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicationReminderLogs_MedicineReminders_MedicineReminderId",
                        column: x => x.MedicineReminderId,
                        principalTable: "MedicineReminders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicationReminderLogs_UserHealthProfiles_UserHealthProfileId",
                        column: x => x.UserHealthProfileId,
                        principalTable: "UserHealthProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicationReminderLogs_MedicineReminderId_ScheduledDate_ReminderNumber",
                table: "MedicationReminderLogs",
                columns: new[] { "MedicineReminderId", "ScheduledDate", "ReminderNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicationReminderLogs_UserHealthProfileId_ScheduledDate",
                table: "MedicationReminderLogs",
                columns: new[] { "UserHealthProfileId", "ScheduledDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MedicationReminderLogs");
        }
    }
}
