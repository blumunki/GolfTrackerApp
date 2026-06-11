using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GolfTrackerApp.Core.Data.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class InitialSqlServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiProviderSettings",
                columns: table => new
                {
                    AiProviderSettingsId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProviderName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiProviderSettings", x => x.AiProviderSettingsId);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ValueType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GolfClubs",
                columns: table => new
                {
                    GolfClubId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AddressLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AddressLine2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CountyOrRegion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Postcode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GolfClubs", x => x.GolfClubId);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GolfCourses",
                columns: table => new
                {
                    GolfCourseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GolfClubId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DefaultPar = table.Column<int>(type: "int", nullable: false),
                    NumberOfHoles = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GolfCourses", x => x.GolfCourseId);
                    table.ForeignKey(
                        name: "FK_GolfCourses_GolfClubs_GolfClubId",
                        column: x => x.GolfClubId,
                        principalTable: "GolfClubs",
                        principalColumn: "GolfClubId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Holes",
                columns: table => new
                {
                    HoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GolfCourseId = table.Column<int>(type: "int", nullable: false),
                    HoleNumber = table.Column<int>(type: "int", nullable: false),
                    Par = table.Column<int>(type: "int", nullable: false),
                    StrokeIndex = table.Column<int>(type: "int", nullable: true),
                    LengthYards = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Holes", x => x.HoleId);
                    table.ForeignKey(
                        name: "FK_Holes_GolfCourses_GolfCourseId",
                        column: x => x.GolfCourseId,
                        principalTable: "GolfCourses",
                        principalColumn: "GolfCourseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeeSets",
                columns: table => new
                {
                    TeeSetId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GolfCourseId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Colour = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CourseRating = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SlopeRating = table.Column<int>(type: "int", nullable: true),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
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
                name: "HoleTees",
                columns: table => new
                {
                    HoleTeeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HoleId = table.Column<int>(type: "int", nullable: false),
                    TeeSetId = table.Column<int>(type: "int", nullable: false),
                    Par = table.Column<int>(type: "int", nullable: false),
                    StrokeIndex = table.Column<int>(type: "int", nullable: true),
                    LengthYards = table.Column<int>(type: "int", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "AiAuditLogs",
                columns: table => new
                {
                    AiAuditLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResponseTimeMs = table.Column<int>(type: "int", nullable: false),
                    InsightType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProviderName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModelUsed = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PromptTokens = table.Column<int>(type: "int", nullable: false),
                    CompletionTokens = table.Column<int>(type: "int", nullable: false),
                    TotalTokens = table.Column<int>(type: "int", nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PromptSent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseReceived = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AiChatSessionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiAuditLogs", x => x.AiAuditLogId);
                });

            migrationBuilder.CreateTable(
                name: "AiChatSessionMessages",
                columns: table => new
                {
                    AiChatSessionMessageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AiChatSessionId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiChatSessionMessages", x => x.AiChatSessionMessageId);
                });

            migrationBuilder.CreateTable(
                name: "AiChatSessions",
                columns: table => new
                {
                    AiChatSessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastMessageAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiChatSessions", x => x.AiChatSessionId);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LinkedPlayerId = table.Column<int>(type: "int", nullable: true),
                    AiInsightsOptOut = table.Column<bool>(type: "bit", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClubMemberships",
                columns: table => new
                {
                    ClubMembershipId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GolfClubId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    MembershipNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                    GolfSocietyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
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
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ActionUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RelatedEntityId = table.Column<int>(type: "int", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestingUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TargetUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerConnections_AspNetUsers_RequestingUserId",
                        column: x => x.RequestingUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerConnections_AspNetUsers_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Handicap = table.Column<double>(type: "float", nullable: true),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedByApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.PlayerId);
                    table.ForeignKey(
                        name: "FK_Players_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Players_AspNetUsers_CreatedByApplicationUserId",
                        column: x => x.CreatedByApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rounds",
                columns: table => new
                {
                    RoundId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GolfCourseId = table.Column<int>(type: "int", nullable: false),
                    DatePlayed = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartingHole = table.Column<int>(type: "int", nullable: false),
                    HolesPlayed = table.Column<int>(type: "int", nullable: false),
                    RoundType = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rounds", x => x.RoundId);
                    table.ForeignKey(
                        name: "FK_Rounds_AspNetUsers_CreatedByApplicationUserId",
                        column: x => x.CreatedByApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Rounds_GolfCourses_GolfCourseId",
                        column: x => x.GolfCourseId,
                        principalTable: "GolfCourses",
                        principalColumn: "GolfCourseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SocietyMemberships",
                columns: table => new
                {
                    SocietyMembershipId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GolfSocietyId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                name: "PlayerMergeRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestingUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TargetUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SourcePlayerId = table.Column<int>(type: "int", nullable: false),
                    TargetPlayerId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RoundsMerged = table.Column<int>(type: "int", nullable: false),
                    RoundsSkipped = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerMergeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerMergeRequests_AspNetUsers_RequestingUserId",
                        column: x => x.RequestingUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerMergeRequests_AspNetUsers_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerMergeRequests_Players_SourcePlayerId",
                        column: x => x.SourcePlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerMergeRequests_Players_TargetPlayerId",
                        column: x => x.TargetPlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoundPlayers",
                columns: table => new
                {
                    RoundId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    TeeSetId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoundPlayers", x => new { x.RoundId, x.PlayerId });
                    table.ForeignKey(
                        name: "FK_RoundPlayers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoundPlayers_Rounds_RoundId",
                        column: x => x.RoundId,
                        principalTable: "Rounds",
                        principalColumn: "RoundId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoundPlayers_TeeSets_TeeSetId",
                        column: x => x.TeeSetId,
                        principalTable: "TeeSets",
                        principalColumn: "TeeSetId");
                });

            migrationBuilder.CreateTable(
                name: "Scores",
                columns: table => new
                {
                    ScoreId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoundId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    HoleId = table.Column<int>(type: "int", nullable: false),
                    Strokes = table.Column<int>(type: "int", nullable: false),
                    Putts = table.Column<int>(type: "int", nullable: true),
                    FairwayHit = table.Column<bool>(type: "bit", nullable: true),
                    TeeSetId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scores", x => x.ScoreId);
                    table.ForeignKey(
                        name: "FK_Scores_Holes_HoleId",
                        column: x => x.HoleId,
                        principalTable: "Holes",
                        principalColumn: "HoleId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Scores_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Scores_Rounds_RoundId",
                        column: x => x.RoundId,
                        principalTable: "Rounds",
                        principalColumn: "RoundId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Scores_TeeSets_TeeSetId",
                        column: x => x.TeeSetId,
                        principalTable: "TeeSets",
                        principalColumn: "TeeSetId");
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

            migrationBuilder.CreateIndex(
                name: "IX_AiProviderSettings_ProviderName",
                table: "AiProviderSettings",
                column: "ProviderName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationSettings_Key",
                table: "ApplicationSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_LinkedPlayerId",
                table: "AspNetUsers",
                column: "LinkedPlayerId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

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
                name: "IX_GolfCourses_GolfClubId",
                table: "GolfCourses",
                column: "GolfClubId");

            migrationBuilder.CreateIndex(
                name: "IX_GolfSocieties_CreatedByUserId",
                table: "GolfSocieties",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Holes_GolfCourseId",
                table: "Holes",
                column: "GolfCourseId");

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
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerConnections_RequestingUserId_TargetUserId",
                table: "PlayerConnections",
                columns: new[] { "RequestingUserId", "TargetUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerConnections_TargetUserId",
                table: "PlayerConnections",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMergeRequests_RequestingUserId",
                table: "PlayerMergeRequests",
                column: "RequestingUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMergeRequests_SourcePlayerId",
                table: "PlayerMergeRequests",
                column: "SourcePlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMergeRequests_TargetPlayerId",
                table: "PlayerMergeRequests",
                column: "TargetPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMergeRequests_TargetUserId",
                table: "PlayerMergeRequests",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_ApplicationUserId",
                table: "Players",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_CreatedByApplicationUserId",
                table: "Players",
                column: "CreatedByApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoundPlayers_PlayerId",
                table: "RoundPlayers",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_RoundPlayers_TeeSetId",
                table: "RoundPlayers",
                column: "TeeSetId");

            migrationBuilder.CreateIndex(
                name: "IX_Rounds_CreatedByApplicationUserId",
                table: "Rounds",
                column: "CreatedByApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Rounds_GolfCourseId",
                table: "Rounds",
                column: "GolfCourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_HoleId",
                table: "Scores",
                column: "HoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_PlayerId",
                table: "Scores",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_RoundId",
                table: "Scores",
                column: "RoundId");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_TeeSetId",
                table: "Scores",
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
                name: "FK_AiAuditLogs_AiChatSessions_AiChatSessionId",
                table: "AiAuditLogs",
                column: "AiChatSessionId",
                principalTable: "AiChatSessions",
                principalColumn: "AiChatSessionId",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_AiAuditLogs_AspNetUsers_ApplicationUserId",
                table: "AiAuditLogs",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AiChatSessionMessages_AiChatSessions_AiChatSessionId",
                table: "AiChatSessionMessages",
                column: "AiChatSessionId",
                principalTable: "AiChatSessions",
                principalColumn: "AiChatSessionId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AiChatSessions_AspNetUsers_ApplicationUserId",
                table: "AiChatSessions",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Players_LinkedPlayerId",
                table: "AspNetUsers",
                column: "LinkedPlayerId",
                principalTable: "Players",
                principalColumn: "PlayerId",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Players_AspNetUsers_ApplicationUserId",
                table: "Players");

            migrationBuilder.DropForeignKey(
                name: "FK_Players_AspNetUsers_CreatedByApplicationUserId",
                table: "Players");

            migrationBuilder.DropTable(
                name: "AiAuditLogs");

            migrationBuilder.DropTable(
                name: "AiChatSessionMessages");

            migrationBuilder.DropTable(
                name: "AiProviderSettings");

            migrationBuilder.DropTable(
                name: "ApplicationSettings");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "ClubMemberships");

            migrationBuilder.DropTable(
                name: "HoleTees");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PlayerConnections");

            migrationBuilder.DropTable(
                name: "PlayerMergeRequests");

            migrationBuilder.DropTable(
                name: "RoundPlayers");

            migrationBuilder.DropTable(
                name: "Scores");

            migrationBuilder.DropTable(
                name: "SocietyMemberships");

            migrationBuilder.DropTable(
                name: "AiChatSessions");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Holes");

            migrationBuilder.DropTable(
                name: "Rounds");

            migrationBuilder.DropTable(
                name: "TeeSets");

            migrationBuilder.DropTable(
                name: "GolfSocieties");

            migrationBuilder.DropTable(
                name: "GolfCourses");

            migrationBuilder.DropTable(
                name: "GolfClubs");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Players");
        }
    }
}
