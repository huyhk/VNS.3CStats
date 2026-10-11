using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VNS._3CStats.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SupportSourcePlayersWithoutExternalIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ExternalPlayerId",
                table: "SourcePlayerIdentities",
                type: "TEXT",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 150);

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "SourcePlayerIdentities",
                type: "TEXT",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "SourcePlayerIdentities",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_SourcePlayerIdentities_DataSourceId_NormalizedName_CountryCode",
                table: "SourcePlayerIdentities",
                columns: new[] { "DataSourceId", "NormalizedName", "CountryCode" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SourcePlayerIdentities_DataSourceId_NormalizedName_CountryCode",
                table: "SourcePlayerIdentities");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "SourcePlayerIdentities");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "SourcePlayerIdentities");

            migrationBuilder.AlterColumn<string>(
                name: "ExternalPlayerId",
                table: "SourcePlayerIdentities",
                type: "TEXT",
                maxLength: 150,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 150,
                oldNullable: true);
        }
    }
}
