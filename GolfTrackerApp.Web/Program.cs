using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GolfTrackerApp.Web.Components;
using GolfTrackerApp.Web.Components.Account;
using GolfTrackerApp.Web.Data;
using GolfTrackerApp.Web.Services;
using GolfTrackerApp.Web.Models;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = true;
    });

// Add API controllers support (for mobile app)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configure JSON options for API responses
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => 
{
    options.SignIn.RequireConfirmedAccount = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddSignInManager()
.AddDefaultTokenProviders();

// Add Google Authentication (only if credentials are configured)
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGoogle(googleOptions =>
        {
            googleOptions.ClientId = googleClientId;
            googleOptions.ClientSecret = googleClientSecret;
        });
}

// Add JWT Authentication for mobile app API
var jwtKey = builder.Configuration["Jwt:Key"] 
    ?? throw new InvalidOperationException("JWT signing key must be configured via 'Jwt:Key'. See appsettings.json.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "GolfTrackerApp";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "GolfTrackerApp";

builder.Services.AddAuthentication()
    .AddJwtBearer("ApiAuth", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = "GolfTrackerAuth";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.Configure<CircuitOptions>(options => options.DetailedErrors = true);

// Configure database provider based on environment
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var databaseProvider = builder.Configuration["DatabaseProvider"] ?? "Sqlite";

// Use DbContextFactory to avoid threading issues in Blazor Server
// The lifetime parameter also registers ApplicationDbContext as a scoped service (for Identity)
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
{
    if (databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            // Enable retry on failure for transient errors
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
            sqlOptions.CommandTimeout(60);
        });
    }
    else
    {
        options.UseSqlite(connectionString);
    }
}, ServiceLifetime.Scoped);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

builder.Services.AddScoped<IGolfCourseService, GolfCourseService>();
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IGolfClubService, GolfClubService>();
builder.Services.AddScoped<IHoleService, HoleService>();
builder.Services.AddScoped<IRoundService, RoundService>();
builder.Services.AddScoped<IScoreService, ScoreService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IRoundWorkflowService, RoundWorkflowService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IConnectionService, ConnectionService>();
builder.Services.AddScoped<IMergeService, MergeService>();
builder.Services.AddScoped<ITeeSetService, TeeSetService>();
builder.Services.AddScoped<IGolfSocietyService, GolfSocietyService>();
builder.Services.AddScoped<IClubMembershipService, ClubMembershipService>();

// AI Insights services
builder.Services.AddScoped<IAiInsightService, AiInsightService>();
builder.Services.AddScoped<IAiRoutingService, AiRoutingService>();
builder.Services.AddScoped<IAiAuditService, AiAuditService>();
builder.Services.AddScoped<IAiChatService, AiChatService>();
builder.Services.AddScoped<IAiProviderSettingsService, AiProviderSettingsService>();
builder.Services.AddScoped<IApplicationSettingsService, ApplicationSettingsService>();
builder.Services.AddHttpClient("AiProvider_OpenAI");
builder.Services.AddHttpClient("AiProvider_Anthropic");
builder.Services.AddHttpClient("AiProvider_Gemini");
builder.Services.AddHttpClient("AiProvider_Grok");
builder.Services.AddHttpClient("AiProvider_Mistral");
builder.Services.AddHttpClient("AiProvider_DeepSeek");
builder.Services.AddHttpClient("AiProvider_MetaLlama");
builder.Services.AddHttpClient("AiProvider_Manus");

builder.Services.AddMudServices();

var app = builder.Build();

