using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rafiq.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackfillSelfOwnerHealthProfileAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill an Active Owner HealthProfileAccess for every existing Self
            // UserHealthProfile (UserId IS NOT NULL) that does not already have one.
            // Safe to re-run: the NOT EXISTS guard skips profiles that already have
            // a Pending/Active access row for their owning user.
            migrationBuilder.Sql(@"
                INSERT INTO HealthProfileAccesses
                    (Id, UserHealthProfileId, GranteeUserId, Role, Status, InvitedByUserId, StatusChangedAt, CreatedAt, UpdatedAt, IsDeleted, DeletedAt)
                SELECT
                    NEWID(), uhp.Id, uhp.UserId, 1, 2, NULL, SYSUTCDATETIME(), SYSUTCDATETIME(), NULL, 0, NULL
                FROM UserHealthProfiles uhp
                WHERE uhp.UserId IS NOT NULL
                  AND uhp.IsDeleted = 0
                  AND NOT EXISTS (
                      SELECT 1
                      FROM HealthProfileAccesses hpa
                      WHERE hpa.UserHealthProfileId = uhp.Id
                        AND hpa.GranteeUserId = uhp.UserId
                        AND hpa.Status IN (1, 2)
                  );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data backfill only; not reversible without knowing which rows it created.
        }
    }
}
