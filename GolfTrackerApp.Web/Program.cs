using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GolfTrackerApp.Web.Components;
using GolfTrackerApp.Web.Components.Account;
using GolfTrackerApp.Core.Data;
using GolfTrackerApp.Core.Services;
using GolfTrackerApp.Core.Models;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

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

// Use DbContextFactory to avoid threading issues in Blazor Server.
// Each provider has its own derived context type carrying its own migration set
// (see GolfTrackerApp.Core/Data/ProviderContexts.cs). Application code keeps depending on
// ApplicationDbContext / IDbContextFactory<ApplicationDbContext> via the
// forwarding registrations below.
if (databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContextFactory<SqlServerApplicationDbContext>(options =>
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
    }, ServiceLifetime.Scoped);
    builder.Services.AddScoped<IDbContextFactory<ApplicationDbContext>>(sp =>
        new DerivedDbContextFactory<SqlServerApplicationDbContext>(
            sp.GetRequiredService<IDbContextFactory<SqlServerApplicationDbContext>>()));
    // Scoped ApplicationDbContext for Identity's AddEntityFrameworkStores
    builder.Services.AddScoped<ApplicationDbContext>(sp =>
        sp.GetRequiredService<SqlServerApplicationDbContext>());
}
else
{
    builder.Services.AddDbContextFactory<SqliteApplicationDbContext>(options =>
    {
        options.UseSqlite(connectionString);
    }, ServiceLifetime.Scoped);
    builder.Services.AddScoped<IDbContextFactory<ApplicationDbContext>>(sp =>
        new DerivedDbContextFactory<SqliteApplicationDbContext>(
            sp.GetRequiredService<IDbContextFactory<SqliteApplicationDbContext>>()));
    builder.Services.AddScoped<ApplicationDbContext>(sp =>
        sp.GetRequiredService<SqliteApplicationDbContext>());
}
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
builder.Services.AddScoped<IHandicapService, HandicapService>();
builder.Services.AddScoped<IDatabaseMigrationService, DatabaseMigrationService>();
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