// Initialize database (migrations and seeding)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var dbProvider = builder.Configuration["DatabaseProvider"] ?? "Sqlite";
    
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        if (dbProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            // For SQL Server (production), ensure the database exists first
            logger.LogInformation("Ensuring SQL Server database schema exists...");
            context.Database.EnsureCreated();
            
            // Check and create missing tables for new features
            await EnsureNewTablesExistAsync(context, logger);
            
            logger.LogInformation("SQL Server database schema ready.");
        }
        else
        {
            // For SQLite (development), use migrations
            logger.LogInformation("Applying SQLite database migrations...");
            context.Database.Migrate();
            logger.LogInformation("SQLite database migrations applied successfully.");
        }

        // Seed roles and admin user
        logger.LogInformation("Seeding database...");
        await SeedData.InitializeAsync(services);
        logger.LogInformation("Database seeding completed.");

        // Seed AI provider settings from config if table is empty
        var providerSettingsService = services.GetRequiredService<IAiProviderSettingsService>();
        await providerSettingsService.SeedFromConfigAsync();

        // Seed application settings with defaults
        var appSettingsService = services.GetRequiredService<IApplicationSettingsService>();
        await appSettingsService.SeedDefaultsAsync();

        // Seed default tee sets for courses that have holes but no tee sets
        var teeSetService = services.GetRequiredService<ITeeSetService>();
        await teeSetService.SeedDefaultTeeSetsAsync();

        // Cleanup old AI audit logs based on retention policy
        var retentionDays = app.Configuration.GetValue<int>("AiInsights:AuditLogging:RetentionDays");
        if (retentionDays > 0)
        {
            var auditService = services.GetRequiredService<IAiAuditService>();
            await auditService.CleanupOldLogsAsync(retentionDays);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database initialization.");
        // In production, you may want to throw to prevent app from starting with bad DB state
        if (!app.Environment.IsDevelopment())
        {
            throw;
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

// Map API controllers (for mobile app)
app.MapControllers();

app.Run();

/// <summary>
/// Ensures new tables exist in SQL Server that were added after initial EnsureCreated.
/// EnsureCreated only works when the database doesn't exist at all.
/// This method checks for and creates missing tables.
/// </summary>
static async Task EnsureNewTablesExistAsync(ApplicationDbContext context, ILogger logger)
{
    var connection = context.Database.GetDbConnection();
    await connection.OpenAsync();
    
    try
    {
        // Check if Notifications table exists
        var notificationsExists = await TableExistsAsync(connection, "Notifications");
        if (!notificationsExists)
        {
            logger.LogInformation("Creating Notifications table...");
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE [Notifications] (
                    [Id] INT IDENTITY(1,1) NOT NULL,
                    [UserId] NVARCHAR(450) NOT NULL,
                    [Type] INT NOT NULL,
                    [Title] NVARCHAR(100) NOT NULL,
                    [Message] NVARCHAR(500) NOT NULL,
                    [ActionUrl] NVARCHAR(200) NULL,
                    [RelatedEntityId] INT NULL,
                    [IsRead] BIT NOT NULL,
                    [CreatedAt] DATETIME2 NOT NULL,
                    CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_Notifications_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
                );
                CREATE INDEX [IX_Notifications_UserId_IsRead_CreatedAt] ON [Notifications] ([UserId], [IsRead], [CreatedAt]);
            ");
            logger.LogInformation("Notifications table created.");
        }

        // Check if PlayerConnections table exists
        var connectionsExists = await TableExistsAsync(connection, "PlayerConnections");
        if (!connectionsExists)
        {
            logger.LogInformation("Creating PlayerConnections table...");
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE [PlayerConnections] (
                    [Id] INT IDENTITY(1,1) NOT NULL,
                    [RequestingUserId] NVARCHAR(450) NOT NULL,
                    [TargetUserId] NVARCHAR(450) NOT NULL,
                    [Status] INT NOT NULL,
                    [RequestedAt] DATETIME2 NOT NULL,
                    [RespondedAt] DATETIME2 NULL,
                    CONSTRAINT [PK_PlayerConnections] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_PlayerConnections_AspNetUsers_RequestingUserId] FOREIGN KEY ([RequestingUserId]) REFERENCES [AspNetUsers]([Id]),
                    CONSTRAINT [FK_PlayerConnections_AspNetUsers_TargetUserId] FOREIGN KEY ([TargetUserId]) REFERENCES [AspNetUsers]([Id])
                );
                CREATE UNIQUE INDEX [IX_PlayerConnections_RequestingUserId_TargetUserId] ON [PlayerConnections] ([RequestingUserId], [TargetUserId]);
                CREATE INDEX [IX_PlayerConnections_TargetUserId] ON [PlayerConnections] ([TargetUserId]);
            ");
            logger.LogInformation("PlayerConnections table created.");
        }

        // Check if LinkedPlayerId column exists on AspNetUsers
        var linkedPlayerIdExists = await ColumnExistsAsync(connection, "AspNetUsers", "LinkedPlayerId");
        if (!linkedPlayerIdExists)
        {
            logger.LogInformation("Adding LinkedPlayerId column to AspNetUsers...");
            await context.Database.ExecuteSqlRawAsync(@"
                ALTER TABLE [AspNetUsers] ADD [LinkedPlayerId] INT NULL;
                CREATE INDEX [IX_AspNetUsers_LinkedPlayerId] ON [AspNetUsers] ([LinkedPlayerId]);
                ALTER TABLE [AspNetUsers] ADD CONSTRAINT [FK_AspNetUsers_Players_LinkedPlayerId] 
                    FOREIGN KEY ([LinkedPlayerId]) REFERENCES [Players]([PlayerId]) ON DELETE NO ACTION;
            ");
            logger.LogInformation("LinkedPlayerId column added to AspNetUsers.");
        }

        // Check if PlayerMergeRequests table exists
        var mergeRequestsExists = await TableExistsAsync(connection, "PlayerMergeRequests");
        if (!mergeRequestsExists)
        {
            logger.LogInformation("Creating PlayerMergeRequests table...");
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE [PlayerMergeRequests] (
                    [Id] INT IDENTITY(1,1) NOT NULL,
                    [RequestingUserId] NVARCHAR(450) NOT NULL,
                    [TargetUserId] NVARCHAR(450) NOT NULL,
                    [SourcePlayerId] INT NOT NULL,
                    [TargetPlayerId] INT NOT NULL,
                    [Status] INT NOT NULL,
                    [RequestedAt] DATETIME2 NOT NULL,
                    [CompletedAt] DATETIME2 NULL,
                    [Message] NVARCHAR(500) NULL,
                    [RoundsMerged] INT NOT NULL,
                    [RoundsSkipped] INT NOT NULL,
                    CONSTRAINT [PK_PlayerMergeRequests] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_PlayerMergeRequests_AspNetUsers_RequestingUserId] FOREIGN KEY ([RequestingUserId]) REFERENCES [AspNetUsers]([Id]),
                    CONSTRAINT [FK_PlayerMergeRequests_AspNetUsers_TargetUserId] FOREIGN KEY ([TargetUserId]) REFERENCES [AspNetUsers]([Id]),
                    CONSTRAINT [FK_PlayerMergeRequests_Players_SourcePlayerId] FOREIGN KEY ([SourcePlayerId]) REFERENCES [Players]([PlayerId]),
                    CONSTRAINT [FK_PlayerMergeRequests_Players_TargetPlayerId] FOREIGN KEY ([TargetPlayerId]) REFERENCES [Players]([PlayerId])
                );
                CREATE INDEX [IX_PlayerMergeRequests_RequestingUserId] ON [PlayerMergeRequests] ([RequestingUserId]);
                CREATE INDEX [IX_PlayerMergeRequests_SourcePlayerId] ON [PlayerMergeRequests] ([SourcePlayerId]);
                CREATE INDEX [IX_PlayerMergeRequests_TargetPlayerId] ON [PlayerMergeRequests] ([TargetPlayerId]);
                CREATE INDEX [IX_PlayerMergeRequests_TargetUserId] ON [PlayerMergeRequests] ([TargetUserId]);
            ");
            logger.LogInformation("PlayerMergeRequests table created.");
        }

        // Check if AiChatSessions table exists
        if (!await TableExistsAsync(connection, "AiChatSessions"))
        {
            logger.LogInformation("Creating AiChatSessions table...");
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE [AiChatSessions] (
                    [AiChatSessionId] INT IDENTITY(1,1) NOT NULL,
                    [ApplicationUserId] NVARCHAR(450) NOT NULL,
                    [Title] NVARCHAR(100) NULL,
                    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                    [LastMessageAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                    [IsArchived] BIT NOT NULL DEFAULT 0,
                    CONSTRAINT [PK_AiChatSessions] PRIMARY KEY ([AiChatSessionId]),
                    CONSTRAINT [FK_AiChatSessions_AspNetUsers] FOREIGN KEY ([ApplicationUserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
                );
                CREATE INDEX [IX_AiChatSessions_UserId_LastMessage] ON [AiChatSessions] ([ApplicationUserId], [LastMessageAt]);
            ");
            logger.LogInformation("AiChatSessions table created.");
        }

        // Check if AiChatSessionMessages table exists
        if (!await TableExistsAsync(connection, "AiChatSessionMessages"))
        {
            logger.LogInformation("Creating AiChatSessionMessages table...");
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE [AiChatSessionMessages] (
                    [AiChatSessionMessageId] INT IDENTITY(1,1) NOT NULL,
                    [AiChatSessionId] INT NOT NULL,
                    [Role] NVARCHAR(20) NOT NULL,
                    [Content] NVARCHAR(MAX) NOT NULL,
                    [Timestamp] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                    CONSTRAINT [PK_AiChatSessionMessages] PRIMARY KEY ([AiChatSessionMessageId]),
                    CONSTRAINT [FK_AiChatSessionMessages_Session] FOREIGN KEY ([AiChatSessionId]) REFERENCES [AiChatSessions]([AiChatSessionId]) ON DELETE CASCADE
                );
                CREATE INDEX [IX_AiChatSessionMessages_SessionId_Timestamp] ON [AiChatSessionMessages] ([AiChatSessionId], [Timestamp]);
            ");
            logger.LogInformation("AiChatSessionMessages table created.");
        }

        // Check if AiAuditLogs table exists
        if (!await TableExistsAsync(connection, "AiAuditLogs"))
        {
            logger.LogInformation("Creating AiAuditLogs table...");
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE [AiAuditLogs] (
                    [AiAuditLogId] INT IDENTITY(1,1) NOT NULL,
                    [ApplicationUserId] NVARCHAR(450) NOT NULL,
                    [RequestedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                    [ResponseTimeMs] INT NOT NULL DEFAULT 0,
                    [InsightType] NVARCHAR(50) NOT NULL,
                    [ProviderName] NVARCHAR(50) NULL,
                    [ModelUsed] NVARCHAR(50) NULL,
                    [PromptTokens] INT NOT NULL DEFAULT 0,
                    [CompletionTokens] INT NOT NULL DEFAULT 0,
                    [TotalTokens] INT NOT NULL DEFAULT 0,
                    [Success] BIT NOT NULL DEFAULT 0,
                    [ErrorMessage] NVARCHAR(500) NULL,
                    [PromptSent] NVARCHAR(MAX) NULL,
                    [ResponseReceived] NVARCHAR(MAX) NULL,
                    [AiChatSessionId] INT NULL,
                    CONSTRAINT [PK_AiAuditLogs] PRIMARY KEY ([AiAuditLogId]),
                    CONSTRAINT [FK_AiAuditLogs_AspNetUsers] FOREIGN KEY ([ApplicationUserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_AiAuditLogs_AiChatSession] FOREIGN KEY ([AiChatSessionId]) REFERENCES [AiChatSessions]([AiChatSessionId]) ON DELETE NO ACTION
                );
                CREATE INDEX [IX_AiAuditLogs_UserId_RequestedAt] ON [AiAuditLogs] ([ApplicationUserId], [RequestedAt]);
                CREATE INDEX [IX_AiAuditLogs_RequestedAt] ON [AiAuditLogs] ([RequestedAt]);
            ");
            logger.LogInformation("AiAuditLogs table created.");
        }

        // Check if AiProviderSettings table exists
        if (!await TableExistsAsync(connection, "AiProviderSettings"))
        {
            logger.LogInformation("Creating AiProviderSettings table...");
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE [AiProviderSettings] (
                    [AiProviderSettingsId] INT IDENTITY(1,1) NOT NULL,
                    [ProviderName] NVARCHAR(50) NOT NULL,
                    [Enabled] BIT NOT NULL DEFAULT 0,
                    [Priority] INT NOT NULL DEFAULT 99,
                    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                    [UpdatedByUserId] NVARCHAR(450) NULL,
                    CONSTRAINT [PK_AiProviderSettings] PRIMARY KEY ([AiProviderSettingsId])
                );
                CREATE UNIQUE INDEX [IX_AiProviderSettings_ProviderName] ON [AiProviderSettings] ([ProviderName]);
            ");
            logger.LogInformation("AiProviderSettings table created.");
        }

        // Check if AiInsightsOptOut column exists on AspNetUsers
        var aiOptOutExists = await ColumnExistsAsync(connection, "AspNetUsers", "AiInsightsOptOut");
        if (!aiOptOutExists)
        {
            logger.LogInformation("Adding AiInsightsOptOut column to AspNetUsers...");
            await context.Database.ExecuteSqlRawAsync(@"
                ALTER TABLE [AspNetUsers] ADD [AiInsightsOptOut] BIT NOT NULL DEFAULT 0;
            ");
            logger.LogInformation("AiInsightsOptOut column added to AspNetUsers.");
        }

        // Check if ApplicationSettings table exists
        if (!await TableExistsAsync(connection, "ApplicationSettings"))
        {
            logger.LogInformation("Creating ApplicationSettings table...");
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE [ApplicationSettings] (
                    [Id] INT IDENTITY(1,1) NOT NULL,
                    [Key] NVARCHAR(100) NOT NULL,
                    [Value] NVARCHAR(500) NOT NULL,
                    [Description] NVARCHAR(200) NULL,
                    [Category] NVARCHAR(50) NOT NULL DEFAULT 'General',
                    [ValueType] NVARCHAR(20) NOT NULL DEFAULT 'string',
                    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                    [UpdatedByUserId] NVARCHAR(450) NULL,
                    CONSTRAINT [PK_ApplicationSettings] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [IX_ApplicationSettings_Key] ON [ApplicationSettings] ([Key]);
            ");
            logger.LogInformation("ApplicationSettings table created.");
        }

        // -- Phase 1: Tee Sets & Course Ratings --

        if (!await TableExistsAsync(connection, "TeeSets"))
        {
            logger.LogInformation("Creating TeeSets table...");
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE [TeeSets] (
                    [TeeSetId] INT IDENTITY(1,1) NOT NULL,
                    [GolfCourseId] INT NOT NULL,
                    [Name] NVARCHAR(50) NOT NULL,
                    [Colour] NVARCHAR(7) NOT NULL DEFAULT '#FFD700',
                    [CourseRating] DECIMAL(4,1) NULL,
                    [SlopeRating] INT NULL,
                    [Gender] INT NOT NULL DEFAULT 0,
                    [SortOrder] INT NOT NULL DEFAULT 0,
                    CONSTRAINT [PK_TeeSets] PRIMARY KEY ([TeeSetId]),
                    CONSTRAINT [FK_TeeSets_GolfCourses] FOREIGN KEY ([GolfCourseId]) REFERENCES [GolfCourses]([GolfCourseId]) ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX [IX_TeeSets_GolfCourseId_Name] ON [TeeSets] ([GolfCourseId], [Name]);
            ");
            logger.LogInformation("TeeSets table created.");
        }

        if (!await TableExistsAsync(connection, "HoleTees"))
        {
            logger.LogInformation("Creating HoleTees table...");
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE [HoleTees] (
                    [HoleTeeId] INT IDENTITY(1,1) NOT NULL,
                    [HoleId] INT NOT NULL,
                    [TeeSetId] INT NOT NULL,
                    [Par] INT NOT NULL,
                    [StrokeIndex] INT NULL,
                    [LengthYards] INT NULL,
                    CONSTRAINT [PK_HoleTees] PRIMARY KEY ([HoleTeeId]),
                    CONSTRAINT [FK_HoleTees_Holes] FOREIGN KEY ([HoleId]) REFERENCES [Holes]([HoleId]) ON DELETE CASCADE,
                    CONSTRAINT [FK_HoleTees_TeeSets] FOREIGN KEY ([TeeSetId]) REFERENCES [TeeSets]([TeeSetId]) ON DELETE NO ACTION
                );
                CREATE UNIQUE INDEX [IX_HoleTees_HoleId_TeeSetId] ON [HoleTees] ([HoleId], [TeeSetId]);
                CREATE INDEX [IX_HoleTees_TeeSetId] ON [HoleTees] ([TeeSetId]);
            ");
            logger.LogInformation("HoleTees table created.");
        }

        // Add TeeSetId columns to Scores and RoundPlayers
        if (!await ColumnExistsAsync(connection, "Scores", "TeeSetId"))
        {
            logger.LogInformation("Adding TeeSetId column to Scores...");
            await context.Database.ExecuteSqlRawAsync(@"
                ALTER TABLE [Scores] ADD [TeeSetId] INT NULL;
                CREATE INDEX [IX_Scores_TeeSetId] ON [Scores] ([TeeSetId]);
                ALTER TABLE [Scores] ADD CONSTRAINT [FK_Scores_TeeSets] FOREIGN KEY ([TeeSetId]) REFERENCES [TeeSets]([TeeSetId]) ON DELETE SET NULL;
            ");
            logger.LogInformation("TeeSetId column added to Scores.");
        }

        if (!await ColumnExistsAsync(connection, "RoundPlayers", "TeeSetId"))
        {
            logger.LogInformation("Adding TeeSetId column to RoundPlayers...");
            await context.Database.ExecuteSqlRawAsync(@"
                ALTER TABLE [RoundPlayers] ADD [TeeSetId] INT NULL;
                CREATE INDEX [IX_RoundPlayers_TeeSetId] ON [RoundPlayers] ([TeeSetId]);
                ALTER TABLE [RoundPlayers] ADD CONSTRAINT [FK_RoundPlayers_TeeSets] FOREIGN KEY ([TeeSetId]) REFERENCES [TeeSets]([TeeSetId]) ON DELETE SET NULL;
            ");
            logger.LogInformation("TeeSetId column added to RoundPlayers.");
        }

        // -- Phase 2: Golf Societies & Memberships --

        if (!await TableExistsAsync(connection, "GolfSocieties"))
        {
            logger.LogInformation("Creating GolfSocieties table...");
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE [GolfSocieties] (
                    [GolfSocietyId] INT IDENTITY(1,1) NOT NULL,
                    [Name] NVARCHAR(100) NOT NULL,
                    [Description] NVARCHAR(500) NULL,
                    [CreatedByUserId] NVARCHAR(450) NOT NULL,
                    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                    [IsActive] BIT NOT NULL DEFAULT 1,
                    CONSTRAINT [PK_GolfSocieties] PRIMARY KEY ([GolfSocietyId]),
                    CONSTRAINT [FK_GolfSocieties_AspNetUsers] FOREIGN KEY ([CreatedByUserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION
                );
            ");
            logger.LogInformation("GolfSocieties table created.");
        }

        if (!await TableExistsAsync(connection, "SocietyMemberships"))
        {
            logger.LogInformation("Creating SocietyMemberships table...");
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE [SocietyMemberships] (
                    [SocietyMembershipId] INT IDENTITY(1,1) NOT NULL,
                    [GolfSocietyId] INT NOT NULL,
                    [UserId] NVARCHAR(450) NOT NULL,
                    [Role] INT NOT NULL DEFAULT 0,
                    [JoinedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                    CONSTRAINT [PK_SocietyMemberships] PRIMARY KEY ([SocietyMembershipId]),
                    CONSTRAINT [FK_SocietyMemberships_GolfSocieties] FOREIGN KEY ([GolfSocietyId]) REFERENCES [GolfSocieties]([GolfSocietyId]) ON DELETE CASCADE,
                    CONSTRAINT [FK_SocietyMemberships_AspNetUsers] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX [IX_SocietyMemberships_GolfSocietyId_UserId] ON [SocietyMemberships] ([GolfSocietyId], [UserId]);
                CREATE INDEX [IX_SocietyMemberships_UserId] ON [SocietyMemberships] ([UserId]);
            ");
            logger.LogInformation("SocietyMemberships table created.");
        }

        if (!await TableExistsAsync(connection, "ClubMemberships"))
        {
            logger.LogInformation("Creating ClubMemberships table...");
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE [ClubMemberships] (
                    [ClubMembershipId] INT IDENTITY(1,1) NOT NULL,
                    [GolfClubId] INT NOT NULL,
                    [UserId] NVARCHAR(450) NOT NULL,
                    [Role] INT NOT NULL DEFAULT 0,
                    [MembershipNumber] NVARCHAR(50) NULL,
                    [JoinedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                    CONSTRAINT [PK_ClubMemberships] PRIMARY KEY ([ClubMembershipId]),
                    CONSTRAINT [FK_ClubMemberships_GolfClubs] FOREIGN KEY ([GolfClubId]) REFERENCES [GolfClubs]([GolfClubId]) ON DELETE CASCADE,
                    CONSTRAINT [FK_ClubMemberships_AspNetUsers] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX [IX_ClubMemberships_GolfClubId_UserId] ON [ClubMemberships] ([GolfClubId], [UserId]);
                CREATE INDEX [IX_ClubMemberships_UserId] ON [ClubMemberships] ([UserId]);
            ");
            logger.LogInformation("ClubMemberships table created.");
        }
    }
    finally
    {
        await connection.CloseAsync();
    }
}

static async Task<bool> TableExistsAsync(System.Data.Common.DbConnection connection, string tableName)
{
    using var command = connection.CreateCommand();
    command.CommandText = $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}'";
    var result = await command.ExecuteScalarAsync();
    return Convert.ToInt32(result) > 0;
}

static async Task<bool> ColumnExistsAsync(System.Data.Common.DbConnection connection, string tableName, string columnName)
{
    using var command = connection.CreateCommand();
    command.CommandText = $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{columnName}'";
    var result = await command.ExecuteScalarAsync();
    return Convert.ToInt32(result) > 0;
}