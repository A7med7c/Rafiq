using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rafiq.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRelationshipToHealthProfileAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Relationship",
                table: "HealthProfileAccesses",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE access
                SET access.Relationship = 1
                FROM HealthProfileAccesses AS access
                INNER JOIN UserHealthProfiles AS profile
                    ON profile.Id = access.UserHealthProfileId
                WHERE profile.UserId IS NOT NULL
                  AND access.GranteeUserId = profile.UserId
                  AND access.Role = 1
                  AND access.Status = 2;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Relationship",
                table: "HealthProfileAccesses");
        }
    }
}
