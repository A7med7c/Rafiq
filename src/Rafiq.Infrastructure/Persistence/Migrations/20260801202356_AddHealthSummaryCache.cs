using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rafiq.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHealthSummaryCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HealthSummaryCaches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserHealthProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SummaryJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    NeedsRefresh = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthSummaryCaches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthSummaryCaches_UserHealthProfiles_UserHealthProfileId",
                        column: x => x.UserHealthProfileId,
                        principalTable: "UserHealthProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HealthSummaryCaches_UserHealthProfileId_Language",
                table: "HealthSummaryCaches",
                columns: new[] { "UserHealthProfileId", "Language" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HealthSummaryCaches");
        }
    }
}
