using GolfTrackerApp.Web.Data;
using GolfTrackerApp.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GolfTrackerApp.Web.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;

    public ReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PlayerPerformanceDataPoint>> GetPlayerPerformanceAsync(int playerId, int? courseId, int? holesPlayed, RoundTypeOption? roundType, DateTime? startDate, DateTime? endDate)
    {
        var query = _context.Rounds
            .Where(r => r.RoundPlayers.Any(rp => rp.PlayerId == playerId) && r.Status == RoundCompletionStatus.Completed);

        // Apply filters
        if (courseId.HasValue && courseId > 0)
        {
            query = query.Where(r => r.GolfCourseId == courseId.Value);
        }
        if (holesPlayed.HasValue && holesPlayed > 0)
        {
            query = query.Where(r => r.HolesPlayed == holesPlayed.Value);
        }
        if (roundType.HasValue)
        {
            query = query.Where(r => r.RoundType == roundType.Value);
        }
        if (startDate.HasValue)
        {
            query = query.Where(r => r.DatePlayed.Date >= startDate.Value.Date);
        }
        if (endDate.HasValue)
        {
            query = query.Where(r => r.DatePlayed.Date <= endDate.Value.Date);
        }

        var performanceData = await query
            .Include(r => r.Scores)
            .ThenInclude(s => s.Hole)
            .Include(r => r.GolfCourse)
            .ThenInclude(gc => gc!.GolfClub) // Eager load GolfClub
            .OrderBy(r => r.DatePlayed)
            .Select(r => new PlayerPerformanceDataPoint
            {
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
        var partners = await _context.RoundPlayers
            .AsNoTracking()
            .Where(rp => userRoundIds.Contains(rp.RoundId) && rp.PlayerId != currentPlayerId)
            .Select(rp => rp.Player)
            .Distinct()
            .ToListAsync();

        var summaryList = new List<PlayingPartnerSummary>();

        foreach (var partner in partners)
        {
            // VVV --- THIS IS THE DEFINITIVE FIX --- VVV
            // This explicit check guarantees to the compiler that 'partner' is not null for the rest of the loop.
            if (partner is null)
            {
                continue;
            }
            // ^^^ ----------------------------------- ^^^

            // Find all rounds played together
            var roundsTogether = await _context.Rounds
                .AsNoTracking()
                .Where(r => r.RoundPlayers.Any(rp => rp.PlayerId == currentPlayerId) &&
                            r.RoundPlayers.Any(rp => rp.PlayerId == partner.PlayerId)) // Now safe
                .Include(r => r.Scores)
                .ToListAsync();

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
                Date = r.DatePlayed,
                TotalScore = r.Scores.Where(s => s.PlayerId == player.PlayerId).Sum(s => s.Strokes),
                TotalPar = r.Scores.Where(s => s.PlayerId == player.PlayerId).Sum(s => s.Hole!.Par)
            })
            .OrderBy(d => d.Date) // Re-order by date ascending for the chart
            .ToListAsync();

        return performanceData;
    }

    public async Task<PlayerReportViewModel> GetPlayerReportViewModelAsync(int playerId, int? courseId, int? holesPlayed, RoundTypeOption? roundType, DateTime? startDate, DateTime? endDate)
    {
        var playerTask = _context.Players.AsNoTracking().FirstOrDefaultAsync(p => p.PlayerId == playerId);
        var coursesTask = _context.GolfCourses.AsNoTracking().Include(c => c.GolfClub).ToListAsync();

        await Task.WhenAll(playerTask, coursesTask);

        var performanceData = await GetPlayerPerformanceAsync(playerId, courseId, holesPlayed, roundType, startDate, endDate);

        return new PlayerReportViewModel
        {
            Player = await playerTask,
            FilterCourses = await coursesTask,
            PerformanceData = performanceData
        };
    }
}