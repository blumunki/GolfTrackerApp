// In GolfTrackerApp.Web/Data/ApplicationDbContext.cs
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GolfTrackerApp.Core.Models; // Add this using statement

namespace GolfTrackerApp.Core.Data
{
    // Ensure ApplicationUser exists or use IdentityUser
    // public class ApplicationUser : IdentityUser {} // If not already defined elsewhere by template

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser> // Or IdentityDbContext<ApplicationUser> if you have a custom ApplicationUser class
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Allows the provider-specific derived contexts (SqliteApplicationDbContext /
        // SqlServerApplicationDbContext) to pass their own typed options through.
        protected ApplicationDbContext(DbContextOptions options)
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

        // Handicaps (Phase 4)
        public DbSet<HandicapRecord> HandicapRecords { get; set; }
        public DbSet<ScoringDifferential> ScoringDifferentials { get; set; }

        // Competitions (Phase 3)
        public DbSet<Competition> Competitions { get; set; }
        public DbSet<CompetitionEntry> CompetitionEntries { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // Important: Call base method first for Identity models
            var optionalRelationshipDeleteBehavior = Database.IsSqlServer()
                ? DeleteBehavior.NoAction
                : DeleteBehavior.SetNull;

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
                .OnDelete(optionalRelationshipDeleteBehavior);

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
                    .OnDelete(optionalRelationshipDeleteBehavior);

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
                .OnDelete(DeleteBehavior.NoAction);

            // Score → TeeSet (optional, denormalized)
            builder.Entity<Score>()
                .HasOne(s => s.TeeSet)
                .WithMany()
                .HasForeignKey(s => s.TeeSetId)
                .OnDelete(DeleteBehavior.NoAction);

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

            // HandicapRecord configuration (Phase 4)
            builder.Entity<HandicapRecord>(entity =>
            {
                // Handicap history is derived data — it dies with the player.
                entity.HasOne(hr => hr.Player)
                    .WithMany(p => p.HandicapRecords)
                    .HasForeignKey(hr => hr.PlayerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(hr => hr.GolfSociety)
                    .WithMany()
                    .HasForeignKey(hr => hr.GolfSocietyId)
                    .OnDelete(optionalRelationshipDeleteBehavior);

                entity.HasOne(hr => hr.GolfClub)
                    .WithMany()
                    .HasForeignKey(hr => hr.GolfClubId)
                    .OnDelete(optionalRelationshipDeleteBehavior);

                entity.HasIndex(hr => new { hr.PlayerId, hr.Source, hr.EffectiveDate });
            });

            // ScoringDifferential configuration (Phase 4)
            builder.Entity<ScoringDifferential>(entity =>
            {
                // Restrict like Score → Player: AspNetUsers cascades into both Players
                // and Rounds, so a second cascade path into this table via Player would
                // be rejected by SQL Server (multiple cascade paths).
                entity.HasOne(sd => sd.Player)
                    .WithMany()
                    .HasForeignKey(sd => sd.PlayerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(sd => sd.Round)
                    .WithMany()
                    .HasForeignKey(sd => sd.RoundId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Restrict like HoleTee → TeeSet; differentials snapshot their inputs.
                entity.HasOne(sd => sd.TeeSet)
                    .WithMany()
                    .HasForeignKey(sd => sd.TeeSetId)
                    .OnDelete(DeleteBehavior.Restrict);

                // One differential per player per round (keeps the backfill idempotent).
                entity.HasIndex(sd => new { sd.PlayerId, sd.RoundId }).IsUnique();
                entity.HasIndex(sd => new { sd.PlayerId, sd.CalculatedAt });
            });

            // Competition configuration (Phase 3)
            builder.Entity<Competition>(entity =>
            {
                // Optional hosts/venue — NoAction on SQL Server (SetNull on SQLite) so a
                // club/society/course delete doesn't multi-path cascade into competitions.
                entity.HasOne(c => c.GolfClub)
                    .WithMany()
                    .HasForeignKey(c => c.GolfClubId)
                    .OnDelete(optionalRelationshipDeleteBehavior);

                entity.HasOne(c => c.GolfSociety)
                    .WithMany()
                    .HasForeignKey(c => c.GolfSocietyId)
                    .OnDelete(optionalRelationshipDeleteBehavior);

                entity.HasOne(c => c.GolfCourse)
                    .WithMany()
                    .HasForeignKey(c => c.GolfCourseId)
                    .OnDelete(optionalRelationshipDeleteBehavior);

                entity.HasOne(c => c.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(c => c.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(c => new { c.GolfClubId, c.Date });
                entity.HasIndex(c => new { c.GolfSocietyId, c.Date });
                entity.HasIndex(c => new { c.Status, c.Date });
            });

            // Round → Competition (optional). NoAction on SQL Server: a round already has a
            // cascade path from AspNetUsers, so this stays NoAction to avoid multiple paths.
            builder.Entity<Round>()
                .HasOne(r => r.Competition)
                .WithMany(c => c.Rounds)
                .HasForeignKey(r => r.CompetitionId)
                .OnDelete(optionalRelationshipDeleteBehavior);

            // CompetitionEntry configuration (Phase 3)
            builder.Entity<CompetitionEntry>(entity =>
            {
                entity.HasOne(e => e.Competition)
                    .WithMany(c => c.Entries)
                    .HasForeignKey(e => e.CompetitionId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Restrict like Score/ScoringDifferential → Player (AspNetUsers cascade paths).
                entity.HasOne(e => e.Player)
                    .WithMany()
                    .HasForeignKey(e => e.PlayerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.TeeSet)
                    .WithMany()
                    .HasForeignKey(e => e.TeeSetId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.CompetitionId, e.PlayerId }).IsUnique();
                entity.HasIndex(e => e.PlayerId);
            });
        }
    }
}
