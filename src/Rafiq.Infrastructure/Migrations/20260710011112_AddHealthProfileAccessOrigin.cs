using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rafiq.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHealthProfileAccessOrigin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Origin",
                table: "HealthProfileAccesses",
                type: "int",
                nullable: false,
                defaultValue: 2); // GrantInvitation

            // Backfill existing rows: prior to this migration, the only way a row could
            // have InvitedByUserId set was through CreateInvitation (Grant Invitation).
            // Rows with no InvitedByUserId were created directly (Self/Managed Owner).
            migrationBuilder.Sql(@"
                UPDATE HealthProfileAccesses
                SET Origin = 1 -- Direct
                WHERE InvitedByUserId IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Origin",
                table: "HealthProfileAccesses");
        }
    }
}
