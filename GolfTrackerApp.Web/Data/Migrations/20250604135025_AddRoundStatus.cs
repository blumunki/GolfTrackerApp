using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GolfTrackerApp.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddRoundStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Rounds",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Rounds");
        }
    }
}
