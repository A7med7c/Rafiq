using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rafiq.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicalAttentionWarningsV6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ConfidenceScore",
                table: "UserMedicines",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicalAttentionReason",
                table: "UserMedicines",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecommendedSpecialty",
                table: "UserMedicines",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ConfidenceScore",
                table: "Prescriptions",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicalAttentionReason",
                table: "Prescriptions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecommendedSpecialty",
                table: "Prescriptions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ConfidenceScore",
                table: "LabReports",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicalAttentionReason",
                table: "LabReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecommendedSpecialty",
                table: "LabReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ConfidenceScore",
                table: "ImagingReports",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicalAttentionReason",
                table: "ImagingReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecommendedSpecialty",
                table: "ImagingReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ConfidenceScore",
                table: "GeneralDocuments",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicalAttentionReason",
                table: "GeneralDocuments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecommendedSpecialty",
                table: "GeneralDocuments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropColumn(
                name: "ConfidenceScore",
                table: "UserMedicines");

            migrationBuilder.DropColumn(
                name: "MedicalAttentionReason",
                table: "UserMedicines");

            migrationBuilder.DropColumn(
                name: "RecommendedSpecialty",
                table: "UserMedicines");

            migrationBuilder.DropColumn(
                name: "ConfidenceScore",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "MedicalAttentionReason",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "RecommendedSpecialty",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "ConfidenceScore",
                table: "LabReports");

            migrationBuilder.DropColumn(
                name: "MedicalAttentionReason",
                table: "LabReports");

            migrationBuilder.DropColumn(
                name: "RecommendedSpecialty",
                table: "LabReports");

            migrationBuilder.DropColumn(
                name: "ConfidenceScore",
                table: "ImagingReports");

            migrationBuilder.DropColumn(
                name: "MedicalAttentionReason",
                table: "ImagingReports");

            migrationBuilder.DropColumn(
                name: "RecommendedSpecialty",
                table: "ImagingReports");

            migrationBuilder.DropColumn(
                name: "ConfidenceScore",
                table: "GeneralDocuments");

            migrationBuilder.DropColumn(
                name: "MedicalAttentionReason",
                table: "GeneralDocuments");

            migrationBuilder.DropColumn(
                name: "RecommendedSpecialty",
                table: "GeneralDocuments");
        }
    }
}
