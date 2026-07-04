using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rafiq.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MigrateLabImageToLocalFileStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LabReports_AspNetUsers_UserId",
                table: "LabReports");

            migrationBuilder.DropColumn(
                name: "ImageData",
                table: "LabReports");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "LabReports",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_LabReports_AspNetUsers_UserId",
                table: "LabReports",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LabReports_AspNetUsers_UserId",
                table: "LabReports");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "LabReports");

            migrationBuilder.AddColumn<byte[]>(
                name: "ImageData",
                table: "LabReports",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddForeignKey(
                name: "FK_LabReports_AspNetUsers_UserId",
                table: "LabReports",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
