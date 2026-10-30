using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gogame.Migrations
{
    /// <inheritdoc />
    public partial class GameStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OpponentRating",
                table: "Games",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlayerColor",
                table: "Games",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OpponentRating",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "PlayerColor",
                table: "Games");
        }
    }
}
