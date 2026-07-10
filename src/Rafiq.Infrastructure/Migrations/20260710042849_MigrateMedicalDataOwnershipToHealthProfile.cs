using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rafiq.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MigrateMedicalDataOwnershipToHealthProfile : Migration
    {
        private static readonly string[] Tables =
        {
            "Appointments",
            "GeneralDocuments",
            "ImagingReports",
            "LabReports",
            "Prescriptions",
            "UserMedicines"
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: drop the old FKs to AspNetUsers before touching the UserId columns.
            migrationBuilder.DropForeignKey(name: "FK_Appointments_AspNetUsers_UserId", table: "Appointments");
            migrationBuilder.DropForeignKey(name: "FK_GeneralDocuments_AspNetUsers_UserId", table: "GeneralDocuments");
            migrationBuilder.DropForeignKey(name: "FK_ImagingReports_AspNetUsers_UserId", table: "ImagingReports");
            migrationBuilder.DropForeignKey(name: "FK_LabReports_AspNetUsers_UserId", table: "LabReports");
            migrationBuilder.DropForeignKey(name: "FK_Prescriptions_AspNetUsers_UserId", table: "Prescriptions");
            migrationBuilder.DropForeignKey(name: "FK_UserMedicines_AspNetUsers_UserId", table: "UserMedicines");

            // Step 2: drop the old UserId-based indexes; they'll be recreated on the new column below.
            migrationBuilder.DropIndex(name: "IX_Appointments_UserId_Status_AppointmentDateTime", table: "Appointments");
            migrationBuilder.DropIndex(name: "IX_Appointments_UserId_AppointmentDateTime", table: "Appointments");
            migrationBuilder.DropIndex(name: "IX_Appointments_UserId", table: "Appointments");
            migrationBuilder.DropIndex(name: "IX_GeneralDocuments_UserId", table: "GeneralDocuments");
            migrationBuilder.DropIndex(name: "IX_ImagingReports_UserId", table: "ImagingReports");
            migrationBuilder.DropIndex(name: "IX_LabReports_UserId", table: "LabReports");
            migrationBuilder.DropIndex(name: "IX_Prescriptions_UserId", table: "Prescriptions");
            migrationBuilder.DropIndex(name: "IX_UserMedicines_UserId", table: "UserMedicines");

            // Step 3: add the new FK column as nullable first, alongside the still-present UserId column.
            foreach (var table in Tables)
            {
                migrationBuilder.AddColumn<Guid>(
                    name: "UserHealthProfileId",
                    table: table,
                    type: "uniqueidentifier",
                    nullable: true);
            }

            // Step 4: backfill from the account's existing Self UserHealthProfile.
            // (UserId on these tables has always meant "the registered account this record belongs
            // to", which is exactly what a Self Profile's UserHealthProfile.UserId represents.)
            foreach (var table in Tables)
            {
                migrationBuilder.Sql($@"
                    UPDATE t
                    SET t.UserHealthProfileId = uhp.Id
                    FROM {table} t
                    INNER JOIN UserHealthProfiles uhp ON uhp.UserId = t.UserId
                    WHERE t.UserId IS NOT NULL;
                ");
            }

            // Step 5: fail loudly instead of silently losing or misattributing rows whose account
            // never created a Self Health Profile (health profile creation is a separate, later
            // step from registration - see Part 1/3). If this fires, those rows must be resolved
            // manually (e.g. backfilling a Self Profile for the affected accounts) before retrying.
            foreach (var table in Tables)
            {
                migrationBuilder.Sql($@"
                    IF EXISTS (SELECT 1 FROM {table} WHERE UserHealthProfileId IS NULL)
                    BEGIN
                        THROW 51000, 'MigrateMedicalDataOwnershipToHealthProfile: {table} has rows whose UserId has no matching Self UserHealthProfile. Resolve manually before retrying this migration.', 1;
                    END
                ");
            }

            // Step 6: the old UserId column is no longer needed - its data now lives in UserHealthProfileId.
            foreach (var table in Tables)
            {
                migrationBuilder.DropColumn(name: "UserId", table: table);
            }

            // Step 7: now that every row is backfilled, make the new FK column required.
            foreach (var table in Tables)
            {
                migrationBuilder.AlterColumn<Guid>(
                    name: "UserHealthProfileId",
                    table: table,
                    type: "uniqueidentifier",
                    nullable: false,
                    oldClrType: typeof(Guid),
                    oldType: "uniqueidentifier",
                    oldNullable: true);
            }

            // Step 8: recreate the indexes against the new column.
            migrationBuilder.CreateIndex(name: "IX_Appointments_UserHealthProfileId", table: "Appointments", column: "UserHealthProfileId");
            migrationBuilder.CreateIndex(name: "IX_Appointments_UserHealthProfileId_AppointmentDateTime", table: "Appointments", columns: new[] { "UserHealthProfileId", "AppointmentDateTime" });
            migrationBuilder.CreateIndex(name: "IX_Appointments_UserHealthProfileId_Status_AppointmentDateTime", table: "Appointments", columns: new[] { "UserHealthProfileId", "Status", "AppointmentDateTime" });
            migrationBuilder.CreateIndex(name: "IX_GeneralDocuments_UserHealthProfileId", table: "GeneralDocuments", column: "UserHealthProfileId");
            migrationBuilder.CreateIndex(name: "IX_ImagingReports_UserHealthProfileId", table: "ImagingReports", column: "UserHealthProfileId");
            migrationBuilder.CreateIndex(name: "IX_LabReports_UserHealthProfileId", table: "LabReports", column: "UserHealthProfileId");
            migrationBuilder.CreateIndex(name: "IX_Prescriptions_UserHealthProfileId", table: "Prescriptions", column: "UserHealthProfileId");
            migrationBuilder.CreateIndex(name: "IX_UserMedicines_UserHealthProfileId", table: "UserMedicines", column: "UserHealthProfileId");

            // Step 9: point the FK at UserHealthProfiles instead of AspNetUsers. Cascade matches the
            // previous behavior (and the existing Allergy/ChronicDisease -> UserHealthProfile FKs);
            // a hard-deleted profile takes its medical records with it, but the app only soft-deletes.
            migrationBuilder.AddForeignKey(name: "FK_Appointments_UserHealthProfiles_UserHealthProfileId", table: "Appointments", column: "UserHealthProfileId", principalTable: "UserHealthProfiles", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_GeneralDocuments_UserHealthProfiles_UserHealthProfileId", table: "GeneralDocuments", column: "UserHealthProfileId", principalTable: "UserHealthProfiles", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_ImagingReports_UserHealthProfiles_UserHealthProfileId", table: "ImagingReports", column: "UserHealthProfileId", principalTable: "UserHealthProfiles", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_LabReports_UserHealthProfiles_UserHealthProfileId", table: "LabReports", column: "UserHealthProfileId", principalTable: "UserHealthProfiles", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_Prescriptions_UserHealthProfiles_UserHealthProfileId", table: "Prescriptions", column: "UserHealthProfileId", principalTable: "UserHealthProfiles", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_UserMedicines_UserHealthProfiles_UserHealthProfileId", table: "UserMedicines", column: "UserHealthProfileId", principalTable: "UserHealthProfiles", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Appointments_UserHealthProfiles_UserHealthProfileId", table: "Appointments");
            migrationBuilder.DropForeignKey(name: "FK_GeneralDocuments_UserHealthProfiles_UserHealthProfileId", table: "GeneralDocuments");
            migrationBuilder.DropForeignKey(name: "FK_ImagingReports_UserHealthProfiles_UserHealthProfileId", table: "ImagingReports");
            migrationBuilder.DropForeignKey(name: "FK_LabReports_UserHealthProfiles_UserHealthProfileId", table: "LabReports");
            migrationBuilder.DropForeignKey(name: "FK_Prescriptions_UserHealthProfiles_UserHealthProfileId", table: "Prescriptions");
            migrationBuilder.DropForeignKey(name: "FK_UserMedicines_UserHealthProfiles_UserHealthProfileId", table: "UserMedicines");

            migrationBuilder.DropIndex(name: "IX_Appointments_UserHealthProfileId_Status_AppointmentDateTime", table: "Appointments");
            migrationBuilder.DropIndex(name: "IX_Appointments_UserHealthProfileId_AppointmentDateTime", table: "Appointments");
            migrationBuilder.DropIndex(name: "IX_Appointments_UserHealthProfileId", table: "Appointments");
            migrationBuilder.DropIndex(name: "IX_GeneralDocuments_UserHealthProfileId", table: "GeneralDocuments");
            migrationBuilder.DropIndex(name: "IX_ImagingReports_UserHealthProfileId", table: "ImagingReports");
            migrationBuilder.DropIndex(name: "IX_LabReports_UserHealthProfileId", table: "LabReports");
            migrationBuilder.DropIndex(name: "IX_Prescriptions_UserHealthProfileId", table: "Prescriptions");
            migrationBuilder.DropIndex(name: "IX_UserMedicines_UserHealthProfileId", table: "UserMedicines");

            foreach (var table in Tables)
            {
                migrationBuilder.AddColumn<Guid>(
                    name: "UserId",
                    table: table,
                    type: "uniqueidentifier",
                    nullable: true);
            }

            // Best-effort reverse mapping via the Self Profile's UserId. Note this is lossy for
            // records owned by a Managed or Shared profile: the pre-Part-11 schema has no way to
            // represent that ownership, so those rows end up with UserId = NULL after a downgrade.
            foreach (var table in Tables)
            {
                migrationBuilder.Sql($@"
                    UPDATE t
                    SET t.UserId = uhp.UserId
                    FROM {table} t
                    INNER JOIN UserHealthProfiles uhp ON uhp.Id = t.UserHealthProfileId
                    WHERE uhp.UserId IS NOT NULL;
                ");
            }

            foreach (var table in Tables)
            {
                migrationBuilder.DropColumn(name: "UserHealthProfileId", table: table);
            }

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Appointments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "GeneralDocuments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "ImagingReports",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "LabReports",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Prescriptions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "UserMedicines",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(name: "IX_Appointments_UserId", table: "Appointments", column: "UserId");
            migrationBuilder.CreateIndex(name: "IX_Appointments_UserId_AppointmentDateTime", table: "Appointments", columns: new[] { "UserId", "AppointmentDateTime" });
            migrationBuilder.CreateIndex(name: "IX_Appointments_UserId_Status_AppointmentDateTime", table: "Appointments", columns: new[] { "UserId", "Status", "AppointmentDateTime" });
            migrationBuilder.CreateIndex(name: "IX_GeneralDocuments_UserId", table: "GeneralDocuments", column: "UserId");
            migrationBuilder.CreateIndex(name: "IX_ImagingReports_UserId", table: "ImagingReports", column: "UserId");
            migrationBuilder.CreateIndex(name: "IX_LabReports_UserId", table: "LabReports", column: "UserId");
            migrationBuilder.CreateIndex(name: "IX_Prescriptions_UserId", table: "Prescriptions", column: "UserId");
            migrationBuilder.CreateIndex(name: "IX_UserMedicines_UserId", table: "UserMedicines", column: "UserId");

            migrationBuilder.AddForeignKey(name: "FK_Appointments_AspNetUsers_UserId", table: "Appointments", column: "UserId", principalTable: "AspNetUsers", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_GeneralDocuments_AspNetUsers_UserId", table: "GeneralDocuments", column: "UserId", principalTable: "AspNetUsers", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_ImagingReports_AspNetUsers_UserId", table: "ImagingReports", column: "UserId", principalTable: "AspNetUsers", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_LabReports_AspNetUsers_UserId", table: "LabReports", column: "UserId", principalTable: "AspNetUsers", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_Prescriptions_AspNetUsers_UserId", table: "Prescriptions", column: "UserId", principalTable: "AspNetUsers", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_UserMedicines_AspNetUsers_UserId", table: "UserMedicines", column: "UserId", principalTable: "AspNetUsers", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
        }
    }
}
