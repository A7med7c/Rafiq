using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rafiq.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── AiRequestLogs ─────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "AiRequestLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Feature = table.Column<int>(type: "int", nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    ErrorType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DurationMs = table.Column<int>(type: "int", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiRequestLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiRequestLogs_CreatedAt",
                table: "AiRequestLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AiRequestLogs_Feature_CreatedAt",
                table: "AiRequestLogs",
                columns: new[] { "Feature", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AiRequestLogs_UserId",
                table: "AiRequestLogs",
                column: "UserId");

            // ── MessageReactions — admin triage columns ───────────────────────
            migrationBuilder.AddColumn<int>(
                name: "TriageStatus",
                table: "MessageReactions",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "MessageReactions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminNotes",
                table: "MessageReactions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "AdminNotes",   table: "MessageReactions");
            migrationBuilder.DropColumn(name: "Category",     table: "MessageReactions");
            migrationBuilder.DropColumn(name: "TriageStatus", table: "MessageReactions");

            migrationBuilder.DropTable(name: "AiRequestLogs");
        }
    }
}
