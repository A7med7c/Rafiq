using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rafiq.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGeneralDocumentMetadataFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentDate",
                table: "GeneralDocuments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentType",
                table: "GeneralDocuments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DoctorName",
                table: "GeneralDocuments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HospitalOrClinic",
                table: "GeneralDocuments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OcrText",
                table: "GeneralDocuments",
                type: "nvarchar(max)",
                maxLength: 10000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentDate",
                table: "GeneralDocuments");

            migrationBuilder.DropColumn(
                name: "DocumentType",
                table: "GeneralDocuments");

            migrationBuilder.DropColumn(
                name: "DoctorName",
                table: "GeneralDocuments");

            migrationBuilder.DropColumn(
                name: "HospitalOrClinic",
                table: "GeneralDocuments");

            migrationBuilder.DropColumn(
                name: "OcrText",
                table: "GeneralDocuments");
        }
    }
}
