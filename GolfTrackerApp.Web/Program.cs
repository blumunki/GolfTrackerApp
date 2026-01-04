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

builder.Services.AddAuthentication()
    .AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    });

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

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(connectionString);
    }
    else
    {
        options.UseSqlite(connectionString);
    }
});
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
            // For SQL Server (production), use EnsureCreated since migrations are SQLite-specific
            // This creates the schema based on the current model if it doesn't exist
            logger.LogInformation("Ensuring SQL Server database schema exists...");
            context.Database.EnsureCreated();
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