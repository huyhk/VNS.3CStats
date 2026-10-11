using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VNS._3CStats.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SplitMatchInningsPerPlayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Innings",
                table: "Matches",
                newName: "Player2Innings");

            migrationBuilder.AddColumn<int>(
                name: "Player1Innings",
                table: "Matches",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Player1Innings",
                table: "Matches");

            migrationBuilder.RenameColumn(
                name: "Player2Innings",
                table: "Matches",
                newName: "Innings");
        }
    }
}
