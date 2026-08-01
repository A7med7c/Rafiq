using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rafiq.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUsageIntelligence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiFlaggedRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserRequest = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AiResponse = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Classification = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiFlaggedRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiUsageActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdminName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiUsageActions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiFlaggedRequests_CreatedAt",
                table: "AiFlaggedRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AiFlaggedRequests_UserId",
                table: "AiFlaggedRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AiFlaggedRequests_UserId_CreatedAt",
                table: "AiFlaggedRequests",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageActions_CreatedAt",
                table: "AiUsageActions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageActions_TargetUserId",
                table: "AiUsageActions",
                column: "TargetUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiFlaggedRequests");

            migrationBuilder.DropTable(
                name: "AiUsageActions");
        }
    }
}
