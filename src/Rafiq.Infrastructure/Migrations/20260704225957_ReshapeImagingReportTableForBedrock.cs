using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rafiq.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReshapeImagingReportTableForBedrock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImagingReports");

            migrationBuilder.CreateTable(
                name: "ImagingReports",
                columns: table => new
                {
                    ReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImagingType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BodyPart = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Findings = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Impression = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReportDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AiSummary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DoctorName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportImage = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImagingReports", x => x.ReportId);
                    table.ForeignKey(
                        name: "FK_ImagingReports_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImagingReports_UserId",
                table: "ImagingReports",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImagingReports");

            migrationBuilder.CreateTable(
                name: "ImagingReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImagingType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BodyPart = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Findings = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Impression = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReportDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImagingReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImagingReports_MedicalDocuments_Id",
                        column: x => x.Id,
                        principalTable: "MedicalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
