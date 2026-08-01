using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rafiq.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAsyncDocumentAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnalysisStatus",
                table: "GeneralDocuments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "GeneralDocuments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessingStartedAt",
                table: "GeneralDocuments",
                type: "datetime2(7)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GeneralDocuments_AnalysisStatus_UpdatedAt",
                table: "GeneralDocuments",
                columns: new[] { "AnalysisStatus", "UpdatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GeneralDocuments_AnalysisStatus_UpdatedAt",
                table: "GeneralDocuments");

            migrationBuilder.DropColumn(
                name: "AnalysisStatus",
                table: "GeneralDocuments");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "GeneralDocuments");

            migrationBuilder.DropColumn(
                name: "ProcessingStartedAt",
                table: "GeneralDocuments");
        }
    }
}
