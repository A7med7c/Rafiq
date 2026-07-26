using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rafiq.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserNotifications",
                columns: table => new
                {
                    Id          = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId      = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title       = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Body        = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Type        = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "system"),
                    IsRead      = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ReadAt      = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt   = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt   = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted   = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt   = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_UserId_IsRead",
                table: "UserNotifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_CreatedAt",
                table: "UserNotifications",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "UserNotifications");
        }
    }
}
