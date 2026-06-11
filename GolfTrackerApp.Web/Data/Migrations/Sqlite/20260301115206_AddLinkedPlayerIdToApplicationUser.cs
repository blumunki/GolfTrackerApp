using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GolfTrackerApp.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLinkedPlayerIdToApplicationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LinkedPlayerId",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_LinkedPlayerId",
                table: "AspNetUsers",
                column: "LinkedPlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Players_LinkedPlayerId",
                table: "AspNetUsers",
                column: "LinkedPlayerId",
                principalTable: "Players",
                principalColumn: "PlayerId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Players_LinkedPlayerId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_LinkedPlayerId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LinkedPlayerId",
                table: "AspNetUsers");
        }
    }
}
