using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rafiq.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MigratePatientCaregiverRolesToUserAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfileImageUrl",
                table: "UserHealthProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            // Seed the two new auth roles if they don't already exist.
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE NormalizedName = 'USER')
                    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
                    VALUES (NEWID(), 'User', 'USER', CONVERT(nvarchar(36), NEWID()));

                IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE NormalizedName = 'ADMIN')
                    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
                    VALUES (NEWID(), 'Admin', 'ADMIN', CONVERT(nvarchar(36), NEWID()));
                """);

            // Reassign every user currently in "Patient" or "Caregiver" to "User", then drop
            // the old role rows. Users already in "User" are skipped to avoid a duplicate
            // AspNetUserRoles (UserId, RoleId) primary key violation.
            migrationBuilder.Sql("""
                DECLARE @UserRoleId uniqueidentifier = (SELECT Id FROM AspNetRoles WHERE NormalizedName = 'USER');
                DECLARE @PatientRoleId uniqueidentifier = (SELECT Id FROM AspNetRoles WHERE NormalizedName = 'PATIENT');
                DECLARE @CaregiverRoleId uniqueidentifier = (SELECT Id FROM AspNetRoles WHERE NormalizedName = 'CAREGIVER');

                IF @PatientRoleId IS NOT NULL
                BEGIN
                    UPDATE ur SET ur.RoleId = @UserRoleId
                    FROM AspNetUserRoles ur
                    WHERE ur.RoleId = @PatientRoleId
                      AND NOT EXISTS (SELECT 1 FROM AspNetUserRoles ur2 WHERE ur2.UserId = ur.UserId AND ur2.RoleId = @UserRoleId);

                    DELETE FROM AspNetUserRoles WHERE RoleId = @PatientRoleId;
                    DELETE FROM AspNetRoles WHERE Id = @PatientRoleId;
                END

                IF @CaregiverRoleId IS NOT NULL
                BEGIN
                    UPDATE ur SET ur.RoleId = @UserRoleId
                    FROM AspNetUserRoles ur
                    WHERE ur.RoleId = @CaregiverRoleId
                      AND NOT EXISTS (SELECT 1 FROM AspNetUserRoles ur2 WHERE ur2.UserId = ur.UserId AND ur2.RoleId = @UserRoleId);

                    DELETE FROM AspNetUserRoles WHERE RoleId = @CaregiverRoleId;
                    DELETE FROM AspNetRoles WHERE Id = @CaregiverRoleId;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Best-effort only: recreates the "Patient"/"Caregiver" role rows, but which
            // specific users originally held them is not recoverable — that information is
            // lost by design once accounts are consolidated onto "User".
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE NormalizedName = 'PATIENT')
                    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
                    VALUES (NEWID(), 'Patient', 'PATIENT', CONVERT(nvarchar(36), NEWID()));

                IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE NormalizedName = 'CAREGIVER')
                    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
                    VALUES (NEWID(), 'Caregiver', 'CAREGIVER', CONVERT(nvarchar(36), NEWID()));
                """);

            migrationBuilder.DropColumn(
                name: "ProfileImageUrl",
                table: "UserHealthProfiles");
        }
    }
}
