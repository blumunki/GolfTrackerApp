using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GolfTrackerApp.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnershipToPlayersAndRounds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByApplicationUserId",
                table: "Rounds",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByApplicationUserId",
                table: "Players",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rounds_CreatedByApplicationUserId",
                table: "Rounds",
                column: "CreatedByApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_CreatedByApplicationUserId",
                table: "Players",
                column: "CreatedByApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Players_AspNetUsers_CreatedByApplicationUserId",
                table: "Players",
                column: "CreatedByApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Rounds_AspNetUsers_CreatedByApplicationUserId",
                table: "Rounds",
                column: "CreatedByApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Players_AspNetUsers_CreatedByApplicationUserId",
                table: "Players");

            migrationBuilder.DropForeignKey(
                name: "FK_Rounds_AspNetUsers_CreatedByApplicationUserId",
                table: "Rounds");

            migrationBuilder.DropIndex(
                name: "IX_Rounds_CreatedByApplicationUserId",
                table: "Rounds");

            migrationBuilder.DropIndex(
                name: "IX_Players_CreatedByApplicationUserId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "CreatedByApplicationUserId",
                table: "Rounds");

            migrationBuilder.DropColumn(
                name: "CreatedByApplicationUserId",
                table: "Players");
        }
    }
}
