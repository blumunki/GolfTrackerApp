using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GolfTrackerApp.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTeeSetsModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoundPlayers_TeeSets_TeeSetId",
                table: "RoundPlayers");

            migrationBuilder.DropForeignKey(
                name: "FK_Scores_TeeSets_TeeSetId",
                table: "Scores");

            migrationBuilder.AddForeignKey(
                name: "FK_RoundPlayers_TeeSets_TeeSetId",
                table: "RoundPlayers",
                column: "TeeSetId",
                principalTable: "TeeSets",
                principalColumn: "TeeSetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Scores_TeeSets_TeeSetId",
                table: "Scores",
                column: "TeeSetId",
                principalTable: "TeeSets",
                principalColumn: "TeeSetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoundPlayers_TeeSets_TeeSetId",
                table: "RoundPlayers");

            migrationBuilder.DropForeignKey(
                name: "FK_Scores_TeeSets_TeeSetId",
                table: "Scores");

            migrationBuilder.AddForeignKey(
                name: "FK_RoundPlayers_TeeSets_TeeSetId",
                table: "RoundPlayers",
                column: "TeeSetId",
                principalTable: "TeeSets",
                principalColumn: "TeeSetId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Scores_TeeSets_TeeSetId",
                table: "Scores",
                column: "TeeSetId",
                principalTable: "TeeSets",
                principalColumn: "TeeSetId",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
