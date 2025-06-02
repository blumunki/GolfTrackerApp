using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GolfTrackerApp.Web.Migrations
{
    /// <inheritdoc />
    public partial class MakeOwnershipFieldsRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if the active provider is SQLite
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                migrationBuilder.Sql("PRAGMA foreign_keys = OFF;");
            }
            migrationBuilder.DropForeignKey(
                name: "FK_Players_AspNetUsers_CreatedByApplicationUserId",
                table: "Players");

            migrationBuilder.DropForeignKey(
                name: "FK_Rounds_AspNetUsers_CreatedByApplicationUserId",
                table: "Rounds");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedByApplicationUserId",
                table: "Rounds",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedByApplicationUserId",
                table: "Players",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Players_AspNetUsers_CreatedByApplicationUserId",
                table: "Players",
                column: "CreatedByApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Rounds_AspNetUsers_CreatedByApplicationUserId",
                table: "Rounds",
                column: "CreatedByApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
            
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                migrationBuilder.Sql("PRAGMA foreign_keys = ON;");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                migrationBuilder.Sql("PRAGMA foreign_keys = OFF;");
            }

            migrationBuilder.DropForeignKey(
                name: "FK_Players_AspNetUsers_CreatedByApplicationUserId",
                table: "Players");

            migrationBuilder.DropForeignKey(
                name: "FK_Rounds_AspNetUsers_CreatedByApplicationUserId",
                table: "Rounds");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedByApplicationUserId",
                table: "Rounds",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedByApplicationUserId",
                table: "Players",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddForeignKey(
                name: "FK_Players_AspNetUsers_CreatedByApplicationUserId",
                table: "Players",
                column: "CreatedByApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Rounds_AspNetUsers_CreatedByApplicationUserId",
                table: "Rounds",
                column: "CreatedByApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                migrationBuilder.Sql("PRAGMA foreign_keys = ON;");
            }
        }
    }
}
