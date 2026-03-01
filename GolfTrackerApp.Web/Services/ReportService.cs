using GolfTrackerApp.Web.Data;
using GolfTrackerApp.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GolfTrackerApp.Web.Services;

public class ReportService : IReportService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<ReportService> _logger;

    public ReportService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<ReportService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    /// <summary>
    /// Applies standard round filters to a query. Eliminates duplicated filter logic across methods.
    /// </summary>
    private static IQueryable<Round> ApplyRoundFilters(
        IQueryable<Round> query,
        int? courseId = null,
        int? holesPlayed = null,
        RoundTypeOption? roundType = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        List<int>? sharedWithPlayerIds = null)
    {
        if (courseId.HasValue && courseId > 0)
            query = query.Where(r => r.GolfCourseId == courseId.Value);
        if (holesPlayed.HasValue && holesPlayed > 0)
            query = query.Where(r => r.HolesPlayed == holesPlayed.Value);
        if (roundType.HasValue)
            query = query.Where(r => r.RoundType == roundType.Value);
        if (startDate.HasValue)
            query = query.Where(r => r.DatePlayed.Date >= startDate.Value.Date);
        if (endDate.HasValue)
            query = query.Where(r => r.DatePlayed.Date <= endDate.Value.Date);
        if (sharedWithPlayerIds?.Any() == true)
        {
            foreach (var cpId in sharedWithPlayerIds)
                query = query.Where(r => r.RoundPlayers.Any(rp => rp.PlayerId == cpId));
        }
        return query;
    }

    public async Task<List<PlayerPerformanceDataPoint>> GetPlayerPerformanceAsync(int playerId, int? courseId, int? holesPlayed, RoundTypeOption? roundType, DateTime? startDate, DateTime? endDate, List<int>? sharedWithPlayerIds = null)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        
        var query = _context.Rounds
            .Where(r => r.RoundPlayers.Any(rp => rp.PlayerId == playerId) && r.Status == RoundCompletionStatus.Completed);

        query = ApplyRoundFilters(query, courseId, holesPlayed, roundType, startDate, endDate, sharedWithPlayerIds);

        var performanceData = await query
            .Include(r => r.Scores)
            .ThenInclude(s => s.Hole)
            .Include(r => r.GolfCourse)
            .ThenInclude(gc => gc!.GolfClub) // Eager load GolfClub
            .OrderBy(r => r.DatePlayed)
            .Select(r => new PlayerPerformanceDataPoint
            {
                RoundId = r.RoundId,
                Date = r.DatePlayed,
                CourseName = $"{r.GolfCourse!.GolfClub!.Name} - {r.GolfCourse.Name}",
                HolesPlayed = r.HolesPlayed,
                TotalScore = r.Scores.Where(s => s.PlayerId == playerId).Sum(s => s.Strokes),
                TotalPar = r.Scores.Where(s => s.PlayerId == playerId).Sum(s => s.Hole!.Par)
            })
            .ToListAsync();

        return performanceData;
    }

    public async Task<List<PlayingPartnerSummary>> GetPlayingPartnerSummaryAsync(string currentUserId, int count)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        
        // Find the PlayerId for the current ApplicationUser
        var currentPlayer = await _context.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ApplicationUserId == currentUserId);

        if (currentPlayer is null)
        {
            return new List<PlayingPartnerSummary>();
        }
        var currentPlayerId = currentPlayer.PlayerId;

        // Find all rounds the current player participated in
        var userRoundIds = await _context.RoundPlayers
            .AsNoTracking()
            .Where(rp => rp.PlayerId == currentPlayerId)
            .Select(rp => rp.RoundId)
            .ToListAsync();

        if (!userRoundIds.Any())
        {
            return new List<PlayingPartnerSummary>();
        }

        // Find all players who played in those same rounds (the partners)
        // Load all rounds with scores in a single query to avoid N+1
        var roundsWithPartners = await _context.Rounds
            .AsNoTracking()
            .Where(r => userRoundIds.Contains(r.RoundId))
            .Include(r => r.RoundPlayers)
                .ThenInclude(rp => rp.Player)
            .Include(r => r.Scores)
            .AsSplitQuery()
            .ToListAsync();

        // Extract unique partners
        var partners = roundsWithPartners
            .SelectMany(r => r.RoundPlayers)
            .Where(rp => rp.PlayerId != currentPlayerId && rp.Player != null)
            .Select(rp => rp.Player!)
            .DistinctBy(p => p.PlayerId)
            .ToList();

        var summaryList = new List<PlayingPartnerSummary>();

        foreach (var partner in partners)
        {
            // Filter from already-loaded data instead of querying per partner
            var roundsTogether = roundsWithPartners
                .Where(r => r.RoundPlayers.Any(rp => rp.PlayerId == currentPlayerId) &&
                            r.RoundPlayers.Any(rp => rp.PlayerId == partner.PlayerId))
                .ToList();

            if (!roundsTogether.Any()) continue;

            int userWins = 0;
            int partnerWins = 0;
            int ties = 0;

            foreach (var round in roundsTogether)
            {
                var userScore = round.Scores.Where(s => s.PlayerId == currentPlayerId).Sum(s => s.Strokes);
                var partnerScore = round.Scores.Where(s => s.PlayerId == partner.PlayerId).Sum(s => s.Strokes); // Now safe

                if (userScore > 0 && partnerScore > 0)
                {
                    if (userScore < partnerScore) userWins++;
                    else if (partnerScore < userScore) partnerWins++;
                    else ties++;
                }
            }

            summaryList.Add(new PlayingPartnerSummary
            {
                PartnerId = partner.PlayerId, // Now safe
                PartnerName = $"{partner.FirstName} {partner.LastName}",
                LastPlayedDate = roundsTogether.Max(r => r.DatePlayed),
                UserWins = userWins,
                PartnerWins = partnerWins,
                Ties = ties
            });
        }

        return summaryList.OrderByDescending(s => s.LastPlayedDate).Take(count).ToList();
    }

    public async Task<List<PlayerPerformanceDataPoint>> GetPlayerPerformanceSummaryAsync(string currentUserId, int roundCount)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        
        var player = await _context.Players.AsNoTracking().FirstOrDefaultAsync(p => p.ApplicationUserId == currentUserId);
        if (player is null)
        {
            return new List<PlayerPerformanceDataPoint>();
        }

        var performanceData = await _context.Rounds
            .AsNoTracking()
            .Where(r => r.RoundPlayers.Any(rp => rp.PlayerId == player.PlayerId) && r.Status == RoundCompletionStatus.Completed)
            .OrderByDescending(r => r.DatePlayed)
            .Take(roundCount)
            .Select(r => new PlayerPerformanceDataPoint
            {
                RoundId = r.RoundId,
                Date = r.DatePlayed,
                TotalScore = r.Scores.Where(s => s.PlayerId == player.PlayerId).Sum(s => s.Strokes),
                TotalPar = r.Scores.Where(s => s.PlayerId == player.PlayerId).Sum(s => s.Hole!.Par)
            })
            .OrderBy(d => d.Date) // Re-order by date ascending for the chart
            .ToListAsync();

        return performanceData;
    }

    public async Task<PlayerReportViewModel> GetPlayerReportViewModelAsync(int playerId, int? courseId, int? holesPlayed, RoundTypeOption? roundType, DateTime? startDate, DateTime? endDate, List<int>? sharedWithPlayerIds = null)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        
        // Await player query first to avoid concurrent operations on same context
        var player = await _context.Players.AsNoTracking().FirstOrDefaultAsync(p => p.PlayerId == playerId);
        
        // Load courses with GolfClub for web app compatibility, but break circular references
        var coursesQuery = _context.GolfCourses
            .AsNoTracking()
            .Include(c => c.GolfClub)
            .Select(c => new 
            {
                Course = c,
                ClubName = c.GolfClub != null ? c.GolfClub.Name : "Unknown Club"
            });

        var coursesData = await coursesQuery.ToListAsync();
        
        // Convert to GolfCourse objects without circular references
        var courses = coursesData.Select(item => new GolfCourse
        {
            GolfCourseId = item.Course.GolfCourseId,
            GolfClubId = item.Course.GolfClubId,
            Name = item.Course.Name,
            DefaultPar = item.Course.DefaultPar,
            NumberOfHoles = item.Course.NumberOfHoles,
            // Create a simple GolfClub without navigation properties
            GolfClub = new GolfClub
            {
                GolfClubId = item.Course.GolfClubId,
                Name = item.ClubName
                // Deliberately omit GolfCourses collection to break circular reference
            }
        }).ToList();

        var performanceData = await GetPlayerPerformanceAsync(playerId, courseId, holesPlayed, roundType, startDate, endDate, sharedWithPlayerIds);

        return new PlayerReportViewModel
        {
            Player = player,
            FilterCourses = courses,
            PerformanceData = performanceData
        };
    }

    public async Task<ScoringDistribution> GetScoringDistributionAsync(int playerId, int? courseId, int? holesPlayed, RoundTypeOption? roundType, DateTime? startDate, DateTime? endDate, List<int>? sharedWithPlayerIds = null)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        
        var query = _context.Rounds
            .Where(r => r.RoundPlayers.Any(rp => rp.PlayerId == playerId) && r.Status == RoundCompletionStatus.Completed);

        query = ApplyRoundFilters(query, courseId, holesPlayed, roundType, startDate, endDate, sharedWithPlayerIds);

        // Get all scores for the player in filtered rounds
        var scores = await query
            .SelectMany(r => r.Scores)
            .Where(s => s.PlayerId == playerId && s.Strokes > 0) // Filter out invalid scores
            .Include(s => s.Hole)
            .Where(s => s.Hole != null && s.Hole.Par > 0) // Filter out holes with invalid par
            .ToListAsync();

        var distribution = new ScoringDistribution();

        foreach (var score in scores)
        {
            var scoreToPar = score.Strokes - score.Hole!.Par;

            switch (scoreToPar)
            {
                case <= -2: // Eagle or better
                    distribution.EagleCount++;
                    break;
                case -1: // Birdie
                    distribution.BirdieCount++;
                    break;
                case 0: // Par
                    distribution.ParCount++;
                    break;
                case 1: // Bogey
                    distribution.BogeyCount++;
                    break;
                case 2: // Double Bogey
                    distribution.DoubleBogeyCount++;
                    break;
                default: // Triple Bogey or worse
                    distribution.TripleBogeyOrWorseCount++;
                    break;
            }
        }

        return distribution;
    }

    public async Task<ScoringDistribution> GetScoringDistributionForClubAsync(int playerId, int clubId, int? holesPlayed, RoundTypeOption? roundType, DateTime? startDate, DateTime? endDate)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        
        var query = _context.Rounds
            .Where(r => r.RoundPlayers.Any(rp => rp.PlayerId == playerId) 
                       && r.Status == RoundCompletionStatus.Completed
                       && r.GolfCourse!.GolfClubId == clubId);

        query = ApplyRoundFilters(query, holesPlayed: holesPlayed, roundType: roundType, startDate: startDate, endDate: endDate);

        // Get all scores for the player in filtered rounds
        var scores = await query
            .SelectMany(r => r.Scores)
            .Where(s => s.PlayerId == playerId && s.Strokes > 0)
            .Include(s => s.Hole)
            .Where(s => s.Hole != null && s.Hole.Par > 0)
            .ToListAsync();

        var distribution = new ScoringDistribution();

        foreach (var score in scores)
        {
            var scoreToPar = score.Strokes - score.Hole!.Par;

            switch (scoreToPar)
            {
                case <= -2:
                    distribution.EagleCount++;
                    break;
                case -1:
                    distribution.BirdieCount++;
                    break;
                case 0:
                    distribution.ParCount++;
                    break;
                case 1:
                    distribution.BogeyCount++;
                    break;
                case 2:
                    distribution.DoubleBogeyCount++;
                    break;
                default:
                    distribution.TripleBogeyOrWorseCount++;
                    break;
            }
        }

        return distribution;
    }

    public async Task<PerformanceByPar> GetPerformanceByParAsync(int playerId, int? courseId, int? holesPlayed, RoundTypeOption? roundType, DateTime? startDate, DateTime? endDate, List<int>? sharedWithPlayerIds = null)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        
        var roundsQuery = _context.Rounds
            .Where(r => r.RoundPlayers.Any(rp => rp.PlayerId == playerId) && r.Status == RoundCompletionStatus.Completed);

        roundsQuery = ApplyRoundFilters(roundsQuery, courseId, holesPlayed, roundType, startDate, endDate, sharedWithPlayerIds);

        // Get the round IDs first
        var filteredRoundIds = await roundsQuery.Select(r => r.RoundId).ToListAsync();

        if (!filteredRoundIds.Any())
        {
            return new PerformanceByPar();
        }

        // Now get all scores for this player in these rounds with hole data
        var playerScores = await _context.Scores
            .Where(s => s.PlayerId == playerId && 
                       filteredRoundIds.Contains(s.RoundId) && 
                       s.Strokes > 0)
            .Include(s => s.Hole)
            .Where(s => s.Hole != null && s.Hole.Par > 0 && s.Hole.Par <= 5)
            .ToListAsync();

        var performance = new PerformanceByPar();

        // Group by par and calculate
        var par3Scores = playerScores.Where(s => s.Hole!.Par == 3).Select(s => s.Strokes).ToList();
        var par4Scores = playerScores.Where(s => s.Hole!.Par == 4).Select(s => s.Strokes).ToList();
        var par5Scores = playerScores.Where(s => s.Hole!.Par == 5).Select(s => s.Strokes).ToList();

        if (par3Scores.Any())
        {
            performance.Par3Average = par3Scores.Average();
            performance.Par3Count = par3Scores.Count;
        }

        if (par4Scores.Any())
        {
            performance.Par4Average = par4Scores.Average();
            performance.Par4Count = par4Scores.Count;
        }

        if (par5Scores.Any())
        {
            performance.Par5Average = par5Scores.Average();
            performance.Par5Count = par5Scores.Count;
        }

        return performance;
    }

    public async Task<List<PlayerPerformanceDataPoint>> GetPlayerPerformanceForClubAsync(string currentUserId, int clubId, int roundCount)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        
        // Get the player for this user
        var player = await _context.Players
            .FirstOrDefaultAsync(p => p.ApplicationUserId == currentUserId);

        if (player == null) return new List<PlayerPerformanceDataPoint>();

        var performanceData = await _context.Rounds
            .Where(r => r.RoundPlayers.Any(rp => rp.PlayerId == player.PlayerId) 
                       && r.Status == RoundCompletionStatus.Completed
                       && r.GolfCourse!.GolfClubId == clubId)
            .Include(r => r.Scores)
            .ThenInclude(s => s.Hole)
            .Include(r => r.GolfCourse)
            .ThenInclude(gc => gc!.GolfClub)
            .OrderByDescending(r => r.DatePlayed)
            .Take(roundCount)
            .Select(r => new PlayerPerformanceDataPoint
            {
                RoundId = r.RoundId,
                Date = r.DatePlayed,
                TotalScore = r.Scores.Where(s => s.PlayerId == player.PlayerId).Sum(s => s.Strokes),
                TotalPar = r.Scores.Where(s => s.PlayerId == player.PlayerId).Sum(s => s.Hole!.Par),
                CourseName = $"{r.GolfCourse!.GolfClub!.Name} - {r.GolfCourse.Name}",
                HolesPlayed = r.HolesPlayed
            })
            .ToListAsync();

        return performanceData.OrderBy(p => p.Date).ToList();
    }

    public async Task<List<PlayerPerformanceDataPoint>> GetPlayerPerformanceForCourseAsync(string currentUserId, int courseId, int roundCount)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        
        // Get the player for this user
        var player = await _context.Players
            .FirstOrDefaultAsync(p => p.ApplicationUserId == currentUserId);

        if (player == null) return new List<PlayerPerformanceDataPoint>();

        var performanceData = await _context.Rounds
            .Where(r => r.RoundPlayers.Any(rp => rp.PlayerId == player.PlayerId) 
                       && r.Status == RoundCompletionStatus.Completed
                       && r.GolfCourseId == courseId)
            .Include(r => r.Scores)
            .ThenInclude(s => s.Hole)
            .Include(r => r.GolfCourse)
            .ThenInclude(gc => gc!.GolfClub)
            .OrderByDescending(r => r.DatePlayed)
            .Take(roundCount)
            .Select(r => new PlayerPerformanceDataPoint
            {
                RoundId = r.RoundId,
                Date = r.DatePlayed,
                TotalScore = r.Scores.Where(s => s.PlayerId == player.PlayerId).Sum(s => s.Strokes),
                TotalPar = r.Scores.Where(s => s.PlayerId == player.PlayerId).Sum(s => s.Hole!.Par),
                CourseName = $"{r.GolfCourse!.GolfClub!.Name} - {r.GolfCourse.Name}",
                HolesPlayed = r.HolesPlayed
            })
            .ToListAsync();

        return performanceData.OrderBy(p => p.Date).ToList();
    }

    public async Task<DashboardStats> GetDashboardStatsAsync(string currentUserId)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        
        var player = await _context.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ApplicationUserId == currentUserId);

        if (player == null)
        {
            return new DashboardStats();
        }

        var playerId = player.PlayerId;

        // Project per-round score summaries in SQL (avoids loading full Score/Hole entities)
        var roundScores = await _context.Rounds
            .AsNoTracking()
            .Where(r => r.RoundPlayers.Any(rp => rp.PlayerId == playerId) 
                       && r.Status == RoundCompletionStatus.Completed)
            .OrderBy(r => r.DatePlayed)
            .Select(r => new
            {
                r.RoundId,
                r.GolfCourseId,
                r.DatePlayed,
                r.HolesPlayed,
                ClubId = r.GolfCourse!.GolfClubId,
                CourseName = r.GolfCourse!.Name,
                ClubName = r.GolfCourse!.GolfClub!.Name,
                TotalScore = r.Scores.Where(s => s.PlayerId == playerId).Sum(s => s.Strokes),
                TotalPar = r.Scores.Where(s => s.PlayerId == playerId).Sum(s => s.Hole!.Par)
            })
            .ToListAsync();

        if (!roundScores.Any())
        {
            return new DashboardStats();
        }

        var stats = new DashboardStats
        {
            TotalRounds = roundScores.Count,
            LastRoundDate = roundScores.Max(r => r.DatePlayed),
            UniqueCoursesPlayed = roundScores.Select(r => r.GolfCourseId).Distinct().Count(),
            UniqueClubsVisited = roundScores.Select(r => r.ClubId).Distinct().Count(),
            NineHoleRounds = roundScores.Count(r => r.HolesPlayed == 9),
            EighteenHoleRounds = roundScores.Count(r => r.HolesPlayed == 18)
        };

        var validScores = roundScores.Where(rs => rs.TotalScore > 0).ToList();

        if (validScores.Any())
        {
            var bestRound = validScores.OrderBy(rs => rs.TotalScore).First();
            stats.BestScore = bestRound.TotalScore;
            stats.BestScoreCourseName = $"{bestRound.ClubName} - {bestRound.CourseName}";
            stats.BestScoreDate = bestRound.DatePlayed;

            stats.AverageScore = validScores.Average(rs => rs.TotalScore);
            
            var toPars = validScores.Select(rs => rs.TotalScore - rs.TotalPar).ToList();
            stats.AverageToPar = toPars.Average();
            stats.LowestToPar = toPars.Min();

            // Find most played course
            var favoriteGroup = validScores
                .GroupBy(rs => rs.GolfCourseId)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            if (favoriteGroup != null)
            {
                var favRound = favoriteGroup.First();
                stats.FavoriteCourseName = $"{favRound.ClubName} - {favRound.CourseName}";
                stats.FavoriteCourseRounds = favoriteGroup.Count();
            }

            // Calculate improvement streak (last 5 rounds vs previous average)
            if (validScores.Count >= 5)
            {
                var recentRounds = validScores.TakeLast(5).ToList();
                var previousRounds = validScores.Take(validScores.Count - 5).ToList();
                
                if (previousRounds.Any())
                {
                    var recentAverage = recentRounds.Average(rs => rs.TotalScore - rs.TotalPar);
                    var previousAverage = previousRounds.Average(rs => rs.TotalScore - rs.TotalPar);
                    
                    stats.IsImprovingStreak = recentAverage < previousAverage;
                    stats.CurrentStreak = recentRounds.Count;
                }
            }
        }

        return stats;
    }

    public async Task<List<CourseHistoryItem>> GetCourseHistoryAsync(string currentUserId, int count = 6)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();

        var player = await _context.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ApplicationUserId == currentUserId);

        if (player == null)
            return new List<CourseHistoryItem>();

        var playerId = player.PlayerId;

        // Project per-round score summaries in SQL (avoids loading full entity graphs)
        var roundSummaries = await _context.Rounds
            .AsNoTracking()
            .Where(r => r.RoundPlayers.Any(rp => rp.PlayerId == playerId)
                       && r.Status == RoundCompletionStatus.Completed)
            .Select(r => new
            {
                r.GolfCourseId,
                r.DatePlayed,
                CourseName = r.GolfCourse!.Name ?? "Unknown",
                ClubName = r.GolfCourse!.GolfClub!.Name ?? "Unknown",
                TotalScore = r.Scores.Where(s => s.PlayerId == playerId).Sum(s => s.Strokes),
                TotalPar = r.Scores.Where(s => s.PlayerId == playerId).Sum(s => s.Hole!.Par)
            })
            .ToListAsync();

        if (!roundSummaries.Any())
            return new List<CourseHistoryItem>();

        var courseGroups = roundSummaries
            .Where(rs => rs.TotalScore > 0)
            .GroupBy(rs => rs.GolfCourseId)
            .Select(g =>
            {
                var ordered = g.OrderByDescending(rs => rs.DatePlayed).ToList();
                var mostRecent = ordered.First();
                var best = ordered.OrderBy(rs => rs.TotalScore).First();

                return new CourseHistoryItem
                {
                    GolfCourseId = g.Key,
                    CourseName = mostRecent.CourseName,
                    ClubName = mostRecent.ClubName,
                    LastPlayedDate = mostRecent.DatePlayed,
                    MostRecentScore = mostRecent.TotalScore,
                    MostRecentToPar = mostRecent.TotalScore - mostRecent.TotalPar,
                    BestScore = best.TotalScore,
                    BestToPar = best.TotalScore - best.TotalPar,
                    TimesPlayed = ordered.Count
                };
            })
            .OrderByDescending(item => item.LastPlayedDate)
            .Take(count)
            .ToList();

        return courseGroups;
    }

    public async Task<PlayerComparisonResult> GetPlayerComparisonAsync(
        int primaryPlayerId, 
        List<int> comparePlayerIds, 
        int? courseId, int? holesPlayed, RoundTypeOption? roundType, 
        DateTime? startDate, DateTime? endDate)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        
        var allPlayerIds = new List<int> { primaryPlayerId };
        allPlayerIds.AddRange(comparePlayerIds);
        allPlayerIds = allPlayerIds.Distinct().ToList();

        // Load all players
        var players = await _context.Players
            .AsNoTracking()
            .Where(p => allPlayerIds.Contains(p.PlayerId))
            .ToListAsync();

        var result = new PlayerComparisonResult();

        // Get performance data for each player, filtered to rounds where ALL players participated
        foreach (var playerId in allPlayerIds)
        {
            var player = players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null) continue;

            var otherPlayerIds = allPlayerIds.Where(id => id != playerId).ToList();
            var perfData = await GetPlayerPerformanceAsync(playerId, courseId, holesPlayed, roundType, startDate, endDate, sharedWithPlayerIds: otherPlayerIds);

            result.PlayerSeries.Add(new PlayerComparisonSeries
            {
                PlayerId = playerId,
                PlayerName = $"{player.FirstName} {player.LastName}",
                DataPoints = perfData
            });
        }

        // Calculate head-to-head stats for shared rounds
        if (allPlayerIds.Count > 1)
        {
            // Find rounds where primary player and each compare player both played
            var primaryRoundIds = result.PlayerSeries
                .FirstOrDefault(s => s.PlayerId == primaryPlayerId)?.DataPoints
                .Select(d => d.RoundId).ToHashSet() ?? new HashSet<int>();

            foreach (var playerId in allPlayerIds)
            {
                var player = players.FirstOrDefault(p => p.PlayerId == playerId);
                if (player == null) continue;

                var series = result.PlayerSeries.FirstOrDefault(s => s.PlayerId == playerId);
                if (series == null) continue;

                var summary = new PlayerComparisonSummary
                {
                    PlayerId = playerId,
                    PlayerName = $"{player.FirstName} {player.LastName}",
                    TotalRounds = series.DataPoints.Count
                };

                if (series.DataPoints.Any())
                {
                    summary.AverageScore = series.DataPoints.Average(d => d.TotalScore);
                    summary.AverageToPar = series.DataPoints.Average(d => (double)d.ScoreVsPar);
                    summary.BestScore = series.DataPoints.Min(d => d.TotalScore);
                    summary.BestToPar = series.DataPoints.Min(d => d.ScoreVsPar);
                }

                // Head-to-head vs primary player
                if (playerId != primaryPlayerId)
                {
                    var compareRoundIds = series.DataPoints.Select(d => d.RoundId).ToHashSet();
                    var sharedRoundIds = primaryRoundIds.Intersect(compareRoundIds).ToList();
                    summary.SharedRounds = sharedRoundIds.Count;

                    var primarySeries = result.PlayerSeries.First(s => s.PlayerId == primaryPlayerId);
                    foreach (var roundId in sharedRoundIds)
                    {
                        var primaryScore = primarySeries.DataPoints.First(d => d.RoundId == roundId).TotalScore;
                        var compareScore = series.DataPoints.First(d => d.RoundId == roundId).TotalScore;

                        if (compareScore < primaryScore) summary.Wins++;
                        else if (compareScore > primaryScore) summary.Losses++;
                        else summary.Ties++;
                    }
                }

                result.Summaries.Add(summary);
            }
        }

        return result;
    }

    public async Task<RoundDetailSummary?> GetRoundDetailAsync(int roundId, int playerId)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        
        var round = await _context.Rounds
            .AsNoTracking()
            .Include(r => r.GolfCourse)
            .ThenInclude(gc => gc!.GolfClub)
            .Include(r => r.Scores)
            .ThenInclude(s => s.Hole)
            .Include(r => r.RoundPlayers)
            .ThenInclude(rp => rp.Player)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.RoundId == roundId);

        if (round == null) return null;

        var detail = new RoundDetailSummary
        {
            RoundId = round.RoundId,
            DatePlayed = round.DatePlayed,
            CourseName = round.GolfCourse?.Name ?? "Unknown",
            ClubName = round.GolfCourse?.GolfClub?.Name ?? "Unknown",
            HolesPlayed = round.HolesPlayed,
            TotalScore = round.Scores.Where(s => s.PlayerId == playerId).Sum(s => s.Strokes),
            TotalPar = round.Scores.Where(s => s.PlayerId == playerId).Sum(s => s.Hole?.Par ?? 0),
            RoundType = round.RoundType.ToString(),
            HoleScores = round.Scores
                .Where(s => s.PlayerId == playerId && s.Hole != null)
                .OrderBy(s => s.Hole!.HoleNumber)
                .Select(s => new RoundDetailScore
                {
                    HoleNumber = s.Hole!.HoleNumber,
                    Par = s.Hole.Par,
                    Strokes = s.Strokes
                })
                .ToList(),
            OtherPlayers = round.RoundPlayers
                .Where(rp => rp.PlayerId != playerId && rp.Player != null)
                .Select(rp => new RoundDetailPlayer
                {
                    PlayerId = rp.PlayerId,
                    PlayerName = $"{rp.Player!.FirstName} {rp.Player.LastName}",
                    TotalScore = round.Scores.Where(s => s.PlayerId == rp.PlayerId).Sum(s => s.Strokes),
                    TotalPar = round.Scores.Where(s => s.PlayerId == rp.PlayerId).Sum(s => s.Hole?.Par ?? 0)
                })
                .ToList()
        };

        return detail;
    }

    public async Task<PlayerQuickStats> GetPlayerQuickStatsAsync(int playerId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var rounds = await context.Rounds
            .AsNoTracking()
            .Where(r => r.RoundPlayers.Any(rp => rp.PlayerId == playerId) && r.Status == RoundCompletionStatus.Completed)
            .Select(r => new
            {
                r.DatePlayed,
                TotalScore = r.Scores.Where(s => s.PlayerId == playerId).Sum(s => s.Strokes),
                TotalPar = r.Scores.Where(s => s.PlayerId == playerId).Sum(s => s.Hole!.Par)
            })
            .ToListAsync();

        if (!rounds.Any())
        {
            return new PlayerQuickStats { PlayerId = playerId };
        }

        var validScores = rounds.Where(r => r.TotalScore > 0).ToList();

        return new PlayerQuickStats
        {
            PlayerId = playerId,
            RoundCount = rounds.Count,
            BestScore = validScores.Any() ? validScores.Min(r => r.TotalScore) : null,
            AverageScore = validScores.Any() ? validScores.Average(r => (double)r.TotalScore) : null,
            LastPlayed = rounds.Max(r => r.DatePlayed),
            AverageVsPar = validScores.Any() ? validScores.Average(r => (double)(r.TotalScore - r.TotalPar)) : null
        };
    }

    public async Task<Dictionary<int, PlayerQuickStats>> GetBatchPlayerQuickStatsAsync(IEnumerable<int> playerIds)
    {
        var ids = playerIds.ToList();
        if (!ids.Any()) return new Dictionary<int, PlayerQuickStats>();

        await using var context = await _contextFactory.CreateDbContextAsync();

        // Step 1: Get round IDs and dates for these players (SQLite-friendly, no APPLY)
        var roundPlayerData = await context.RoundPlayers
            .AsNoTracking()
            .Where(rp => ids.Contains(rp.PlayerId))
            .Join(context.Rounds.Where(r => r.Status == RoundCompletionStatus.Completed),
                  rp => rp.RoundId, r => r.RoundId,
                  (rp, r) => new { rp.PlayerId, r.RoundId, r.DatePlayed })
            .ToListAsync();

        var roundIds = roundPlayerData.Select(r => r.RoundId).Distinct().ToList();

        // Step 2: Get scores for those rounds and players
        var scores = await context.Scores
            .AsNoTracking()
            .Where(s => roundIds.Contains(s.RoundId) && ids.Contains(s.PlayerId))
            .Select(s => new { s.PlayerId, s.RoundId, s.Strokes, HolePar = s.Hole!.Par })
            .ToListAsync();

        // Step 3: Compute stats in memory
        var scoresByPlayerRound = scores
            .GroupBy(s => new { s.PlayerId, s.RoundId })
            .Select(g => new
            {
                g.Key.PlayerId,
                g.Key.RoundId,
                TotalScore = g.Sum(s => s.Strokes),
                TotalPar = g.Sum(s => s.HolePar)
            })
            .ToList();

        var result = new Dictionary<int, PlayerQuickStats>();
        foreach (var id in ids)
        {
            var playerRoundDates = roundPlayerData.Where(r => r.PlayerId == id).ToList();
            var playerScores = scoresByPlayerRound.Where(s => s.PlayerId == id).ToList();
            var validScores = playerScores.Where(s => s.TotalScore > 0).ToList();

            result[id] = new PlayerQuickStats
            {
                PlayerId = id,
                RoundCount = playerRoundDates.Count,
                BestScore = validScores.Any() ? validScores.Min(r => r.TotalScore) : null,
                AverageScore = validScores.Any() ? validScores.Average(r => (double)r.TotalScore) : null,
                LastPlayed = playerRoundDates.Any() ? playerRoundDates.Max(r => r.DatePlayed) : null,
                AverageVsPar = validScores.Any() ? validScores.Average(r => (double)(r.TotalScore - r.TotalPar)) : null
            };
        }

        return result;
    }

    public async Task<List<string>> GetRecentActivityAsync(string currentUserId, int limit = 5)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var recentRounds = await context.Rounds
            .AsNoTracking()
            .Include(r => r.GolfCourse)
            .ThenInclude(gc => gc!.GolfClub!)
            .Where(r => r.CreatedByApplicationUserId == currentUserId)
            .OrderByDescending(r => r.DatePlayed)
            .Take(limit)
            .Select(r => $"Played {(r.GolfCourse != null ? r.GolfCourse.Name : "Unknown Course")} on {r.DatePlayed:MMM dd}")
            .ToListAsync();

        if (!recentRounds.Any())
        {
            recentRounds = new List<string>
            {
                "Welcome to Golf Tracker!",
                "Start by recording your first round",
                "Explore golf clubs in your area"
            };
        }

        return recentRounds;
    }

    public async Task<List<ScoreDistributionBucket>> GetScoreDistributionBucketedAsync(string currentUserId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var roundScores = await context.Scores
            .AsNoTracking()
            .Where(s => s.Round != null && s.Round.CreatedByApplicationUserId == currentUserId)
            .GroupBy(s => s.RoundId)
            .Select(g => g.Sum(s => s.Strokes))
            .ToListAsync();

        if (!roundScores.Any())
            return new List<ScoreDistributionBucket>();

        var totalRounds = roundScores.Count;

        return new List<ScoreDistributionBucket>
        {
            new() { Range = "Under 80", Count = roundScores.Count(s => s < 80), Percentage = Math.Round((double)roundScores.Count(s => s < 80) / totalRounds * 100, 1) },
            new() { Range = "80-89", Count = roundScores.Count(s => s >= 80 && s <= 89), Percentage = Math.Round((double)roundScores.Count(s => s >= 80 && s <= 89) / totalRounds * 100, 1) },
            new() { Range = "90-99", Count = roundScores.Count(s => s >= 90 && s <= 99), Percentage = Math.Round((double)roundScores.Count(s => s >= 90 && s <= 99) / totalRounds * 100, 1) },
            new() { Range = "100+", Count = roundScores.Count(s => s >= 100), Percentage = Math.Round((double)roundScores.Count(s => s >= 100) / totalRounds * 100, 1) }
        };
    }

    public async Task<List<FavoriteCourseItem>> GetFavoriteCoursesAsync(string currentUserId, int limit = 10)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var favoriteCourses = await context.Rounds
            .AsNoTracking()
            .Include(r => r.GolfCourse)
            .ThenInclude(gc => gc!.GolfClub!)
            .Where(r => r.CreatedByApplicationUserId == currentUserId)
            .GroupBy(r => new { r.GolfCourseId, CourseName = r.GolfCourse!.Name, ClubName = r.GolfCourse.GolfClub!.Name })
            .Select(g => new FavoriteCourseItem
            {
                Name = g.Key.CourseName,
                Location = g.Key.ClubName,
                RoundsPlayed = g.Count(),
                AverageScore = Math.Round(g.SelectMany(r => r.Scores).GroupBy(s => s.RoundId).Average(sg => sg.Sum(s => s.Strokes)), 1)
            })
            .OrderByDescending(fc => fc.RoundsPlayed)
            .Take(limit)
            .ToListAsync();

        return favoriteCourses;
    }
}