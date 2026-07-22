using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rafiq.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageReactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MessageReactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AiMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReactionType = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageReactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageReactions_AiMessages_AiMessageId",
                        column: x => x.AiMessageId,
                        principalTable: "AiMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MessageReactions_AiMessageId_UserId_ReactionType",
                table: "MessageReactions",
                columns: new[] { "AiMessageId", "UserId", "ReactionType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MessageReactions");
        }
    }
}
