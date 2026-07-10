using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rafiq.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHealthProfileAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HealthProfileAccesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserHealthProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GranteeUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    InvitedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StatusChangedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthProfileAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthProfileAccesses_AspNetUsers_GranteeUserId",
                        column: x => x.GranteeUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HealthProfileAccesses_AspNetUsers_InvitedByUserId",
                        column: x => x.InvitedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HealthProfileAccesses_UserHealthProfiles_UserHealthProfileId",
                        column: x => x.UserHealthProfileId,
                        principalTable: "UserHealthProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HealthProfileAccesses_GranteeUserId",
                table: "HealthProfileAccesses",
                column: "GranteeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthProfileAccesses_InvitedByUserId",
                table: "HealthProfileAccesses",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthProfileAccesses_UserHealthProfileId",
                table: "HealthProfileAccesses",
                column: "UserHealthProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthProfileAccesses_UserHealthProfileId_GranteeUserId",
                table: "HealthProfileAccesses",
                columns: new[] { "UserHealthProfileId", "GranteeUserId" },
                unique: true,
                filter: "[Status] IN (1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HealthProfileAccesses");
        }
    }
}
