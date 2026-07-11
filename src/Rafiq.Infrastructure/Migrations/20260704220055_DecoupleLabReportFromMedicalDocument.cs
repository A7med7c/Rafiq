using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rafiq.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DecoupleLabReportFromMedicalDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LabReports_MedicalDocuments_Id",
                table: "LabReports");

            migrationBuilder.AlterColumn<string>(
                name: "DoctorName",
                table: "LabReports",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "LabReports",
                type: "datetime2(7)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "LabReports",
                type: "datetime2(7)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "LabReports",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ImageData",
                table: "LabReports",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "LabReports",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OCRText",
                table: "LabReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "LabReports",
                type: "datetime2(7)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "LabReports",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_LabReports_CreatedAt",
                table: "LabReports",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LabReports_UserId",
                table: "LabReports",
                column: "UserId");

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

            migrationBuilder.DropIndex(
                name: "IX_LabReports_CreatedAt",
                table: "LabReports");

            migrationBuilder.DropIndex(
                name: "IX_LabReports_UserId",
                table: "LabReports");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "LabReports");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "LabReports");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "LabReports");

            migrationBuilder.DropColumn(
                name: "ImageData",
                table: "LabReports");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "LabReports");

            migrationBuilder.DropColumn(
                name: "OCRText",
                table: "LabReports");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "LabReports");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "LabReports");

            migrationBuilder.AlterColumn<string>(
                name: "DoctorName",
                table: "LabReports",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddForeignKey(
                name: "FK_LabReports_MedicalDocuments_Id",
                table: "LabReports",
                column: "Id",
                principalTable: "MedicalDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