// Initialize database (migrations and seeding).
//
// Resilience: a web app that crash-loops when its database is briefly unavailable is
// worse than one that starts degraded and recovers. Two switches control startup:
//   Database:MigrateOnStartup   (default true)  — attempt EF migrations at startup.
//                                                  Set false to skip the (compute-costing)
//                                                  attempt when the DB is known-unavailable;
//                                                  apply later via restart or the admin page.
//   Database:ContinueOnInitFailure (default true) — start the app even if DB init fails,
//                                                  logging CRITICAL instead of crash-looping.
//                                                  Set false to fail fast (old behaviour).
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var dbProvider = builder.Configuration["DatabaseProvider"] ?? "Sqlite";
    var migrateOnStartup = builder.Configuration.GetValue("Database:MigrateOnStartup", true);
    var continueOnInitFailure = builder.Configuration.GetValue("Database:ContinueOnInitFailure", true);

    var context = services.GetRequiredService<ApplicationDbContext>();
    var schemaReady = false;

    if (migrateOnStartup)
    {
        try
        {
            logger.LogInformation("Applying {DatabaseProvider} database migrations...", dbProvider);
            await context.Database.MigrateAsync();
            logger.LogInformation("{DatabaseProvider} database migrations applied successfully.", dbProvider);
            schemaReady = true;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex,
                "Database migration failed for {DatabaseProvider}. The app is starting in a DEGRADED state — "
                + "database-backed features will error until migrations are applied. Apply them when the "
                + "database is available (restart with a reachable DB, or the admin Database Migrations page).",
                dbProvider);
            if (!continueOnInitFailure) throw;
        }
    }
    else
    {
        logger.LogWarning(
            "Database:MigrateOnStartup is false — skipping startup migrations for {DatabaseProvider}. "
            + "Apply pending migrations manually when the database is available.", dbProvider);
        // Schema may already be in place from a prior deploy; only seed if we can connect.
        try { schemaReady = await context.Database.CanConnectAsync(); }
        catch (Exception ex) { logger.LogWarning(ex, "Database is unreachable; skipping seeding."); }
    }

    if (schemaReady)
    {
        try
        {
            // Seed roles and admin user
            logger.LogInformation("Seeding database...");
            await SeedData.InitializeAsync(services);
            logger.LogInformation("Database seeding completed.");

            // Auto-sync clubs and courses from CSV files
            await SyncClubsAndCoursesFromCsvAsync(services, app.Environment.ContentRootPath, logger);

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
            logger.LogError(ex, "An error occurred during database seeding.");
            if (!continueOnInitFailure) throw;
        }
    }
    else
    {
        logger.LogWarning("Skipping database seeding — schema is not ready / database is unreachable.");
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

static async Task SyncClubsAndCoursesFromCsvAsync(IServiceProvider services, string contentRootPath, ILogger logger)
{
    var dataPath = Path.Combine(contentRootPath, "Data");

    var golfClubService = services.GetRequiredService<IGolfClubService>();
    var golfCourseService = services.GetRequiredService<IGolfCourseService>();

    // Sync clubs
    var clubsFile = Path.Combine(dataPath, "GolfClubs.csv");
    if (File.Exists(clubsFile))
    {
        var existingClubNames = (await golfClubService.GetAllGolfClubsAsync())
            .Select(gc => gc.Name.ToLowerInvariant()).ToHashSet();

        int added = 0;
        using var stream = File.OpenRead(clubsFile);
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
        });

        await foreach (var record in csv.GetRecordsAsync<GolfClubCsvRecord>())
        {
            if (string.IsNullOrWhiteSpace(record.Name)) continue;
            if (existingClubNames.Contains(record.Name.ToLowerInvariant())) continue;

            var newClub = new GolfClub
            {
                Name = record.Name,
                AddressLine1 = record.AddressLine1,
                AddressLine2 = record.AddressLine2,
                City = record.City,
                CountyOrRegion = record.CountyOrRegion,
                Postcode = record.Postcode,
                Country = record.Country,
                Website = record.Website
            };
            await golfClubService.AddGolfClubAsync(newClub);
            existingClubNames.Add(newClub.Name.ToLowerInvariant());
            added++;
            logger.LogInformation("Auto-added golf club: {ClubName}", newClub.Name);
        }

        if (added > 0) logger.LogInformation("Auto-synced {Count} new golf club(s) from CSV.", added);
    }

    // Sync courses
    var coursesFile = Path.Combine(dataPath, "GolfCourses.csv");
    if (File.Exists(coursesFile))
    {
        var clubNameToId = (await golfClubService.GetAllGolfClubsAsync())
            .ToDictionary(gc => gc.Name.ToLowerInvariant(), gc => gc.GolfClubId);

        var existingCourses = (await golfCourseService.GetAllGolfCoursesAsync())
            .Select(c => $"{c.GolfClubId}_{c.Name.ToLowerInvariant()}")
            .ToHashSet();

        int added = 0;
        using var stream = File.OpenRead(coursesFile);
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
        });

        await foreach (var record in csv.GetRecordsAsync<GolfCourseCsvRecord>())
        {
            if (string.IsNullOrWhiteSpace(record.CourseName) || string.IsNullOrWhiteSpace(record.ClubName)) continue;
            if (!clubNameToId.TryGetValue(record.ClubName.ToLowerInvariant(), out var clubId)) continue;

            var key = $"{clubId}_{record.CourseName.ToLowerInvariant()}";
            if (existingCourses.Contains(key)) continue;

            var newCourse = new GolfCourse
            {
                GolfClubId = clubId,
                Name = record.CourseName,
                DefaultPar = record.DefaultPar,
                NumberOfHoles = record.NumberOfHoles <= 0 ? 18 : record.NumberOfHoles
            };
            await golfCourseService.AddGolfCourseAsync(newCourse);
            existingCourses.Add(key);
            added++;
            logger.LogInformation("Auto-added golf course: {CourseName} at {ClubName}", record.CourseName, record.ClubName);
        }

        if (added > 0) logger.LogInformation("Auto-synced {Count} new golf course(s) from CSV.", added);
    }

    // Sync holes
    var holesFile = Path.Combine(dataPath, "Holes.csv");
    if (File.Exists(holesFile))
    {
        var courses = await golfCourseService.GetAllGolfCoursesAsync();
        var courseLookup = courses.ToDictionary(
            c => $"{(c.GolfClub?.Name ?? "").ToLowerInvariant()}_{c.Name.ToLowerInvariant()}",
            c => c.GolfCourseId
        );

        var holeService = services.GetRequiredService<IHoleService>();
        var existingHoles = new HashSet<string>();
        foreach (var course in courses)
        {
            var holes = await holeService.GetHolesForCourseAsync(course.GolfCourseId);
            foreach (var hole in holes)
                existingHoles.Add($"{hole.GolfCourseId}_{hole.HoleNumber}");
        }

        int holesAdded = 0;
        var holeTeeYardages = new Dictionary<int, List<(int HoleId, int? White, int? Yellow, int? Red)>>();
        var affectedCourseIds = new HashSet<int>();

        using var hStream = File.OpenRead(holesFile);
        using var hReader = new StreamReader(hStream);
        using var hCsv = new CsvReader(hReader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
        });

        await foreach (var record in hCsv.GetRecordsAsync<HoleCsvRecord>())
        {
            if (string.IsNullOrWhiteSpace(record.CourseName) || string.IsNullOrWhiteSpace(record.ClubName)
                || record.HoleNumber <= 0 || record.Par <= 0)
                continue;

            var courseKey = $"{record.ClubName.ToLowerInvariant()}_{record.CourseName.ToLowerInvariant()}";
            if (!courseLookup.TryGetValue(courseKey, out var courseId)) continue;

            var holeKey = $"{courseId}_{record.HoleNumber}";
            if (existingHoles.Contains(holeKey)) continue;

            var newHole = new Hole
            {
                GolfCourseId = courseId,
                HoleNumber = record.HoleNumber,
                Par = record.Par,
                StrokeIndex = record.StrokeIndex,
                LengthYards = record.YellowYards ?? record.LengthYards
            };
            await holeService.AddHoleAsync(newHole);
            existingHoles.Add(holeKey);
            holesAdded++;

            if (record.WhiteYards.HasValue || record.YellowYards.HasValue || record.RedYards.HasValue)
            {
                if (!holeTeeYardages.ContainsKey(courseId))
                    holeTeeYardages[courseId] = new List<(int HoleId, int? White, int? Yellow, int? Red)>();
                holeTeeYardages[courseId].Add((newHole.HoleId, record.WhiteYards, record.YellowYards ?? record.LengthYards, record.RedYards));
            }

            affectedCourseIds.Add(courseId);
        }

        if (holesAdded > 0)
        {
            logger.LogInformation("Auto-synced {Count} new hole(s) from CSV.", holesAdded);

            var teeSetService = services.GetRequiredService<ITeeSetService>();
            foreach (var courseId in affectedCourseIds)
            {
                await teeSetService.EnsureStandardTeeSetsAsync(courseId);
            }

            // Update HoleTee records with per-tee yardages from CSV
            foreach (var (courseId, holeEntries) in holeTeeYardages)
            {
                var teeSets = await teeSetService.GetTeeSetsForCourseAsync(courseId);
                var whiteTee = teeSets.FirstOrDefault(ts => ts.Name == "White");
                var yellowTee = teeSets.FirstOrDefault(ts => ts.Name == "Yellow");
                var redTee = teeSets.FirstOrDefault(ts => ts.Name == "Red");

                foreach (var (holeId, whiteYards, yellowYards, redYards) in holeEntries)
                {
                    if (whiteTee != null && whiteYards.HasValue)
                        await UpdateHoleTeeYardageAsync(teeSetService, holeId, whiteTee.TeeSetId, whiteYards.Value);
                    if (yellowTee != null && yellowYards.HasValue)
                        await UpdateHoleTeeYardageAsync(teeSetService, holeId, yellowTee.TeeSetId, yellowYards.Value);
                    if (redTee != null && redYards.HasValue)
                        await UpdateHoleTeeYardageAsync(teeSetService, holeId, redTee.TeeSetId, redYards.Value);
                }
            }
        }
    }
}

