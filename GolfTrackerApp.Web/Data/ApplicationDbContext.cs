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
        
        // Connection and notification tables
        public DbSet<PlayerConnection> PlayerConnections { get; set; }
        public DbSet<PlayerMergeRequest> PlayerMergeRequests { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // Important: Call base method first for Identity models

            // Configure composite key for RoundPlayer join table
            builder.Entity<RoundPlayer>()
                .HasKey(rp => new { rp.RoundId, rp.PlayerId });

            // Configure relationships
            builder.Entity<Hole>()
                .HasOne(h => h.GolfCourse)
                .WithMany(gc => gc.Holes)
                .HasForeignKey(h => h.GolfCourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure the many-to-many relationship between Round and Player via RoundPlayer
            // Use Restrict to avoid multiple cascade paths (SQL Server requirement)
            builder.Entity<RoundPlayer>()
                .HasOne(rp => rp.Round)
                .WithMany(r => r.RoundPlayers)
                .HasForeignKey(rp => rp.RoundId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RoundPlayer>()
                .HasOne(rp => rp.Player)
                .WithMany(p => p.RoundPlayers)
                .HasForeignKey(rp => rp.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Use Restrict for Score relationships to avoid cascade conflicts with SQL Server
            builder.Entity<Score>()
                .HasOne(s => s.Player)
                .WithMany(p => p.Scores)
                .HasForeignKey(s => s.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Score>()
                .HasOne(s => s.Round)
                .WithMany(r => r.Scores)
                .HasForeignKey(s => s.RoundId)
                .OnDelete(DeleteBehavior.Cascade); // Keep cascade - when round deleted, delete scores

            builder.Entity<Score>()
                .HasOne(s => s.Hole)
                .WithMany(h => h.Scores)
                .HasForeignKey(s => s.HoleId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.Entity<GolfCourse>()
                .HasOne(gc => gc.GolfClub)
                .WithMany(club => club.GolfCourses)
                .HasForeignKey(gc => gc.GolfClubId);

            // PlayerConnection configuration
            builder.Entity<PlayerConnection>()
                .HasIndex(pc => new { pc.RequestingUserId, pc.TargetUserId })
                .IsUnique(); // Prevent duplicate connection requests

            builder.Entity<PlayerConnection>()
                .HasOne(pc => pc.RequestingUser)
                .WithMany()
                .HasForeignKey(pc => pc.RequestingUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PlayerConnection>()
                .HasOne(pc => pc.TargetUser)
                .WithMany()
                .HasForeignKey(pc => pc.TargetUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // PlayerMergeRequest configuration
            builder.Entity<PlayerMergeRequest>()
                .HasOne(mr => mr.RequestingUser)
                .WithMany()
                .HasForeignKey(mr => mr.RequestingUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PlayerMergeRequest>()
                .HasOne(mr => mr.TargetUser)
                .WithMany()
                .HasForeignKey(mr => mr.TargetUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PlayerMergeRequest>()
                .HasOne(mr => mr.SourcePlayer)
                .WithMany()
                .HasForeignKey(mr => mr.SourcePlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PlayerMergeRequest>()
                .HasOne(mr => mr.TargetPlayer)
                .WithMany()
                .HasForeignKey(mr => mr.TargetPlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Notification configuration
            builder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt }); // Index for efficient queries

            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Delete notifications when user is deleted

            // ApplicationUser → Player (cached FK to avoid N+1 queries)
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.LinkedPlayer)
                .WithMany()
                .HasForeignKey(u => u.LinkedPlayerId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}