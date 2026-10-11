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
                newName: "Player1Innings");

            migrationBuilder.AddColumn<int>(
                name: "Player2Innings",
                table: "Matches",
                type: "INTEGER",
                nullable: true);

            // The old schema stored only one innings value. Preserve it on both
            // sides so existing rows remain internally consistent after the split.
            migrationBuilder.Sql(
                "UPDATE Matches SET Player2Innings = Player1Innings WHERE Player1Innings IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Player2Innings",
                table: "Matches");

            migrationBuilder.RenameColumn(
                name: "Player1Innings",
                table: "Matches",
                newName: "Innings");
        }
    }
}
