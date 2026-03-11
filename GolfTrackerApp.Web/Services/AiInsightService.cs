using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using GolfTrackerApp.Web.Data;
using GolfTrackerApp.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GolfTrackerApp.Web.Services
{
    public class AiInsightService : IAiInsightService
    {
        private readonly IAiRoutingService _aiRouting;
        private readonly IReportService _reportService;
        private readonly IPlayerService _playerService;
        private readonly IGolfClubService _clubService;
        private readonly IGolfCourseService _courseService;
        private readonly IRoundService _roundService;
        private readonly IAiAuditService _auditService;
        private readonly IAiChatService _chatService;
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AiInsightService> _logger;

        private record CachedInsight(
            AiInsightResult Result,
            DateTime DataWatermark,
            DateTime GeneratedAt);

        private static readonly ConcurrentDictionary<string, CachedInsight> _cache = new();

        public AiInsightService(
            IAiRoutingService aiRouting,
            IReportService reportService,
            IPlayerService playerService,
            IGolfClubService clubService,
            IGolfCourseService courseService,
            IRoundService roundService,
            IAiAuditService auditService,
            IAiChatService chatService,
            IDbContextFactory<ApplicationDbContext> contextFactory,
            IConfiguration configuration,
            ILogger<AiInsightService> logger)
        {
            _aiRouting = aiRouting;
            _reportService = reportService;
            _playerService = playerService;
            _clubService = clubService;
            _courseService = courseService;
            _roundService = roundService;
            _auditService = auditService;
            _chatService = chatService;
            _contextFactory = contextFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AiInsightResult> GetDashboardInsightsAsync(
            string userId, bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            if (!IsEnabled()) return DisabledResult();

            var cacheKey = $"dashboard_{userId}";

            var player = await _playerService.GetPlayerByApplicationUserIdAsync(userId);
            if (player == null)
                return new AiInsightResult { Success = false, ErrorMessage = "Player not found." };

            var lastDataChange = await GetLastDataChangeAsync(player.PlayerId);

            if (!forceRefresh && TryGetCachedWithWatermark(cacheKey, lastDataChange, out var cached))
                return cached;

            if (await _auditService.IsRateLimitedAsync(userId))
                return new AiInsightResult { Success = false, ErrorMessage = "Rate limit reached. Try again later." };

            var statsTask = _reportService.GetDashboardStatsAsync(userId);
            var scoringTask = _reportService.GetScoringDistributionAsync(player.PlayerId, null, null, null, null, null);
            var parTask = _reportService.GetPerformanceByParAsync(player.PlayerId, null, null, null, null, null);
            var coursesTask = _reportService.GetCourseHistoryAsync(userId, 6);
            var partnersTask = _reportService.GetPlayingPartnerSummaryAsync(userId, 5);

            await Task.WhenAll(statsTask, scoringTask, parTask, coursesTask, partnersTask);

            var stats = await statsTask;
            var scoring = await scoringTask;
            var par = await parTask;
            var courses = await coursesTask;
            var partners = await partnersTask;

            if (stats.TotalRounds == 0)
                return new AiInsightResult
                {
                    Success = true,
                    Content = "Record some rounds to unlock AI-powered insights about your game!"
                };

            var systemPrompt = BuildSystemPrompt();
            var userPrompt = BuildDashboardPrompt(player, stats, scoring, par, courses, partners);

            var stopwatch = Stopwatch.StartNew();
            var result = await _aiRouting.RouteCompletionAsync(
                systemPrompt, userPrompt, cancellationToken: cancellationToken);
            stopwatch.Stop();

            var logPrompts = _configuration.GetValue<bool>("AiInsights:AuditLogging:LogPrompts");
            var logResponses = _configuration.GetValue<bool>("AiInsights:AuditLogging:LogResponses");
            await _auditService.LogAsync(new AiAuditLog
            {
                ApplicationUserId = userId,
                InsightType = "Dashboard",
                ProviderName = result.ProviderUsed,
                ModelUsed = result.ModelUsed,
                PromptTokens = result.PromptTokens,
                CompletionTokens = result.CompletionTokens,
                TotalTokens = result.TokensUsed,
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                Success = result.Success,
                ErrorMessage = result.ErrorMessage,
                PromptSent = logPrompts ? userPrompt : null,
                ResponseReceived = logResponses ? result.Content : null
            });

            if (result.Success)
            {
                result.GeneratedAt = DateTime.UtcNow;
                CacheWithWatermark(cacheKey, result, lastDataChange);
            }
            return result;
        }

        public async Task<AiInsightResult> GetPlayerReportInsightsAsync(
            string userId, int playerId, int? courseId = null, int? holesPlayed = null,
            CancellationToken cancellationToken = default)
        {
            if (!IsEnabled()) return DisabledResult();

            var cacheKey = $"report_{userId}_{playerId}_{courseId}_{holesPlayed}";
            var lastDataChange = await GetLastDataChangeAsync(playerId);

            if (TryGetCachedWithWatermark(cacheKey, lastDataChange, out var cached))
                return cached;

            if (await _auditService.IsRateLimitedAsync(userId))
                return new AiInsightResult { Success = false, ErrorMessage = "Rate limit reached. Try again later." };

            // Determine if this is the logged-in user's own report or another player's
            var viewedPlayer = await _playerService.GetPlayerByIdAsync(playerId);
            var loggedInPlayer = await _playerService.GetPlayerByApplicationUserIdAsync(userId);
            var isOwnReport = loggedInPlayer != null && loggedInPlayer.PlayerId == playerId;

            var reportTask = _reportService.GetPlayerReportViewModelAsync(
                playerId, courseId, holesPlayed, null, null, null);
            var scoringTask = _reportService.GetScoringDistributionAsync(
                playerId, courseId, holesPlayed, null, null, null);
            var parTask = _reportService.GetPerformanceByParAsync(
                playerId, courseId, holesPlayed, null, null, null);

            await Task.WhenAll(reportTask, scoringTask, parTask);

            var report = await reportTask;
            var scoring = await scoringTask;
            var par = await parTask;

            if (report?.PerformanceData == null || !report.PerformanceData.Any())
                return new AiInsightResult { Success = true, Content = "Not enough data for analysis." };

            string systemPrompt;
            string userPrompt;

            if (!isOwnReport && loggedInPlayer != null)
            {
                // Viewing another player — provide comparison-focused insights
                var comparison = await _reportService.GetPlayerComparisonAsync(
                    loggedInPlayer.PlayerId, new List<int> { playerId }, courseId, holesPlayed, null, null, null);

                systemPrompt = BuildSystemPrompt();
                userPrompt = BuildComparisonPrompt(loggedInPlayer, viewedPlayer!, report, scoring, par, comparison, courseId, holesPlayed);
            }
            else
            {
                systemPrompt = BuildSystemPrompt();
                userPrompt = BuildPlayerReportPrompt(report, scoring, par, courseId, holesPlayed);
            }

            var stopwatch = Stopwatch.StartNew();
            var result = await _aiRouting.RouteCompletionAsync(
                systemPrompt, userPrompt, cancellationToken: cancellationToken);
            stopwatch.Stop();

            var logPrompts = _configuration.GetValue<bool>("AiInsights:AuditLogging:LogPrompts");
            var logResponses = _configuration.GetValue<bool>("AiInsights:AuditLogging:LogResponses");
            await _auditService.LogAsync(new AiAuditLog
            {
                ApplicationUserId = userId,
                InsightType = "PlayerReport",
                ProviderName = result.ProviderUsed,
                ModelUsed = result.ModelUsed,
                PromptTokens = result.PromptTokens,
                CompletionTokens = result.CompletionTokens,
                TotalTokens = result.TokensUsed,
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                Success = result.Success,
                ErrorMessage = result.ErrorMessage,
                PromptSent = logPrompts ? userPrompt : null,
                ResponseReceived = logResponses ? result.Content : null
            });

            if (result.Success)
            {
                result.GeneratedAt = DateTime.UtcNow;
                CacheWithWatermark(cacheKey, result, lastDataChange);
            }
            return result;
        }

        public async Task<AiInsightResult> GetClubInsightsAsync(string userId, int clubId,
            CancellationToken cancellationToken = default)
        {
            if (!IsEnabled()) return DisabledResult();

            var cacheKey = $"club_{userId}_{clubId}";
            var player = await _playerService.GetPlayerByApplicationUserIdAsync(userId);
            if (player == null)
                return new AiInsightResult { Success = false, ErrorMessage = "Player not found." };

            var lastDataChange = await GetLastDataChangeAsync(player.PlayerId);

            if (TryGetCachedWithWatermark(cacheKey, lastDataChange, out var cached))
                return cached;

            if (await _auditService.IsRateLimitedAsync(userId))
                return new AiInsightResult { Success = false, ErrorMessage = "Rate limit reached. Try again later." };

            var club = await _clubService.GetGolfClubByIdAsync(clubId);
            if (club == null)
                return new AiInsightResult { Success = false, ErrorMessage = "Golf club not found." };

            var scoringTask = _reportService.GetScoringDistributionForClubAsync(
                player.PlayerId, clubId, null, null, null, null);
            var performanceTask = _reportService.GetPlayerPerformanceForClubAsync(userId, clubId, 20);
            var roundCountTask = _roundService.GetRoundCountForClubAsync(userId, clubId);

            await Task.WhenAll(scoringTask, performanceTask, roundCountTask);

            var scoring = await scoringTask;
            var performance = await performanceTask;
            var roundCount = await roundCountTask;

            if (roundCount == 0)
                return new AiInsightResult { Success = true, Content = "Play some rounds at this club to unlock AI insights!" };

            var systemPrompt = BuildSystemPrompt();
            var userPrompt = BuildClubPrompt(player, club, scoring, performance, roundCount);

            var stopwatch = Stopwatch.StartNew();
            var result = await _aiRouting.RouteCompletionAsync(
                systemPrompt, userPrompt, cancellationToken: cancellationToken);
            stopwatch.Stop();

            var logPrompts = _configuration.GetValue<bool>("AiInsights:AuditLogging:LogPrompts");
            var logResponses = _configuration.GetValue<bool>("AiInsights:AuditLogging:LogResponses");
            await _auditService.LogAsync(new AiAuditLog
            {
                ApplicationUserId = userId,
                InsightType = "Club",
                ProviderName = result.ProviderUsed,
                ModelUsed = result.ModelUsed,
                PromptTokens = result.PromptTokens,
                CompletionTokens = result.CompletionTokens,
                TotalTokens = result.TokensUsed,
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                Success = result.Success,
                ErrorMessage = result.ErrorMessage,
                PromptSent = logPrompts ? userPrompt : null,
                ResponseReceived = logResponses ? result.Content : null
            });

            if (result.Success)
            {
                result.GeneratedAt = DateTime.UtcNow;
                CacheWithWatermark(cacheKey, result, lastDataChange);
            }
            return result;
        }

        public async Task<AiInsightResult> GetCourseInsightsAsync(string userId, int courseId,
            CancellationToken cancellationToken = default)
        {
            if (!IsEnabled()) return DisabledResult();

            var cacheKey = $"course_{userId}_{courseId}";
            var player = await _playerService.GetPlayerByApplicationUserIdAsync(userId);
            if (player == null)
                return new AiInsightResult { Success = false, ErrorMessage = "Player not found." };

            var lastDataChange = await GetLastDataChangeAsync(player.PlayerId);

            if (TryGetCachedWithWatermark(cacheKey, lastDataChange, out var cached))
                return cached;

            if (await _auditService.IsRateLimitedAsync(userId))
                return new AiInsightResult { Success = false, ErrorMessage = "Rate limit reached. Try again later." };

            var course = await _courseService.GetGolfCourseByIdAsync(courseId);
            if (course == null)
                return new AiInsightResult { Success = false, ErrorMessage = "Golf course not found." };

            var scoringTask = _reportService.GetScoringDistributionAsync(
                player.PlayerId, courseId, null, null, null, null);
            var parTask = _reportService.GetPerformanceByParAsync(
                player.PlayerId, courseId, null, null, null, null);
            var performanceTask = _reportService.GetPlayerPerformanceForCourseAsync(userId, courseId, 20);
            var roundCountTask = _roundService.GetRoundCountForCourseAsync(userId, courseId);

            await Task.WhenAll(scoringTask, parTask, performanceTask, roundCountTask);

            var scoring = await scoringTask;
            var par = await parTask;
            var performance = await performanceTask;
            var roundCount = await roundCountTask;

            if (roundCount == 0)
                return new AiInsightResult { Success = true, Content = "Play some rounds at this course to unlock AI insights!" };

            var systemPrompt = BuildSystemPrompt();
            var userPrompt = BuildCoursePrompt(player, course, scoring, par, performance, roundCount);

            var stopwatch = Stopwatch.StartNew();
            var result = await _aiRouting.RouteCompletionAsync(
                systemPrompt, userPrompt, cancellationToken: cancellationToken);
            stopwatch.Stop();

            var logPrompts = _configuration.GetValue<bool>("AiInsights:AuditLogging:LogPrompts");
            var logResponses = _configuration.GetValue<bool>("AiInsights:AuditLogging:LogResponses");
            await _auditService.LogAsync(new AiAuditLog
            {
                ApplicationUserId = userId,
                InsightType = "Course",
                ProviderName = result.ProviderUsed,
                ModelUsed = result.ModelUsed,
                PromptTokens = result.PromptTokens,
                CompletionTokens = result.CompletionTokens,
                TotalTokens = result.TokensUsed,
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                Success = result.Success,
                ErrorMessage = result.ErrorMessage,
                PromptSent = logPrompts ? userPrompt : null,
                ResponseReceived = logResponses ? result.Content : null
            });

            if (result.Success)
            {
                result.GeneratedAt = DateTime.UtcNow;
                CacheWithWatermark(cacheKey, result, lastDataChange);
            }
            return result;
        }

        public async Task<AiInsightResult> ChatAsync(
            string userId, string userMessage,
            int? sessionId = null,
            CancellationToken cancellationToken = default)
        {
            if (!IsEnabled()) return DisabledResult();
            if (await _auditService.IsRateLimitedAsync(userId))
                return new AiInsightResult { Success = false, ErrorMessage = "Rate limit reached. Try again later." };

            var player = await _playerService.GetPlayerByApplicationUserIdAsync(userId);
            if (player == null)
                return new AiInsightResult { Success = false, ErrorMessage = "Player not found." };

            AiChatSession? session;
            if (sessionId.HasValue)
                session = await _chatService.GetSessionAsync(sessionId.Value, userId);
            else
                session = await _chatService.CreateSessionAsync(userId, userMessage);

            if (session == null)
                return new AiInsightResult { Success = false, ErrorMessage = "Chat session not found." };

            await _chatService.AddMessageAsync(session.AiChatSessionId, "user", userMessage);

            var stats = await _reportService.GetDashboardStatsAsync(userId);

            var systemPrompt = """
                You are a friendly golf coach assistant. The user can ask you questions about
                their golf performance. You have access to their stats provided below.
                Answer conversationally. If you don't have enough data to answer, say so.
                Keep responses concise (under 200 words).
                """ + $"\n\nPlayer context:\n{BuildBriefPlayerContext(player, stats)}";

            var history = await _chatService.GetMessagesAsync(session.AiChatSessionId, 20);
            var fullPrompt = new StringBuilder();
            foreach (var msg in history.TakeLast(10))
                fullPrompt.AppendLine($"[{msg.Role}]: {msg.Content}");

            var stopwatch = Stopwatch.StartNew();
            var result = await _aiRouting.RouteCompletionAsync(
                systemPrompt, fullPrompt.ToString(),
                maxTokens: 300, cancellationToken: cancellationToken);
            stopwatch.Stop();

            if (result.Success)
                await _chatService.AddMessageAsync(session.AiChatSessionId, "assistant", result.Content);

            var logPrompts = _configuration.GetValue<bool>("AiInsights:AuditLogging:LogPrompts");
            var logResponses = _configuration.GetValue<bool>("AiInsights:AuditLogging:LogResponses");
            await _auditService.LogAsync(new AiAuditLog
            {
                ApplicationUserId = userId,
                InsightType = "Chat",
                ProviderName = result.ProviderUsed,
                ModelUsed = result.ModelUsed,
                PromptTokens = result.PromptTokens,
                CompletionTokens = result.CompletionTokens,
                TotalTokens = result.TokensUsed,
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                Success = result.Success,
                ErrorMessage = result.ErrorMessage,
                PromptSent = logPrompts ? fullPrompt.ToString() : null,
                ResponseReceived = logResponses ? result.Content : null,
                AiChatSessionId = session.AiChatSessionId
            });

            result.ChatSessionId = session.AiChatSessionId;
            return result;
        }

        private static string BuildSystemPrompt()
        {
            return """
                You are a friendly, knowledgeable golf coach analysing a player's performance data.
                Give concise, actionable insights. Use a warm but professional tone.
                Focus on patterns, trends, strengths, and specific areas for improvement.
                Reference actual numbers from the data provided.
                Do NOT make up data or assume information not provided.
                Format your response as 2-4 short bullet points using • as the bullet character.
                Keep total response under 150 words.
                """;
        }

        private static string BuildDashboardPrompt(
            Player player,
            DashboardStats stats,
            ScoringDistribution scoring,
            PerformanceByPar par,
            List<CourseHistoryItem> courses,
            List<PlayingPartnerSummary> partners)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Player: {player.FirstName} {player.LastName}");
            if (player.Handicap.HasValue)
                sb.AppendLine($"Handicap: {player.Handicap.Value}");

            sb.AppendLine($"\n--- Overall Stats ---");
            sb.AppendLine($"Total rounds: {stats.TotalRounds}");
            sb.AppendLine($"Best score: {stats.BestScore} at {stats.BestScoreCourseName}");
            sb.AppendLine($"Average score: {stats.AverageScore:F1}");
            sb.AppendLine($"Average to par: {stats.AverageToPar:+0.0;-0.0;0}");
            sb.AppendLine($"Lowest to par: {stats.LowestToPar}");
            sb.AppendLine($"18-hole rounds: {stats.EighteenHoleRounds}, 9-hole: {stats.NineHoleRounds}");
            sb.AppendLine($"Unique courses: {stats.UniqueCoursesPlayed}, clubs: {stats.UniqueClubsVisited}");
            sb.AppendLine($"Trend: {(stats.IsImprovingStreak ? "Improving" : "Declining")} over last {stats.CurrentStreak} rounds");

            sb.AppendLine($"\n--- Scoring Distribution ---");
            sb.AppendLine($"Eagles: {scoring.EagleCount} ({scoring.EaglePercentage:F1}%)");
            sb.AppendLine($"Birdies: {scoring.BirdieCount} ({scoring.BirdiePercentage:F1}%)");
            sb.AppendLine($"Pars: {scoring.ParCount} ({scoring.ParPercentage:F1}%)");
            sb.AppendLine($"Bogeys: {scoring.BogeyCount} ({scoring.BogeyPercentage:F1}%)");
            sb.AppendLine($"Double bogeys: {scoring.DoubleBogeyCount} ({scoring.DoubleBogeyPercentage:F1}%)");
            sb.AppendLine($"Triple+: {scoring.TripleBogeyOrWorseCount} ({scoring.TripleBogeyOrWorsePercentage:F1}%)");

            if (par.HasValidData)
            {
                sb.AppendLine($"\n--- Performance by Par ---");
                sb.AppendLine($"Par 3 avg: {par.Par3Average:F2} ({par.Par3RelativeToPar:+0.00;-0.00})");
                sb.AppendLine($"Par 4 avg: {par.Par4Average:F2} ({par.Par4RelativeToPar:+0.00;-0.00})");
                sb.AppendLine($"Par 5 avg: {par.Par5Average:F2} ({par.Par5RelativeToPar:+0.00;-0.00})");
            }

            if (courses.Any())
            {
                sb.AppendLine($"\n--- Recent Courses ---");
                foreach (var c in courses.Take(5))
                    sb.AppendLine($"  {c.CourseName} ({c.ClubName}): played {c.TimesPlayed}x, best {c.BestToPar:+0;-0;E} to par, last score {c.MostRecentScore}");
            }

            if (partners.Any())
            {
                sb.AppendLine($"\n--- Playing Partners ---");
                foreach (var p in partners)
                    sb.AppendLine($"  {p.PartnerName}: W{p.UserWins}/L{p.PartnerWins}/T{p.Ties}");
            }

            sb.AppendLine("\nProvide 2-4 key insights about this golfer's overall game.");
            return sb.ToString();
        }

        private static string BuildPlayerReportPrompt(
            PlayerReportViewModel report,
            ScoringDistribution scoring,
            PerformanceByPar par,
            int? courseId,
            int? holesPlayed)
        {
            var sb = new StringBuilder();
            var player = report.Player!;
            sb.AppendLine($"Player: {player.FirstName} {player.LastName}");
            if (player.Handicap.HasValue)
                sb.AppendLine($"Handicap: {player.Handicap.Value}");

            if (courseId.HasValue || holesPlayed.HasValue)
            {
                var filters = new List<string>();
                if (courseId.HasValue) filters.Add($"filtered to course ID {courseId}");
                if (holesPlayed.HasValue) filters.Add($"{holesPlayed}-hole rounds only");
                sb.AppendLine($"Filters: {string.Join(", ", filters)}");
            }

            sb.AppendLine($"\n--- Performance Data ({report.PerformanceData.Count} rounds) ---");
            var bestScore = report.PerformanceData.Min(d => d.TotalScore);
            var avgScore = report.PerformanceData.Average(d => (double)d.TotalScore);
            var avgToPar = report.PerformanceData.Average(d => (double)d.ScoreVsPar);
            sb.AppendLine($"Best score: {bestScore}");
            sb.AppendLine($"Average score: {avgScore:F1}");
            sb.AppendLine($"Average vs par: {avgToPar:+0.0;-0.0;0}");

            // Recent trend (last 5 rounds)
            var recent = report.PerformanceData.OrderByDescending(d => d.Date).Take(5).ToList();
            if (recent.Count >= 2)
            {
                sb.AppendLine($"\n--- Recent Trend (last {recent.Count} rounds) ---");
                foreach (var r in recent)
                    sb.AppendLine($"  {r.Date:dd MMM} at {r.CourseName}: {r.TotalScore} ({r.ScoreVsPar:+0;-0;E} vs par)");
            }

            sb.AppendLine($"\n--- Scoring Distribution ---");
            sb.AppendLine($"Eagles: {scoring.EagleCount} ({scoring.EaglePercentage:F1}%)");
            sb.AppendLine($"Birdies: {scoring.BirdieCount} ({scoring.BirdiePercentage:F1}%)");
            sb.AppendLine($"Pars: {scoring.ParCount} ({scoring.ParPercentage:F1}%)");
            sb.AppendLine($"Bogeys: {scoring.BogeyCount} ({scoring.BogeyPercentage:F1}%)");
            sb.AppendLine($"Double bogeys: {scoring.DoubleBogeyCount} ({scoring.DoubleBogeyPercentage:F1}%)");
            sb.AppendLine($"Triple+: {scoring.TripleBogeyOrWorseCount} ({scoring.TripleBogeyOrWorsePercentage:F1}%)");

            if (par.HasValidData)
            {
                sb.AppendLine($"\n--- Performance by Par ---");
                sb.AppendLine($"Par 3 avg: {par.Par3Average:F2} ({par.Par3RelativeToPar:+0.00;-0.00})");
                sb.AppendLine($"Par 4 avg: {par.Par4Average:F2} ({par.Par4RelativeToPar:+0.00;-0.00})");
                sb.AppendLine($"Par 5 avg: {par.Par5Average:F2} ({par.Par5RelativeToPar:+0.00;-0.00})");
            }

            sb.AppendLine("\nProvide 2-4 specific insights about this player's performance, highlighting strengths and areas for improvement.");
            return sb.ToString();
        }

        private static string BuildComparisonPrompt(
            Player loggedInPlayer,
            Player viewedPlayer,
            PlayerReportViewModel viewedReport,
            ScoringDistribution viewedScoring,
            PerformanceByPar viewedPar,
            PlayerComparisonResult comparison,
            int? courseId,
            int? holesPlayed)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"You (the golfer asking): {loggedInPlayer.FirstName} {loggedInPlayer.LastName}");
            if (loggedInPlayer.Handicap.HasValue)
                sb.AppendLine($"Your handicap: {loggedInPlayer.Handicap.Value}");

            sb.AppendLine($"\nYou are viewing the report for: {viewedPlayer.FirstName} {viewedPlayer.LastName}");
            if (viewedPlayer.Handicap.HasValue)
                sb.AppendLine($"Their handicap: {viewedPlayer.Handicap.Value}");

            if (courseId.HasValue || holesPlayed.HasValue)
            {
                var filters = new List<string>();
                if (courseId.HasValue) filters.Add($"filtered to course ID {courseId}");
                if (holesPlayed.HasValue) filters.Add($"{holesPlayed}-hole rounds only");
                sb.AppendLine($"Filters: {string.Join(", ", filters)}");
            }

            // Head-to-head comparison data
            // Note: GetPlayerComparisonAsync only sets SharedRounds/Wins/Losses on the NON-primary player,
            // so we use theirSummary for shared round count, and their Wins = your Losses (and vice versa)
            var yourSummary = comparison.Summaries.FirstOrDefault(s => s.PlayerId == loggedInPlayer.PlayerId);
            var theirSummary = comparison.Summaries.FirstOrDefault(s => s.PlayerId == viewedPlayer.PlayerId);

            if (yourSummary != null && theirSummary != null && theirSummary.SharedRounds > 0)
            {
                sb.AppendLine($"\n--- Head-to-Head ({theirSummary.SharedRounds} shared rounds) ---");
                sb.AppendLine($"Your wins: {theirSummary.Losses}, Their wins: {theirSummary.Wins}, Ties: {theirSummary.Ties}");
                sb.AppendLine($"Your avg score (all rounds): {yourSummary.AverageScore:F1}, Their avg score (all rounds): {theirSummary.AverageScore:F1}");
                sb.AppendLine($"Your avg vs par: {yourSummary.AverageToPar:+0.0;-0.0;0}, Their avg vs par: {theirSummary.AverageToPar:+0.0;-0.0;0}");
                sb.AppendLine($"Your best: {yourSummary.BestScore}, Their best: {theirSummary.BestScore}");
            }
            else
            {
                sb.AppendLine("\nNo shared rounds found between you and this player.");
            }

            // Their overall stats for context
            if (viewedReport.PerformanceData?.Any() == true)
            {
                sb.AppendLine($"\n--- {viewedPlayer.FirstName}'s Overall Stats ({viewedReport.PerformanceData.Count} rounds) ---");
                var avgScore = viewedReport.PerformanceData.Average(d => (double)d.TotalScore);
                var avgToPar = viewedReport.PerformanceData.Average(d => (double)d.ScoreVsPar);
                sb.AppendLine($"Avg score: {avgScore:F1}, Avg vs par: {avgToPar:+0.0;-0.0;0}");
            }

            sb.AppendLine("\nProvide 2-4 insights from the perspective of the logged-in golfer about how they compare to this player. " +
                "Focus on head-to-head record, relative strengths, and what they could learn from or leverage against this playing partner.");
            return sb.ToString();
        }

        private static string BuildClubPrompt(
            Player player,
            GolfClub club,
            ScoringDistribution scoring,
            List<PlayerPerformanceDataPoint> performance,
            int roundCount)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Player: {player.FirstName} {player.LastName}");
            if (player.Handicap.HasValue)
                sb.AppendLine($"Handicap: {player.Handicap.Value}");

            sb.AppendLine($"\n--- Club: {club.Name} ---");
            sb.AppendLine($"Total rounds at this club: {roundCount}");
            if (club.GolfCourses?.Any() == true)
                sb.AppendLine($"Courses: {string.Join(", ", club.GolfCourses.Select(c => $"{c.Name} (Par {c.DefaultPar})"))}");

            if (performance.Any())
            {
                var bestScore = performance.Min(d => d.TotalScore);
                var avgScore = performance.Average(d => (double)d.TotalScore);
                var avgToPar = performance.Average(d => (double)d.ScoreVsPar);
                sb.AppendLine($"Best score: {bestScore}");
                sb.AppendLine($"Average score: {avgScore:F1}");
                sb.AppendLine($"Average vs par: {avgToPar:+0.0;-0.0;0}");

                // Trend
                var recent = performance.OrderByDescending(d => d.Date).Take(5).ToList();
                if (recent.Count >= 2)
                {
                    sb.AppendLine($"\n--- Recent rounds ---");
                    foreach (var r in recent)
                        sb.AppendLine($"  {r.Date:dd MMM} at {r.CourseName}: {r.TotalScore} ({r.ScoreVsPar:+0;-0;E})");
                }
            }

            if (scoring.TotalHoles > 0)
            {
                sb.AppendLine($"\n--- Scoring Distribution at this club ---");
                sb.AppendLine($"Eagles: {scoring.EagleCount} ({scoring.EaglePercentage:F1}%)");
                sb.AppendLine($"Birdies: {scoring.BirdieCount} ({scoring.BirdiePercentage:F1}%)");
                sb.AppendLine($"Pars: {scoring.ParCount} ({scoring.ParPercentage:F1}%)");
                sb.AppendLine($"Bogeys: {scoring.BogeyCount} ({scoring.BogeyPercentage:F1}%)");
                sb.AppendLine($"Double bogeys: {scoring.DoubleBogeyCount} ({scoring.DoubleBogeyPercentage:F1}%)");
                sb.AppendLine($"Triple+: {scoring.TripleBogeyOrWorseCount} ({scoring.TripleBogeyOrWorsePercentage:F1}%)");
            }

            sb.AppendLine($"\nProvide 2-4 insights about this player's performance at {club.Name}, including trends and areas to focus on.");
            return sb.ToString();
        }

        private static string BuildCoursePrompt(
            Player player,
            GolfCourse course,
            ScoringDistribution scoring,
            PerformanceByPar par,
            List<PlayerPerformanceDataPoint> performance,
            int roundCount)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Player: {player.FirstName} {player.LastName}");
            if (player.Handicap.HasValue)
                sb.AppendLine($"Handicap: {player.Handicap.Value}");

            sb.AppendLine($"\n--- Course: {course.Name} (Par {course.DefaultPar}, {course.NumberOfHoles} holes) ---");
            sb.AppendLine($"Club: {course.GolfClub?.Name ?? "Unknown"}");
            sb.AppendLine($"Total rounds at this course: {roundCount}");

            if (performance.Any())
            {
                var bestScore = performance.Min(d => d.TotalScore);
                var avgScore = performance.Average(d => (double)d.TotalScore);
                var avgToPar = performance.Average(d => (double)d.ScoreVsPar);
                sb.AppendLine($"Best score: {bestScore}");
                sb.AppendLine($"Average score: {avgScore:F1}");
                sb.AppendLine($"Average vs par: {avgToPar:+0.0;-0.0;0}");

                var recent = performance.OrderByDescending(d => d.Date).Take(5).ToList();
                if (recent.Count >= 2)
                {
                    sb.AppendLine($"\n--- Recent rounds ---");
                    foreach (var r in recent)
                        sb.AppendLine($"  {r.Date:dd MMM}: {r.TotalScore} ({r.ScoreVsPar:+0;-0;E})");
                }
            }

            if (scoring.TotalHoles > 0)
            {
                sb.AppendLine($"\n--- Scoring Distribution ---");
                sb.AppendLine($"Eagles: {scoring.EagleCount} ({scoring.EaglePercentage:F1}%)");
                sb.AppendLine($"Birdies: {scoring.BirdieCount} ({scoring.BirdiePercentage:F1}%)");
                sb.AppendLine($"Pars: {scoring.ParCount} ({scoring.ParPercentage:F1}%)");
                sb.AppendLine($"Bogeys: {scoring.BogeyCount} ({scoring.BogeyPercentage:F1}%)");
                sb.AppendLine($"Double bogeys: {scoring.DoubleBogeyCount} ({scoring.DoubleBogeyPercentage:F1}%)");
                sb.AppendLine($"Triple+: {scoring.TripleBogeyOrWorseCount} ({scoring.TripleBogeyOrWorsePercentage:F1}%)");
            }

            if (par.HasValidData)
            {
                sb.AppendLine($"\n--- Performance by Par ---");
                sb.AppendLine($"Par 3 avg: {par.Par3Average:F2} ({par.Par3RelativeToPar:+0.00;-0.00})");
                sb.AppendLine($"Par 4 avg: {par.Par4Average:F2} ({par.Par4RelativeToPar:+0.00;-0.00})");
                sb.AppendLine($"Par 5 avg: {par.Par5Average:F2} ({par.Par5RelativeToPar:+0.00;-0.00})");
            }

            sb.AppendLine($"\nProvide 2-4 insights about this player's performance at {course.Name}, including course-specific strengths and weaknesses.");
            return sb.ToString();
        }

        private static string BuildBriefPlayerContext(Player player, DashboardStats stats)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Player: {player.FirstName} {player.LastName}");
            if (player.Handicap.HasValue)
                sb.AppendLine($"Handicap: {player.Handicap.Value}");
            sb.AppendLine($"Total rounds: {stats.TotalRounds}");
            if (stats.AverageScore.HasValue)
                sb.AppendLine($"Average score: {stats.AverageScore.Value:F1}");
            if (stats.BestScore.HasValue)
                sb.AppendLine($"Best score: {stats.BestScore.Value}");
            return sb.ToString();
        }

        private bool IsEnabled() =>
            _configuration.GetValue<bool>("AiInsights:Enabled");

        private static AiInsightResult DisabledResult() =>
            new() { Success = false, ErrorMessage = "AI Insights are not enabled." };

        private bool TryGetCachedWithWatermark(string key, DateTime? lastDataChange, out AiInsightResult result)
        {
            if (_cache.TryGetValue(key, out var cached)
                && lastDataChange.HasValue
                && cached.DataWatermark >= lastDataChange.Value)
            {
                result = new AiInsightResult
                {
                    Success = cached.Result.Success,
                    Content = cached.Result.Content,
                    ProviderUsed = cached.Result.ProviderUsed,
                    ModelUsed = cached.Result.ModelUsed,
                    TokensUsed = cached.Result.TokensUsed,
                    PromptTokens = cached.Result.PromptTokens,
                    CompletionTokens = cached.Result.CompletionTokens,
                    GeneratedAt = cached.GeneratedAt
                };

                var staleMonths = _configuration.GetValue<int>("AiInsights:StaleInsightMonths");
                var age = DateTime.UtcNow - cached.GeneratedAt;
                if (staleMonths > 0 && age.TotalDays > staleMonths * 30)
                {
                    result.StaleMessage = $"This insight is {(int)(age.TotalDays / 30)} months old — go play some more golf!";
                }

                return true;
            }
            result = default!;
            return false;
        }

        private static void CacheWithWatermark(string key, AiInsightResult result, DateTime? lastDataChange)
        {
            if (lastDataChange.HasValue)
                _cache[key] = new CachedInsight(result, lastDataChange.Value, DateTime.UtcNow);
        }

        private async Task<DateTime?> GetLastDataChangeAsync(int playerId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Rounds
                .Where(r => r.Scores.Any(s => s.PlayerId == playerId))
                .OrderByDescending(r => r.DatePlayed)
                .Select(r => r.DatePlayed)
                .FirstOrDefaultAsync();
        }
    }
}
