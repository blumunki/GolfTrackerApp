using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GolfTrackerApp.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class RestrictDeleteBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoundPlayers_Players_PlayerId",
                table: "RoundPlayers");

            migrationBuilder.DropForeignKey(
                name: "FK_RoundPlayers_Rounds_RoundId",
                table: "RoundPlayers");

            migrationBuilder.DropForeignKey(
                name: "FK_Scores_Holes_HoleId",
                table: "Scores");

            migrationBuilder.DropForeignKey(
                name: "FK_Scores_Players_PlayerId",
                table: "Scores");

            migrationBuilder.AddForeignKey(
                name: "FK_RoundPlayers_Players_PlayerId",
                table: "RoundPlayers",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "PlayerId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RoundPlayers_Rounds_RoundId",
                table: "RoundPlayers",
                column: "RoundId",
                principalTable: "Rounds",
                principalColumn: "RoundId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Scores_Holes_HoleId",
                table: "Scores",
                column: "HoleId",
                principalTable: "Holes",
                principalColumn: "HoleId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Scores_Players_PlayerId",
                table: "Scores",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "PlayerId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoundPlayers_Players_PlayerId",
                table: "RoundPlayers");

            migrationBuilder.DropForeignKey(
                name: "FK_RoundPlayers_Rounds_RoundId",
                table: "RoundPlayers");

            migrationBuilder.DropForeignKey(
                name: "FK_Scores_Holes_HoleId",
                table: "Scores");

            migrationBuilder.DropForeignKey(
                name: "FK_Scores_Players_PlayerId",
                table: "Scores");

            migrationBuilder.AddForeignKey(
                name: "FK_RoundPlayers_Players_PlayerId",
                table: "RoundPlayers",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "PlayerId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RoundPlayers_Rounds_RoundId",
                table: "RoundPlayers",
                column: "RoundId",
                principalTable: "Rounds",
                principalColumn: "RoundId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Scores_Holes_HoleId",
                table: "Scores",
                column: "HoleId",
                principalTable: "Holes",
                principalColumn: "HoleId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Scores_Players_PlayerId",
                table: "Scores",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "PlayerId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
