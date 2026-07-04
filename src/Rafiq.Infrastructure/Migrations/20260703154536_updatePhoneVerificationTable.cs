using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rafiq.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updatePhoneVerificationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhoneVerification_AspNetUsers_UserId",
                table: "PhoneVerification");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PhoneVerification",
                table: "PhoneVerification");

            migrationBuilder.RenameTable(
                name: "PhoneVerification",
                newName: "PhoneVerifications");

            migrationBuilder.RenameIndex(
                name: "IX_PhoneVerification_UserId",
                table: "PhoneVerifications",
                newName: "IX_PhoneVerifications_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PhoneVerifications",
                table: "PhoneVerifications",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PhoneVerifications_AspNetUsers_UserId",
                table: "PhoneVerifications",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhoneVerifications_AspNetUsers_UserId",
                table: "PhoneVerifications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PhoneVerifications",
                table: "PhoneVerifications");

            migrationBuilder.RenameTable(
                name: "PhoneVerifications",
                newName: "PhoneVerification");

            migrationBuilder.RenameIndex(
                name: "IX_PhoneVerifications_UserId",
                table: "PhoneVerification",
                newName: "IX_PhoneVerification_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PhoneVerification",
                table: "PhoneVerification",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PhoneVerification_AspNetUsers_UserId",
                table: "PhoneVerification",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
