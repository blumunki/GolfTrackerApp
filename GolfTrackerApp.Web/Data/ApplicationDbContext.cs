// In GolfTrackerApp.Web/Data/ApplicationDbContext.cs
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GolfTrackerApp.Web.Models; // Add this using statement

namespace GolfTrackerApp.Web.Data
{
    // Ensure ApplicationUser exists or use IdentityUser
    // public class ApplicationUser : IdentityUser {} // If not already defined elsewhere by template

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser> // Or IdentityDbContext<ApplicationUser> if you have a custom ApplicationUser class
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Add DbSet properties for your application models
        public DbSet<Player> Players { get; set; }
        public DbSet<GolfClub> GolfClubs { get; set; }
        public DbSet<GolfCourse> GolfCourses { get; set; }
        public DbSet<Hole> Holes { get; set; }
        public DbSet<Round> Rounds { get; set; }
        public DbSet<Score> Scores { get; set; }
        public DbSet<RoundPlayer> RoundPlayers { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // Important: Call base method first for Identity models

            // Configure composite key for RoundPlayer join table
            builder.Entity<RoundPlayer>()
                .HasKey(rp => new { rp.RoundId, rp.PlayerId });

            // Configure relationships (optional, EF Core conventions often handle these)
            // Example: Define one-to-many relationship between GolfCourse and Hole
            builder.Entity<Hole>()
                .HasOne(h => h.GolfCourse)
                .WithMany(gc => gc.Holes)
                .HasForeignKey(h => h.GolfCourseId);

            // Example: Define one-to-many from Player to Score
            builder.Entity<Score>()
                .HasOne(s => s.Player)
                .WithMany(p => p.Scores)
                .HasForeignKey(s => s.PlayerId);

            // Example: Define one-to-many from Round to Score
            builder.Entity<Score>()
                .HasOne(s => s.Round)
                .WithMany(r => r.Scores)
                .HasForeignKey(s => s.RoundId);

            // Example: Define one-to-many from Hole to Score
            builder.Entity<Score>()
                .HasOne(s => s.Hole)
                .WithMany(h => h.Scores)
                .HasForeignKey(s => s.HoleId);

            // Configure the many-to-many relationship between Round and Player via RoundPlayer
            builder.Entity<RoundPlayer>()
                .HasOne(rp => rp.Round)
                .WithMany(r => r.RoundPlayers)
                .HasForeignKey(rp => rp.RoundId);

            builder.Entity<RoundPlayer>()
                .HasOne(rp => rp.Player)
                .WithMany(p => p.RoundPlayers)
                .HasForeignKey(rp => rp.PlayerId);
            
            builder.Entity<GolfCourse>()
                .HasOne(gc => gc.GolfClub)
                .WithMany(club => club.GolfCourses)
                .HasForeignKey(gc => gc.GolfClubId);

            // Add any other specific configurations if needed
            // For example, to prevent cascade delete on a specific relationship if necessary:
            // builder.Entity<SomeEntity>().HasOne(e => e.OtherEntity).WithMany().OnDelete(DeleteBehavior.Restrict);
        }
    }
}