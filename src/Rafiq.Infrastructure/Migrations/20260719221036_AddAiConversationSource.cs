using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rafiq.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiConversationSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "AiConversations",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Source",
                table: "AiConversations");
        }
    }
}