static async Task UpdateHoleTeeYardageAsync(ITeeSetService teeSetService, int holeId, int teeSetId, int yards)
{
    var holeTees = await teeSetService.GetHoleTeesForTeeSetAsync(teeSetId);
    var existing = holeTees.FirstOrDefault(ht => ht.HoleId == holeId);
    if (existing != null)
    {
        existing.LengthYards = yards;
        await teeSetService.AddOrUpdateHoleTeeAsync(existing);
    }
    else
    {
        await teeSetService.AddOrUpdateHoleTeeAsync(new HoleTee
        {
            HoleId = holeId, TeeSetId = teeSetId, LengthYards = yards
        });
    }
}

record GolfClubCsvRecord
{
    public string? Name { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? CountyOrRegion { get; set; }
    public string? Postcode { get; set; }
    public string? Country { get; set; }
    public string? Website { get; set; }
}

record GolfCourseCsvRecord
{
    public string? ClubName { get; set; }
    public string? CourseName { get; set; }
    public int DefaultPar { get; set; }
    public int NumberOfHoles { get; set; } = 18;
}

record HoleCsvRecord
{
    public string? ClubName { get; set; }
    public string? CourseName { get; set; }
    public int HoleNumber { get; set; }
    public int Par { get; set; }
    public int? StrokeIndex { get; set; }
    public int? WhiteYards { get; set; }
    public int? YellowYards { get; set; }
    public int? RedYards { get; set; }
    public int? LengthYards { get; set; }
}
