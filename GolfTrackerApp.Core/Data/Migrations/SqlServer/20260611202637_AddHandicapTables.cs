using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GolfTrackerApp.Core.Data.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddHandicapTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrimaryHandicapSource",
                table: "Players",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HandicapRecords",
                columns: table => new
                {
                    HandicapRecordId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    HandicapIndex = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    GolfSocietyId = table.Column<int>(type: "int", nullable: true),
                    GolfClubId = table.Column<int>(type: "int", nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CalculationDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsManualEntry = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandicapRecords", x => x.HandicapRecordId);
                    table.ForeignKey(
                        name: "FK_HandicapRecords_GolfClubs_GolfClubId",
                        column: x => x.GolfClubId,
                        principalTable: "GolfClubs",
                        principalColumn: "GolfClubId");
                    table.ForeignKey(
                        name: "FK_HandicapRecords_GolfSocieties_GolfSocietyId",
                        column: x => x.GolfSocietyId,
                        principalTable: "GolfSocieties",
                        principalColumn: "GolfSocietyId");
                    table.ForeignKey(
                        name: "FK_HandicapRecords_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScoringDifferentials",
                columns: table => new
                {
                    ScoringDifferentialId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    RoundId = table.Column<int>(type: "int", nullable: false),
                    TeeSetId = table.Column<int>(type: "int", nullable: false),
                    AdjustedGrossScore = table.Column<int>(type: "int", nullable: false),
                    CourseRating = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SlopeRating = table.Column<int>(type: "int", nullable: false),
                    Differential = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsUsedInCalculation = table.Column<bool>(type: "bit", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoringDifferentials", x => x.ScoringDifferentialId);
                    table.ForeignKey(
                        name: "FK_ScoringDifferentials_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScoringDifferentials_Rounds_RoundId",
                        column: x => x.RoundId,
                        principalTable: "Rounds",
                        principalColumn: "RoundId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScoringDifferentials_TeeSets_TeeSetId",
                        column: x => x.TeeSetId,
                        principalTable: "TeeSets",
                        principalColumn: "TeeSetId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HandicapRecords_GolfClubId",
                table: "HandicapRecords",
                column: "GolfClubId");

            migrationBuilder.CreateIndex(
                name: "IX_HandicapRecords_GolfSocietyId",
                table: "HandicapRecords",
                column: "GolfSocietyId");

            migrationBuilder.CreateIndex(
                name: "IX_HandicapRecords_PlayerId_Source_EffectiveDate",
                table: "HandicapRecords",
                columns: new[] { "PlayerId", "Source", "EffectiveDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ScoringDifferentials_PlayerId_CalculatedAt",
                table: "ScoringDifferentials",
                columns: new[] { "PlayerId", "CalculatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ScoringDifferentials_PlayerId_RoundId",
                table: "ScoringDifferentials",
                columns: new[] { "PlayerId", "RoundId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScoringDifferentials_RoundId",
                table: "ScoringDifferentials",
                column: "RoundId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoringDifferentials_TeeSetId",
                table: "ScoringDifferentials",
                column: "TeeSetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HandicapRecords");

            migrationBuilder.DropTable(
                name: "ScoringDifferentials");

            migrationBuilder.DropColumn(
                name: "PrimaryHandicapSource",
                table: "Players");
        }
    }
}
