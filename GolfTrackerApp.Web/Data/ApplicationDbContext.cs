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

        // AI Insights tables
        public DbSet<AiAuditLog> AiAuditLogs { get; set; }
        public DbSet<AiChatSession> AiChatSessions { get; set; }
        public DbSet<AiChatSessionMessage> AiChatSessionMessages { get; set; }
        public DbSet<AiProviderSettings> AiProviderSettings { get; set; }

        // Application settings
        public DbSet<ApplicationSetting> ApplicationSettings { get; set; }

        // Tee sets (Phase 1)
        public DbSet<TeeSet> TeeSets { get; set; }
        public DbSet<HoleTee> HoleTees { get; set; }

        // Societies & memberships (Phase 2)
        public DbSet<GolfSociety> GolfSocieties { get; set; }
        public DbSet<SocietyMembership> SocietyMemberships { get; set; }
        public DbSet<ClubMembership> ClubMemberships { get; set; }

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

            // AI Audit Log
            builder.Entity<AiAuditLog>(entity =>
            {
                entity.HasOne(a => a.ApplicationUser)
                    .WithMany()
                    .HasForeignKey(a => a.ApplicationUserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.AiChatSession)
                    .WithMany(s => s.AuditLogs)
                    .HasForeignKey(a => a.AiChatSessionId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(a => new { a.ApplicationUserId, a.RequestedAt });
                entity.HasIndex(a => a.RequestedAt);
            });

            // AI Chat Session
            builder.Entity<AiChatSession>(entity =>
            {
                entity.HasOne(s => s.ApplicationUser)
                    .WithMany()
                    .HasForeignKey(s => s.ApplicationUserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(s => new { s.ApplicationUserId, s.LastMessageAt });
            });

            // AI Chat Session Message
            builder.Entity<AiChatSessionMessage>(entity =>
            {
                entity.HasOne(m => m.AiChatSession)
                    .WithMany(s => s.Messages)
                    .HasForeignKey(m => m.AiChatSessionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(m => new { m.AiChatSessionId, m.Timestamp });
            });

            // AI Provider Settings
            builder.Entity<AiProviderSettings>(entity =>
            {
                entity.HasIndex(s => s.ProviderName).IsUnique();
            });

            // Application Settings
            builder.Entity<ApplicationSetting>(entity =>
            {
                entity.HasIndex(s => s.Key).IsUnique();
            });

            // TeeSet configuration
            builder.Entity<TeeSet>(entity =>
            {
                entity.HasOne(ts => ts.GolfCourse)
                    .WithMany(gc => gc.TeeSets)
                    .HasForeignKey(ts => ts.GolfCourseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(ts => new { ts.GolfCourseId, ts.Name }).IsUnique();
            });

            // HoleTee configuration
            builder.Entity<HoleTee>(entity =>
            {
                entity.HasOne(ht => ht.Hole)
                    .WithMany(h => h.HoleTees)
                    .HasForeignKey(ht => ht.HoleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ht => ht.TeeSet)
                    .WithMany(ts => ts.HoleTees)
                    .HasForeignKey(ht => ht.TeeSetId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(ht => new { ht.HoleId, ht.TeeSetId }).IsUnique();
            });

            // RoundPlayer → TeeSet (optional)
            builder.Entity<RoundPlayer>()
                .HasOne(rp => rp.TeeSet)
                .WithMany()
                .HasForeignKey(rp => rp.TeeSetId)
                .OnDelete(DeleteBehavior.SetNull);

            // Score → TeeSet (optional, denormalized)
            builder.Entity<Score>()
                .HasOne(s => s.TeeSet)
                .WithMany()
                .HasForeignKey(s => s.TeeSetId)
                .OnDelete(DeleteBehavior.SetNull);

            // GolfSociety configuration
            builder.Entity<GolfSociety>(entity =>
            {
                entity.HasOne(gs => gs.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(gs => gs.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // SocietyMembership configuration
            builder.Entity<SocietyMembership>(entity =>
            {
                entity.HasOne(sm => sm.GolfSociety)
                    .WithMany(gs => gs.Memberships)
                    .HasForeignKey(sm => sm.GolfSocietyId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(sm => sm.User)
                    .WithMany(u => u.SocietyMemberships)
                    .HasForeignKey(sm => sm.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(sm => new { sm.GolfSocietyId, sm.UserId }).IsUnique();
            });

            // ClubMembership configuration
            builder.Entity<ClubMembership>(entity =>
            {
                entity.HasOne(cm => cm.GolfClub)
                    .WithMany(gc => gc.ClubMemberships)
                    .HasForeignKey(cm => cm.GolfClubId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(cm => cm.User)
                    .WithMany(u => u.ClubMemberships)
                    .HasForeignKey(cm => cm.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(cm => new { cm.GolfClubId, cm.UserId }).IsUnique();
            });
        }
    }
}