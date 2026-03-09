using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GolfTrackerApp.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAiProviderSettingsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiProviderSettings",
                columns: table => new
                {
                    AiProviderSettingsId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProviderName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiProviderSettings", x => x.AiProviderSettingsId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiProviderSettings_ProviderName",
                table: "AiProviderSettings",
                column: "ProviderName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiProviderSettings");
        }
    }
}
