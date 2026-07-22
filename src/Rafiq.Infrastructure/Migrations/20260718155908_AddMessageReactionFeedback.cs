using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rafiq.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageReactionFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Feedback",
                table: "MessageReactions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Feedback",
                table: "MessageReactions");
        }
    }
}
