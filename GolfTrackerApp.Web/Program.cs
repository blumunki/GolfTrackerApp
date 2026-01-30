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
var jwtKey = builder.Configuration["Jwt:Key"] ?? "your-super-secret-jwt-key-that-is-at-least-32-characters-long";
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