using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GolfTrackerApp.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddGolfClubAndLinkToCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "GolfCourses");

            migrationBuilder.AddColumn<int>(
                name: "GolfClubId",
                table: "GolfCourses",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "GolfClubs",
                columns: table => new
                {
                    GolfClubId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AddressLine1 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    AddressLine2 = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CountyOrRegion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Postcode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Country = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Website = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GolfClubs", x => x.GolfClubId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GolfCourses_GolfClubId",
                table: "GolfCourses",
                column: "GolfClubId");

            migrationBuilder.AddForeignKey(
                name: "FK_GolfCourses_GolfClubs_GolfClubId",
                table: "GolfCourses",
                column: "GolfClubId",
                principalTable: "GolfClubs",
                principalColumn: "GolfClubId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GolfCourses_GolfClubs_GolfClubId",
                table: "GolfCourses");

            migrationBuilder.DropTable(
                name: "GolfClubs");

            migrationBuilder.DropIndex(
                name: "IX_GolfCourses_GolfClubId",
                table: "GolfCourses");

            migrationBuilder.DropColumn(
                name: "GolfClubId",
                table: "GolfCourses");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "GolfCourses",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }
    }
}
