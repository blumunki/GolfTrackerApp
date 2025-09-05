using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GolfTrackerApp.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddRoundDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HolesPlayed",
                table: "Rounds",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RoundType",
                table: "Rounds",
                type: "INTEGER",
                maxLength: 50,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StartingHole",
                table: "Rounds",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HolesPlayed",
                table: "Rounds");

            migrationBuilder.DropColumn(
                name: "RoundType",
                table: "Rounds");

            migrationBuilder.DropColumn(
                name: "StartingHole",
                table: "Rounds");
        }
    }
}
