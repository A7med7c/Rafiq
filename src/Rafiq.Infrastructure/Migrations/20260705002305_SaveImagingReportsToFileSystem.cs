using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rafiq.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SaveImagingReportsToFileSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReportImage",
                table: "ImagingReports");

            migrationBuilder.AddColumn<string>(
                name: "ReportImagePath",
                table: "ImagingReports",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReportImagePath",
                table: "ImagingReports");

            migrationBuilder.AddColumn<byte[]>(
                name: "ReportImage",
                table: "ImagingReports",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);
        }
    }
}
