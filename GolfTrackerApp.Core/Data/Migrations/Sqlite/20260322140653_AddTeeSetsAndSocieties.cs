using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GolfTrackerApp.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTeeSetsAndSocieties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TeeSetId",
                table: "Scores",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeeSetId",
                table: "RoundPlayers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClubMemberships",
                columns: table => new
                {
                    ClubMembershipId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GolfClubId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    MembershipNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    JoinedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubMemberships", x => x.ClubMembershipId);
                    table.ForeignKey(
                        name: "FK_ClubMemberships_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClubMemberships_GolfClubs_GolfClubId",
                        column: x => x.GolfClubId,
                        principalTable: "GolfClubs",
                        principalColumn: "GolfClubId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GolfSocieties",
                columns: table => new
                {
                    GolfSocietyId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GolfSocieties", x => x.GolfSocietyId);
                    table.ForeignKey(
                        name: "FK_GolfSocieties_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeeSets",
                columns: table => new
                {
                    TeeSetId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GolfCourseId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Colour = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CourseRating = table.Column<decimal>(type: "TEXT", nullable: true),
                    SlopeRating = table.Column<int>(type: "INTEGER", nullable: true),
                    Gender = table.Column<int>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeeSets", x => x.TeeSetId);
                    table.ForeignKey(
                        name: "FK_TeeSets_GolfCourses_GolfCourseId",
                        column: x => x.GolfCourseId,
                        principalTable: "GolfCourses",
                        principalColumn: "GolfCourseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SocietyMemberships",
                columns: table => new
                {
                    SocietyMembershipId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GolfSocietyId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocietyMemberships", x => x.SocietyMembershipId);
                    table.ForeignKey(
                        name: "FK_SocietyMemberships_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SocietyMemberships_GolfSocieties_GolfSocietyId",
                        column: x => x.GolfSocietyId,
                        principalTable: "GolfSocieties",
                        principalColumn: "GolfSocietyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HoleTees",
                columns: table => new
                {
                    HoleTeeId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HoleId = table.Column<int>(type: "INTEGER", nullable: false),
                    TeeSetId = table.Column<int>(type: "INTEGER", nullable: false),
                    Par = table.Column<int>(type: "INTEGER", nullable: false),
                    StrokeIndex = table.Column<int>(type: "INTEGER", nullable: true),
                    LengthYards = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoleTees", x => x.HoleTeeId);
                    table.ForeignKey(
                        name: "FK_HoleTees_Holes_HoleId",
                        column: x => x.HoleId,
                        principalTable: "Holes",
                        principalColumn: "HoleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HoleTees_TeeSets_TeeSetId",
                        column: x => x.TeeSetId,
                        principalTable: "TeeSets",
                        principalColumn: "TeeSetId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Scores_TeeSetId",
                table: "Scores",
                column: "TeeSetId");

            migrationBuilder.CreateIndex(
                name: "IX_RoundPlayers_TeeSetId",
                table: "RoundPlayers",
                column: "TeeSetId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubMemberships_GolfClubId_UserId",
                table: "ClubMemberships",
                columns: new[] { "GolfClubId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClubMemberships_UserId",
                table: "ClubMemberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GolfSocieties_CreatedByUserId",
                table: "GolfSocieties",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HoleTees_HoleId_TeeSetId",
                table: "HoleTees",
                columns: new[] { "HoleId", "TeeSetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HoleTees_TeeSetId",
                table: "HoleTees",
                column: "TeeSetId");

            migrationBuilder.CreateIndex(
                name: "IX_SocietyMemberships_GolfSocietyId_UserId",
                table: "SocietyMemberships",
                columns: new[] { "GolfSocietyId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SocietyMemberships_UserId",
                table: "SocietyMemberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TeeSets_GolfCourseId_Name",
                table: "TeeSets",
                columns: new[] { "GolfCourseId", "Name" },
                unique: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoundPlayers_TeeSets_TeeSetId",
                table: "RoundPlayers");

            migrationBuilder.DropForeignKey(
                name: "FK_Scores_TeeSets_TeeSetId",
                table: "Scores");

            migrationBuilder.DropTable(
                name: "ClubMemberships");

            migrationBuilder.DropTable(
                name: "HoleTees");

            migrationBuilder.DropTable(
                name: "SocietyMemberships");

            migrationBuilder.DropTable(
                name: "TeeSets");

            migrationBuilder.DropTable(
                name: "GolfSocieties");

            migrationBuilder.DropIndex(
                name: "IX_Scores_TeeSetId",
                table: "Scores");

            migrationBuilder.DropIndex(
                name: "IX_RoundPlayers_TeeSetId",
                table: "RoundPlayers");

            migrationBuilder.DropColumn(
                name: "TeeSetId",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "TeeSetId",
                table: "RoundPlayers");
        }
    }
}
