using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GolfTrackerApp.Core.Data.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddCompetitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompetitionId",
                table: "Rounds",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Competitions",
                columns: table => new
                {
                    CompetitionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    GolfClubId = table.Column<int>(type: "int", nullable: true),
                    GolfSocietyId = table.Column<int>(type: "int", nullable: true),
                    GolfCourseId = table.Column<int>(type: "int", nullable: true),
                    ScoringFormat = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsOpen = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Competitions", x => x.CompetitionId);
                    table.ForeignKey(
                        name: "FK_Competitions_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Competitions_GolfClubs_GolfClubId",
                        column: x => x.GolfClubId,
                        principalTable: "GolfClubs",
                        principalColumn: "GolfClubId");
                    table.ForeignKey(
                        name: "FK_Competitions_GolfCourses_GolfCourseId",
                        column: x => x.GolfCourseId,
                        principalTable: "GolfCourses",
                        principalColumn: "GolfCourseId");
                    table.ForeignKey(
                        name: "FK_Competitions_GolfSocieties_GolfSocietyId",
                        column: x => x.GolfSocietyId,
                        principalTable: "GolfSocieties",
                        principalColumn: "GolfSocietyId");
                });

            migrationBuilder.CreateTable(
                name: "CompetitionEntries",
                columns: table => new
                {
                    CompetitionEntryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompetitionId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    TeeSetId = table.Column<int>(type: "int", nullable: true),
                    HandicapAtEntry = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GrossScore = table.Column<int>(type: "int", nullable: true),
                    NetScore = table.Column<int>(type: "int", nullable: true),
                    StablefordPoints = table.Column<int>(type: "int", nullable: true),
                    Position = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitionEntries", x => x.CompetitionEntryId);
                    table.ForeignKey(
                        name: "FK_CompetitionEntries_Competitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalTable: "Competitions",
                        principalColumn: "CompetitionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompetitionEntries_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompetitionEntries_TeeSets_TeeSetId",
                        column: x => x.TeeSetId,
                        principalTable: "TeeSets",
                        principalColumn: "TeeSetId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rounds_CompetitionId",
                table: "Rounds",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionEntries_CompetitionId_PlayerId",
                table: "CompetitionEntries",
                columns: new[] { "CompetitionId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionEntries_PlayerId",
                table: "CompetitionEntries",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionEntries_TeeSetId",
                table: "CompetitionEntries",
                column: "TeeSetId");

            migrationBuilder.CreateIndex(
                name: "IX_Competitions_CreatedByUserId",
                table: "Competitions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Competitions_GolfClubId_Date",
                table: "Competitions",
                columns: new[] { "GolfClubId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Competitions_GolfCourseId",
                table: "Competitions",
                column: "GolfCourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Competitions_GolfSocietyId_Date",
                table: "Competitions",
                columns: new[] { "GolfSocietyId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Competitions_Status_Date",
                table: "Competitions",
                columns: new[] { "Status", "Date" });

            migrationBuilder.AddForeignKey(
                name: "FK_Rounds_Competitions_CompetitionId",
                table: "Rounds",
                column: "CompetitionId",
                principalTable: "Competitions",
                principalColumn: "CompetitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rounds_Competitions_CompetitionId",
                table: "Rounds");

            migrationBuilder.DropTable(
                name: "CompetitionEntries");

            migrationBuilder.DropTable(
                name: "Competitions");

            migrationBuilder.DropIndex(
                name: "IX_Rounds_CompetitionId",
                table: "Rounds");

            migrationBuilder.DropColumn(
                name: "CompetitionId",
                table: "Rounds");
        }
    }
}
