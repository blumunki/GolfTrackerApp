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
            IAiAuditService auditService,
            IAiChatService chatService,
            IDbContextFactory<ApplicationDbContext> contextFactory,
            IConfiguration configuration,
            ILogger<AiInsightService> logger)
        {
            _aiRouting = aiRouting;
            _reportService = reportService;
            _playerService = playerService;
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

        public Task<AiInsightResult> GetPlayerReportInsightsAsync(
            int playerId, int? courseId = null, int? holesPlayed = null,
            CancellationToken cancellationToken = default)
        {
            // Will be implemented in Phase 4
            return Task.FromResult(new AiInsightResult
            {
                Success = false,
                ErrorMessage = "Player report insights coming soon."
            });
        }

        public Task<AiInsightResult> GetClubInsightsAsync(string userId, int clubId,
            CancellationToken cancellationToken = default)
        {
            // Will be implemented in Phase 4
            return Task.FromResult(new AiInsightResult
            {
                Success = false,
                ErrorMessage = "Club insights coming soon."
            });
        }

        public Task<AiInsightResult> GetCourseInsightsAsync(string userId, int courseId,
            CancellationToken cancellationToken = default)
        {
            // Will be implemented in Phase 4
            return Task.FromResult(new AiInsightResult
            {
                Success = false,
                ErrorMessage = "Course insights coming soon."
            });
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
