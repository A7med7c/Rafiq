using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rafiq.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceAppReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "AppReviews",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "AppReviews",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AdminNotes",
                table: "AppReviews",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminReply",
                table: "AppReviews",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RepliedAt",
                table: "AppReviews",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "AppReviews",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceInfo",
                table: "AppReviews",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppVersion",
                table: "AppReviews",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppLanguage",
                table: "AppReviews",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppReviews_Status",
                table: "AppReviews",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppReviews_CreatedAt",
                table: "AppReviews",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_AppReviews_Status", table: "AppReviews");
            migrationBuilder.DropIndex(name: "IX_AppReviews_CreatedAt", table: "AppReviews");

            migrationBuilder.DropColumn(name: "Status",      table: "AppReviews");
            migrationBuilder.DropColumn(name: "Category",    table: "AppReviews");
            migrationBuilder.DropColumn(name: "AdminNotes",  table: "AppReviews");
            migrationBuilder.DropColumn(name: "AdminReply",  table: "AppReviews");
            migrationBuilder.DropColumn(name: "RepliedAt",   table: "AppReviews");
            migrationBuilder.DropColumn(name: "ReviewedAt",  table: "AppReviews");
            migrationBuilder.DropColumn(name: "DeviceInfo",  table: "AppReviews");
            migrationBuilder.DropColumn(name: "AppVersion",  table: "AppReviews");
            migrationBuilder.DropColumn(name: "AppLanguage", table: "AppReviews");
        }
    }
}
