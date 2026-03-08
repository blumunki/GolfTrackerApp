using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GolfTrackerApp.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAiInsightsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiChatSessions",
                columns: table => new
                {
                    AiChatSessionId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationUserId = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastMessageAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiChatSessions", x => x.AiChatSessionId);
                    table.ForeignKey(
                        name: "FK_AiChatSessions_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AiAuditLogs",
                columns: table => new
                {
                    AiAuditLogId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationUserId = table.Column<string>(type: "TEXT", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResponseTimeMs = table.Column<int>(type: "INTEGER", nullable: false),
                    InsightType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ProviderName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModelUsed = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    PromptTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    CompletionTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    Success = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PromptSent = table.Column<string>(type: "TEXT", nullable: true),
                    ResponseReceived = table.Column<string>(type: "TEXT", nullable: true),
                    AiChatSessionId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiAuditLogs", x => x.AiAuditLogId);
                    table.ForeignKey(
                        name: "FK_AiAuditLogs_AiChatSessions_AiChatSessionId",
                        column: x => x.AiChatSessionId,
                        principalTable: "AiChatSessions",
                        principalColumn: "AiChatSessionId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AiAuditLogs_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AiChatSessionMessages",
                columns: table => new
                {
                    AiChatSessionMessageId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AiChatSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiChatSessionMessages", x => x.AiChatSessionMessageId);
                    table.ForeignKey(
                        name: "FK_AiChatSessionMessages_AiChatSessions_AiChatSessionId",
                        column: x => x.AiChatSessionId,
                        principalTable: "AiChatSessions",
                        principalColumn: "AiChatSessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiAuditLogs_AiChatSessionId",
                table: "AiAuditLogs",
                column: "AiChatSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AiAuditLogs_ApplicationUserId_RequestedAt",
                table: "AiAuditLogs",
                columns: new[] { "ApplicationUserId", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AiAuditLogs_RequestedAt",
                table: "AiAuditLogs",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AiChatSessionMessages_AiChatSessionId_Timestamp",
                table: "AiChatSessionMessages",
                columns: new[] { "AiChatSessionId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AiChatSessions_ApplicationUserId_LastMessageAt",
                table: "AiChatSessions",
                columns: new[] { "ApplicationUserId", "LastMessageAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiAuditLogs");

            migrationBuilder.DropTable(
                name: "AiChatSessionMessages");

            migrationBuilder.DropTable(
                name: "AiChatSessions");
        }
    }
}
