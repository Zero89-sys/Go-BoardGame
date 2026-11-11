using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gogame.Migrations
{
    /// <inheritdoc />
    public partial class AddBotRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OpponentRating",
                table: "Games");

            migrationBuilder.AddColumn<string>(
                name: "BotRating",
                table: "Games",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BotRating",
                table: "Games");

            migrationBuilder.AddColumn<int>(
                name: "OpponentRating",
                table: "Games",
                type: "INTEGER",
                nullable: true);
        }
    }
}
