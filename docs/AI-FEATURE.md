# AI Insights Feature — Implementation Specification

## 1. Feature Overview

The AI Insights feature adds natural-language golf performance analysis powered by multiple AI providers with automatic failover. Users will see AI-generated insights embedded within existing dashboard and report pages on both web and mobile, as well as a dedicated AI chat page for free-form questions about their golf data.

### 1.1 Goals
- Surface actionable insights from the user's round, score, and course data
- Support multiple AI providers (with failover) so no single vendor dependency
- Integrate seamlessly into existing pages without disrupting current UX
- Deliver on both web (Blazor Server) and mobile (MAUI Blazor Hybrid)
- Keep costs manageable through caching, token limits, and rate-limiting

### 1.2 User Experience Summary

| Location | What the User Sees |
|---|---|
| Web Dashboard ([Home.razor](../GolfTrackerApp.Web/Components/Pages/Home.razor)) | New "AI Insights" widget card in the dashboard grid showing 2–3 bullet-point observations |
| Web Player Report ([PlayerReport.razor](../GolfTrackerApp.Web/Components/Pages/Players/PlayerReport.razor)) | Expandable "AI Analysis" panel below the summary strip |
| Web Golf Club Detail ([GolfClubDetails.razor](../GolfTrackerApp.Web/Components/Pages/GolfClubs/GolfClubDetails.razor)) | AI-generated course strategy tips (when the user has rounds at that club) |
| Web Golf Course Detail ([GolfCourseDetails.razor](../GolfTrackerApp.Web/Components/Pages/GolfCourses/GolfCourseDetails.razor)) | Per-course AI insight panel with hole-by-hole tendencies |
| Mobile Dashboard ([Home.razor](../GolfTrackerApp.Mobile/Components/Pages/Home.razor)) | New `AiInsightsWidget` below existing dashboard widgets |
| Mobile Player Report ([PlayerReportPage.razor](../GolfTrackerApp.Mobile/Components/Pages/PlayerReportPage.razor)) | AI analysis section at the bottom of the report |
| Dedicated AI Chat (new page) | Full-page conversational interface for free-form golf data questions |

---

## 2. Architecture

### 2.1 System Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          GolfTrackerApp.Web                             │
│                                                                         │
│  ┌──────────────────┐     ┌──────────────────────────────────────────┐  │
│  │  Blazor Pages     │     │  InsightsController                      │  │
│  │  (Home, Report,   │     │  [Route("api/[controller]")]             │  │
│  │   Club, Course)   │     │  : BaseApiController                     │  │
│  └────────┬──────────┘     └──────────────┬──────────────────────────┘  │
│           │ DI                             │ DI                          │
│           ▼                                ▼                             │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │                      IAiInsightService                           │   │
│  │  GetDashboardInsightsAsync(userId)                               │   │
│  │  GetPlayerReportInsightsAsync(playerId, filters)                 │   │
│  │  GetClubInsightsAsync(userId, clubId)                            │   │
│  │  GetCourseInsightsAsync(userId, courseId)                        │   │
│  │  ChatAsync(userId, message, sessionId)                           │   │
│  └────────────────────────────┬─────────────────────────────────────┘   │
│                               │                                         │
│                Uses existing services for data:                         │
│                IReportService, IRoundService, IPlayerService,           │
│                IGolfCourseService, IScoreService                        │
│                               │                                         │
│                               ▼                                         │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │                      IAiProviderService                          │   │
│  │  GenerateCompletionAsync(systemPrompt, userPrompt, options)      │   │
│  │                                                                  │   │
│  │  Implementations:                                                │   │
│  │    OpenAiProviderService                                         │   │
│  │    AnthropicProviderService                                      │   │
│  │    GeminiProviderService                                         │   │
│  │    GrokProviderService                                           │   │
│  │    MistralProviderService                                        │   │
│  │    DeepSeekProviderService                                       │   │
│  │    MetaLlamaProviderService                                      │   │
│  │    ManusProviderService                                          │   │
│  └──────────────────────────────┬───────────────────────────────────┘   │
│                                 │                                       │
│  ┌──────────────────────────────┴───────────────────────────────────┐   │
│  │                      IAiRoutingService                           │   │
│  │  RouteCompletionAsync(systemPrompt, userPrompt, options)         │   │
│  │                                                                  │   │
│  │  - Provider priority list from configuration                     │   │
│  │  - Automatic failover: try providers in order until one succeeds │   │
│  │  - Circuit breaker: temporarily skip providers returning errors  │   │
│  │  - Timeout enforcement per-provider                              │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                       GolfTrackerApp.Mobile                             │
│                                                                         │
│  ┌──────────────────┐     ┌──────────────────────────────────────────┐  │
│  │  Blazor Pages     │     │  InsightsApiService                      │  │
│  │  (Home, Report,   │────▶│  : IInsightsApiService                   │  │
│  │   AiChat)         │     │  Calls api/insights/* endpoints          │  │
│  └──────────────────┘     └──────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
```

### 2.2 Design Decisions

| Decision | Rationale |
|---|---|
| AI calls only on the server, never from mobile | Keeps API keys server-side; centralised rate-limiting; no client SDK bloat |
| `IAiInsightService` calls existing `IReportService` / `IRoundService` for data | No new database queries or EF changes — reuses tested data-access methods |
| Data-watermark caching (not time-based) | Only regenerate insights when underlying data has changed; avoids wasting tokens on identical data |
| Provider enabled/disabled in database, not config | Administrators toggle providers via admin UI; avoids stuffing operational state into env vars or user-secrets |
| Only API keys in user-secrets / env vars | Secrets stores are for secrets; operational config (enabled, priority) belongs in the DB; static config (model, endpoint) belongs in appsettings.json |
| Separate `IAiProviderService` per vendor | Each provider's HTTP contract is different; clean separation of concerns |
| `IAiRoutingService` wraps provider selection | Central failover logic; easy to add/remove providers without touching consumers |

---

## 3. Configuration

### 3.1 appsettings Structure

The `appsettings.json` (base) contains **non-secret** configuration only — model names, endpoints, priorities, timeouts, and feature-level defaults. **API keys** are stored in user-secrets (dev) or environment variables / Azure Key Vault (production). **Provider enabled/disabled state** is managed in the database via the admin UI (see Section 3.5).

```json
{
  "AiInsights": {
    "Enabled": false,
    "MaxTokens": 500,
    "Temperature": 0.7,
    "CacheMinutes": 60,
    "StaleInsightMonths": 3,
    "RateLimitPerUserPerHour": 20,
    "Providers": {
      "OpenAI": {
        "Priority": 1,
        "ApiKey": "",
        "Model": "gpt-4o-mini",
        "Endpoint": "https://api.openai.com/v1/chat/completions",
        "TimeoutSeconds": 30
      },
      "Anthropic": {
        "Priority": 2,
        "ApiKey": "",
        "Model": "claude-sonnet-4-20250514",
        "Endpoint": "https://api.anthropic.com/v1/messages",
        "TimeoutSeconds": 30
      },
      "Gemini": {
        "Priority": 3,
        "ApiKey": "",
        "Model": "gemini-3.1-flash-lite-preview",
        "Endpoint": "https://generativelanguage.googleapis.com/v1beta/models",
        "TimeoutSeconds": 30
      },
      "Grok": {
        "Priority": 4,
        "ApiKey": "",
        "Model": "grok-3-mini",
        "Endpoint": "https://api.x.ai/v1/chat/completions",
        "TimeoutSeconds": 30
      },
      "Mistral": {
        "Priority": 5,
        "ApiKey": "",
        "Model": "mistral-small-latest",
        "Endpoint": "https://api.mistral.ai/v1/chat/completions",
        "TimeoutSeconds": 30
      },
      "DeepSeek": {
        "Priority": 6,
        "ApiKey": "",
        "Model": "deepseek-chat",
        "Endpoint": "https://api.deepseek.com/v1/chat/completions",
        "TimeoutSeconds": 30
      },
      "MetaLlama": {
        "Priority": 7,
        "ApiKey": "",
        "Model": "llama-3.1-70b",
        "Endpoint": "",
        "TimeoutSeconds": 30
      },
      "Manus": {
        "Priority": 8,
        "ApiKey": "",
        "Model": "",
        "Endpoint": "",
        "TimeoutSeconds": 30
      }
    }
  }
}
```

> **Important — what goes where:**
> | Setting | Where | Example |
> |---|---|---|
> | API keys | User-secrets (dev) / env vars (prod) | `dotnet user-secrets set "AiInsights:Providers:Anthropic:ApiKey" "sk-..."` |
> | Provider enabled/disabled + priority | Database (`AiProviderSettings` table), managed via admin UI | See Section 3.5 |
> | Model, endpoint, timeout | `appsettings.json` | Committed to source |
> | Feature-level on/off (`AiInsights:Enabled`) | `appsettings.json` / `appsettings.Development.json` | `true` in dev, `false` in base |

> **Note**: Providers are a named dictionary (not an array), so user-secrets use clear names:
> `dotnet user-secrets set "AiInsights:Providers:Anthropic:ApiKey" "sk-..."`.
> The `Name` property is populated automatically from the dictionary key at runtime.

### 3.2 Configuration Access Pattern

Follow the existing codebase pattern of direct `IConfiguration` access (no `IOptions<T>`):

```csharp
// In service constructors or methods:
var enabled = _configuration.GetValue<bool>("AiInsights:Enabled");
var maxTokens = _configuration.GetValue<int>("AiInsights:MaxTokens");
var providers = _configuration.GetSection("AiInsights:Providers").Get<List<AiProviderConfig>>();
```

### 3.3 AiProviderConfig Model

Create in `GolfTrackerApp.Web/Models/AiProviderConfig.cs`:

```csharp
public class AiProviderConfig
{
    public string Name { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}
```

> **Note**: `Enabled` is NOT on this model — it lives in the `AiProviderSettings` database table (Section 3.5), so administrators can toggle providers on/off from the admin UI without redeploying or touching environment variables.

### 3.4 Security — API Key Storage

- **Development**: `dotnet user-secrets set "AiInsights:Providers:{Name}:ApiKey" "key"` — the ONLY values stored in user-secrets are API keys
- **Production**: Environment variables (`AiInsights__Providers__{Name}__ApiKey`) or Azure Key Vault — never commit keys to source
- **Never in user-secrets or env vars**: Enabled/disabled flags, model names, priorities, timeouts — these are either in `appsettings.json` (committed) or managed via the admin UI (database)
- API keys are ONLY accessed server-side; they never reach the mobile app

### 3.5 Provider Settings — Database-Managed (Admin UI)

Provider on/off state and priority overrides are stored in a new `AiProviderSettings` database table, managed exclusively through the admin UI. This avoids the anti-pattern of stuffing operational config into user-secrets or environment variables.

#### 3.5.1 AiProviderSettings Entity

```csharp
// GolfTrackerApp.Web/Models/AiProviderSettings.cs

public class AiProviderSettings
{
    public int AiProviderSettingsId { get; set; }                    // PK
    [Required][StringLength(50)] public string ProviderName { get; set; } = string.Empty; // e.g. "Anthropic"
    public bool Enabled { get; set; }                                // Admin toggle
    public int Priority { get; set; }                                // Admin-adjustable priority
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [StringLength(450)] public string? UpdatedByUserId { get; set; } // FK → ApplicationUser (who toggled it)
}
```

#### 3.5.2 Bootstrap / Seeding

On app startup, if the `AiProviderSettings` table is empty, seed it from the `appsettings.json` provider entries with `Enabled = false` (all off by default — an admin must explicitly enable providers). Subsequent starts read from the database.

```csharp
// In startup (after EnsureNewTablesExist or migration):
await SeedAiProviderSettingsAsync(app.Services);

static async Task SeedAiProviderSettingsAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
    await using var context = await contextFactory.CreateDbContextAsync();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    
    if (await context.AiProviderSettings.AnyAsync()) return; // Already seeded
    
    var providersSection = config.GetSection("AiInsights:Providers");
    foreach (var child in providersSection.GetChildren())
    {
        context.AiProviderSettings.Add(new AiProviderSettings
        {
            ProviderName = child.Key,
            Enabled = false, // Off by default — admin enables
            Priority = child.GetValue<int>("Priority")
        });
    }
    await context.SaveChangesAsync();
}
```

#### 3.5.3 How AiRoutingService Uses Provider Settings

The routing service merges config from `appsettings.json` (model, endpoint, API key) with database settings (enabled, priority):

```csharp
// In AiRoutingService.RouteCompletionAsync:
var providersSection = _configuration.GetSection("AiInsights:Providers");
var dbSettings = await GetProviderSettingsAsync(); // from AiProviderSettings table

var providers = new List<AiProviderConfig>();
foreach (var child in providersSection.GetChildren())
{
    var config = child.Get<AiProviderConfig>() ?? new();
    config.Name = child.Key;
    
    // Merge DB settings (enabled + priority override)
    var dbEntry = dbSettings.FirstOrDefault(s => s.ProviderName == config.Name);
    if (dbEntry == null || !dbEntry.Enabled) continue; // Skip if not enabled in DB
    config.Priority = dbEntry.Priority; // DB priority takes precedence
    
    if (string.IsNullOrEmpty(config.ApiKey)) continue; // Skip if no API key configured
    providers.Add(config);
}

var ordered = providers.OrderBy(p => p.Priority).ToList();
```

### 3.6 Data Freshness — Smart Cache Invalidation

AI insights should only be regenerated when the underlying data has changed. Requesting a new AI completion against the same data wastes tokens and money.

#### 3.6.1 Concept

```
┌──────────────────────────┐     ┌──────────────────────────┐
│ Last Data Change         │     │ Last Insight Generated    │
│ (most recent round date  │     │ (cached insight timestamp)│
│  for this user/context)  │     │                           │
└────────────┬─────────────┘     └────────────┬──────────────┘
             │                                │
             ▼                                ▼
    ┌─────────────────────────────────────────────────┐
    │ Data changed since last insight?                 │
    │                                                  │
    │ YES → Request new AI insight                     │
    │ NO  → Return cached insight                      │
    │       + if insight > StaleInsightMonths old:      │
    │         show "Insight is X months old — go play   │
    │         some more golf!" note                    │
    └─────────────────────────────────────────────────┘
```

#### 3.6.2 Implementation

**Determine "last data change"**: Query the most recent `Round.DatePlayed` (or `Round.CreatedAt`) for the user. This is the data watermark — if no new rounds exist since the last insight was generated, the data hasn't changed.

```csharp
// In AiInsightService — replaces the simple time-based cache:
private async Task<DateTime?> GetLastDataChangeAsync(string userId)
{
    // Uses existing IRoundService or direct query
    var player = await _playerService.GetPlayerByApplicationUserIdAsync(userId);
    if (player == null) return null;
    
    await using var context = await _contextFactory.CreateDbContextAsync();
    return await context.Rounds
        .Where(r => r.Scores.Any(s => s.PlayerId == player.PlayerId))
        .OrderByDescending(r => r.DatePlayed)
        .Select(r => r.DatePlayed)
        .FirstOrDefaultAsync();
}
```

**Cache key includes data watermark**: The cached insight is stored with the data watermark timestamp. On subsequent requests, if the watermark hasn't changed, the cached insight is returned directly — no AI call.

```csharp
private static readonly ConcurrentDictionary<string, CachedInsight> _cache = new();

private record CachedInsight(
    AiInsightResult Result,
    DateTime DataWatermark,   // When the underlying data was last updated
    DateTime GeneratedAt);    // When the AI insight was generated

public async Task<AiInsightResult> GetDashboardInsightsAsync(...)
{
    var lastDataChange = await GetLastDataChangeAsync(userId);
    var cacheKey = $"dashboard_{userId}";
    
    if (_cache.TryGetValue(cacheKey, out var cached)
        && lastDataChange.HasValue
        && cached.DataWatermark >= lastDataChange.Value)
    {
        // Data hasn't changed — return cached insight
        var result = cached.Result;
        
        // Check staleness
        var staleMonths = _configuration.GetValue<int>("AiInsights:StaleInsightMonths");
        var age = DateTime.UtcNow - cached.GeneratedAt;
        if (staleMonths > 0 && age.TotalDays > staleMonths * 30)
        {
            result = result with { }; // Copy
            result.StaleMessage = $"This insight is {(int)(age.TotalDays / 30)} months old — go play some more golf!";
        }
        return result;
    }
    
    // Data has changed (or no cache) — request new AI insight
    // ... existing logic ...
    
    // Cache with watermark
    if (result.Success && lastDataChange.HasValue)
        _cache[cacheKey] = new CachedInsight(result, lastDataChange.Value, DateTime.UtcNow);
    return result;
}
```

#### 3.6.3 AiInsightResult — Staleness Property

Add to `AiInsightResult`:

```csharp
public string? StaleMessage { get; set; }  // e.g. "This insight is 4 months old — go play some more golf!"
public DateTime? GeneratedAt { get; set; }  // When the insight was originally generated
```

The UI renders this as a subtle note below the insight content:

```razor
@if (!string.IsNullOrEmpty(aiInsight.StaleMessage))
{
    <MudAlert Severity="Severity.Info" Dense="true" Class="mt-2">
        @aiInsight.StaleMessage
    </MudAlert>
}
```

#### 3.6.4 Configuration

| Setting | Default | Description |
|---|---|---|
| `AiInsights:StaleInsightMonths` | 3 | After this many months with no new data, show a staleness message encouraging the user to play more golf |

---

## 4. Service Layer Implementation

### 4.1 New Files to Create

```
GolfTrackerApp.Web/
├── Models/
│   ├── AiProviderConfig.cs              # Provider configuration model (no Enabled field)
│   ├── AiProviderSettings.cs            # DB entity for admin-managed provider on/off + priority
│   ├── AiInsightResult.cs               # AI response DTO (includes StaleMessage, GeneratedAt)
│   ├── AiChatMessage.cs                 # Chat conversation message DTO (for API requests)
│   ├── AiAuditLog.cs                    # Audit log entity
│   ├── AiChatSession.cs                 # Chat session entity
│   └── AiChatSessionMessage.cs          # Chat message entity
├── Services/
│   ├── IAiProviderService.cs            # Provider abstraction interface
│   ├── IAiRoutingService.cs             # Routing/failover interface
│   ├── AiRoutingService.cs              # Routing/failover implementation
│   ├── IAiInsightService.cs             # Golf insight orchestration interface
│   ├── AiInsightService.cs              # Golf insight orchestration implementation
│   ├── IAiAuditService.cs               # Audit logging + rate limiting interface
│   ├── AiAuditService.cs                # Audit logging + rate limiting implementation
│   ├── IAiChatService.cs                # Persistent chat session interface
│   ├── AiChatService.cs                 # Persistent chat session implementation
│   ├── IAiProviderSettingsService.cs     # Admin provider settings interface
│   ├── AiProviderSettingsService.cs      # Admin provider settings implementation
│   └── AiProviders/                     # Provider implementations (new folder)
│       ├── OpenAiProviderService.cs
│       ├── AnthropicProviderService.cs
│       ├── GeminiProviderService.cs
│       ├── GrokProviderService.cs
│       ├── MistralProviderService.cs
│       ├── DeepSeekProviderService.cs
│       ├── MetaLlamaProviderService.cs
│       └── ManusProviderService.cs
├── Controllers/
│   └── InsightsController.cs            # API endpoints for mobile
├── Components/
│   └── Pages/
│       └── Admin/
│           ├── AiProviders.razor         # Admin: enable/disable providers + set priority
│           └── AiUsage.razor             # Admin: AI usage statistics dashboard
```

```
GolfTrackerApp.Mobile/
├── Services/
│   └── Api/
│       └── InsightsApiService.cs        # Mobile API client
├── Components/
│   ├── Dashboard/
│   │   └── AiInsightsWidget.razor       # Dashboard widget
│   └── Pages/
│       └── AiChatPage.razor             # Dedicated chat page
```

### 4.2 IAiProviderService Interface

```csharp
// GolfTrackerApp.Web/Services/IAiProviderService.cs

public interface IAiProviderService
{
    string ProviderName { get; }
    Task<AiInsightResult> GenerateCompletionAsync(
        string systemPrompt,
        string userPrompt,
        int maxTokens,
        double temperature,
        CancellationToken cancellationToken = default);
}
```

### 4.3 AiInsightResult Model

```csharp
// GolfTrackerApp.Web/Models/AiInsightResult.cs

public class AiInsightResult
{
    public bool Success { get; set; }
    public string Content { get; set; } = string.Empty;
    public string ProviderUsed { get; set; } = string.Empty;
    public string ModelUsed { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public string? ErrorMessage { get; set; }
    public int? ChatSessionId { get; set; }                     // Returned for chat calls so client can continue the session
}
```

### 4.4 AiChatMessage Model

```csharp
// GolfTrackerApp.Web/Models/AiChatMessage.cs

public class AiChatMessage
{
    public string Role { get; set; } = string.Empty;   // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

### 4.5 Provider Implementations

Each provider implements `IAiProviderService`. They are NOT registered individually in DI — instead, `AiRoutingService` creates/manages them based on configuration. Each provider uses `IHttpClientFactory` for HTTP calls.

#### Example: OpenAI Provider

```csharp
// GolfTrackerApp.Web/Services/AiProviders/OpenAiProviderService.cs

public class OpenAiProviderService : IAiProviderService
{
    private readonly HttpClient _httpClient;
    private readonly AiProviderConfig _config;
    private readonly ILogger<OpenAiProviderService> _logger;

    public string ProviderName => "OpenAI";

    public OpenAiProviderService(
        HttpClient httpClient,
        AiProviderConfig config,
        ILogger<OpenAiProviderService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<AiInsightResult> GenerateCompletionAsync(
        string systemPrompt, string userPrompt,
        int maxTokens, double temperature,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            model = _config.Model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            max_tokens = maxTokens,
            temperature = temperature
        };

        var request = new HttpRequestMessage(HttpMethod.Post, _config.Endpoint)
        {
            Content = JsonContent.Create(requestBody)
        };
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.ApiKey);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(_config.TimeoutSeconds));

        var response = await _httpClient.SendAsync(request, cts.Token);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cts.Token);
        using var doc = JsonDocument.Parse(json);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        var tokensUsed = doc.RootElement
            .GetProperty("usage")
            .GetProperty("total_tokens")
            .GetInt32();

        return new AiInsightResult
        {
            Success = true,
            Content = content,
            ProviderUsed = ProviderName,
            TokensUsed = tokensUsed
        };
    }
}
```

#### Anthropic Provider (Key Differences)

- Uses `x-api-key` header instead of `Authorization: Bearer`
- Uses `anthropic-version: 2023-06-01` header
- Request body format: `{ model, max_tokens, system, messages: [{ role, content }] }`
- Response path: `content[0].text`

#### Gemini Provider (Key Differences)

- API key passed as query parameter: `?key={apiKey}`
- URL includes model: `{endpoint}/{model}:generateContent`
- Request body: `{ contents: [{ parts: [{ text }] }], generationConfig: { maxOutputTokens, temperature } }`
- System instructions sent as `systemInstruction` field
- Response path: `candidates[0].content.parts[0].text`

#### Grok / Mistral / DeepSeek Providers

- All use OpenAI-compatible API format (same request/response structure as OpenAI)
- Only differ in endpoint URL, model name, and API key
- Can extend or reuse the OpenAI implementation with a different config

#### MetaLlama Provider

- Placeholder implementation — will depend on hosting choice (self-hosted, Replicate, Together AI, etc.)
- Returns `Success = false` with `ErrorMessage = "Provider not yet configured"` until implemented

#### Manus Provider

- Fully implemented async task-based provider using the Manus Open API (`https://api.manus.im`)
- Uses OpenAI Responses API compatible format: creates a task via `POST /v1/responses`, then polls every 3s until status is `completed` or `failed`
- Auth via `X-API-Key` header (not Bearer token)
- Default model: `manus-1.6-lite`, default timeout: 120s
- Sets `hide_in_task_list: true` to avoid polluting the user's Manus dashboard
- Extracts the last assistant text from the `output` array in the completed response

### 4.6 IAiRoutingService

```csharp
// GolfTrackerApp.Web/Services/IAiRoutingService.cs

public interface IAiRoutingService
{
    Task<AiInsightResult> RouteCompletionAsync(
        string systemPrompt,
        string userPrompt,
        int? maxTokens = null,
        double? temperature = null,
        CancellationToken cancellationToken = default);
}
```

### 4.7 AiRoutingService Implementation

```csharp
// GolfTrackerApp.Web/Services/AiRoutingService.cs

public class AiRoutingService : IAiRoutingService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AiRoutingService> _logger;
    private readonly ILoggerFactory _loggerFactory;

    // Circuit breaker: track providers that have failed recently
    private static readonly ConcurrentDictionary<string, DateTime> _circuitBreakerState = new();
    private static readonly TimeSpan CircuitBreakerCooldown = TimeSpan.FromMinutes(5);

    public AiRoutingService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<AiRoutingService> logger,
        ILoggerFactory loggerFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public async Task<AiInsightResult> RouteCompletionAsync(
        string systemPrompt, string userPrompt,
        int? maxTokens = null, double? temperature = null,
        CancellationToken cancellationToken = default)
    {
        var providers = _configuration
            .GetSection("AiInsights:Providers")
            .Get<List<AiProviderConfig>>()
            ?? new List<AiProviderConfig>();

        var effectiveMaxTokens = maxTokens
            ?? _configuration.GetValue<int>("AiInsights:MaxTokens");
        var effectiveTemperature = temperature
            ?? _configuration.GetValue<double>("AiInsights:Temperature");

        var enabledProviders = providers
            .Where(p => p.Enabled && !string.IsNullOrEmpty(p.ApiKey))
            .OrderBy(p => p.Priority)
            .ToList();

        if (!enabledProviders.Any())
        {
            return new AiInsightResult
            {
                Success = false,
                ErrorMessage = "No AI providers are configured and enabled."
            };
        }

        foreach (var config in enabledProviders)
        {
            // Check circuit breaker
            if (_circuitBreakerState.TryGetValue(config.Name, out var failedAt)
                && DateTime.UtcNow - failedAt < CircuitBreakerCooldown)
            {
                _logger.LogDebug("Skipping provider {Provider} — circuit breaker active", config.Name);
                continue;
            }

            try
            {
                var provider = CreateProvider(config);
                var result = await provider.GenerateCompletionAsync(
                    systemPrompt, userPrompt,
                    effectiveMaxTokens, effectiveTemperature,
                    cancellationToken);

                if (result.Success)
                {
                    // Clear circuit breaker on success
                    _circuitBreakerState.TryRemove(config.Name, out _);
                    _logger.LogInformation(
                        "AI insight generated by {Provider} ({Tokens} tokens)",
                        config.Name, result.TokensUsed);
                    return result;
                }

                _logger.LogWarning(
                    "Provider {Provider} returned unsuccessful: {Error}",
                    config.Name, result.ErrorMessage);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw; // Don't catch user-requested cancellation
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Provider {Provider} failed, trying next", config.Name);
                _circuitBreakerState[config.Name] = DateTime.UtcNow;
            }
        }

        return new AiInsightResult
        {
            Success = false,
            ErrorMessage = "All AI providers failed. Please try again later."
        };
    }

    private IAiProviderService CreateProvider(AiProviderConfig config)
    {
        var httpClient = _httpClientFactory.CreateClient($"AiProvider_{config.Name}");
        return config.Name switch
        {
            "OpenAI" => new OpenAiProviderService(httpClient, config,
                _loggerFactory.CreateLogger<OpenAiProviderService>()),
            "Anthropic" => new AnthropicProviderService(httpClient, config,
                _loggerFactory.CreateLogger<AnthropicProviderService>()),
            "Gemini" => new GeminiProviderService(httpClient, config,
                _loggerFactory.CreateLogger<GeminiProviderService>()),
            "Grok" => new GrokProviderService(httpClient, config,
                _loggerFactory.CreateLogger<GrokProviderService>()),
            "Mistral" => new MistralProviderService(httpClient, config,
                _loggerFactory.CreateLogger<MistralProviderService>()),
            "DeepSeek" => new DeepSeekProviderService(httpClient, config,
                _loggerFactory.CreateLogger<DeepSeekProviderService>()),
            "MetaLlama" => new MetaLlamaProviderService(httpClient, config,
                _loggerFactory.CreateLogger<MetaLlamaProviderService>()),
            "Manus" => new ManusProviderService(httpClient, config,
                _loggerFactory.CreateLogger<ManusProviderService>()),
            _ => throw new ArgumentException($"Unknown AI provider: {config.Name}")
        };
    }
}
```

### 4.8 IAiInsightService

This is the **golf-specific** service that constructs prompts from real user data and returns insight text. It sits between the UI layer and the routing layer.

```csharp
// GolfTrackerApp.Web/Services/IAiInsightService.cs

public interface IAiInsightService
{
    Task<AiInsightResult> GetDashboardInsightsAsync(string userId,
        CancellationToken cancellationToken = default);

    Task<AiInsightResult> GetPlayerReportInsightsAsync(int playerId,
        int? courseId = null, int? holesPlayed = null,
        CancellationToken cancellationToken = default);

    Task<AiInsightResult> GetClubInsightsAsync(string userId, int clubId,
        CancellationToken cancellationToken = default);

    Task<AiInsightResult> GetCourseInsightsAsync(string userId, int courseId,
        CancellationToken cancellationToken = default);

    Task<AiInsightResult> ChatAsync(string userId, string userMessage,
        int? sessionId = null,
        CancellationToken cancellationToken = default);
}
```

### 4.9 AiInsightService Implementation

```csharp
// GolfTrackerApp.Web/Services/AiInsightService.cs

public class AiInsightService : IAiInsightService
{
    private readonly IAiRoutingService _aiRouting;
    private readonly IReportService _reportService;
    private readonly IRoundService _roundService;
    private readonly IPlayerService _playerService;
    private readonly IGolfCourseService _courseService;
    private readonly IGolfClubService _clubService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiInsightService> _logger;

    private readonly IAiAuditService _auditService;
    private readonly IAiChatService _chatService;

    // In-memory cache (consider IMemoryCache for production)
    private static readonly ConcurrentDictionary<string, (AiInsightResult Result, DateTime CachedAt)>
        _cache = new();

    public AiInsightService(
        IAiRoutingService aiRouting,
        IReportService reportService,
        IRoundService roundService,
        IPlayerService playerService,
        IGolfCourseService courseService,
        IGolfClubService clubService,
        IAiAuditService auditService,
        IAiChatService chatService,
        IConfiguration configuration,
        ILogger<AiInsightService> logger)
    {
        _aiRouting = aiRouting;
        _reportService = reportService;
        _roundService = roundService;
        _playerService = playerService;
        _auditService = auditService;
        _chatService = chatService;
        _courseService = courseService;
        _clubService = clubService;
        _configuration = configuration;
        _logger = logger;
    }
```

#### 4.9.1 Dashboard Insights

Collects `DashboardStats`, `ScoringDistribution`, `PerformanceByPar`, recent `CourseHistoryItem` list, and `PlayingPartnerSummary` list — the same data already loaded on the dashboard — and sends a structured prompt.

```csharp
    public async Task<AiInsightResult> GetDashboardInsightsAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled()) return DisabledResult();

        var cacheKey = $"dashboard_{userId}";
        if (TryGetCached(cacheKey, out var cached)) return cached;

        var player = await _playerService.GetPlayerByApplicationUserIdAsync(userId);
        if (player == null)
            return new AiInsightResult { Success = false, ErrorMessage = "Player not found." };

        // Gather data using existing services (same calls the dashboard page makes)
        var statsTask = _reportService.GetDashboardStatsAsync(userId);
        var scoringTask = _reportService.GetScoringDistributionAsync(player.PlayerId);
        var parTask = _reportService.GetPerformanceByParAsync(player.PlayerId);
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

        var result = await _aiRouting.RouteCompletionAsync(
            systemPrompt, userPrompt, cancellationToken: cancellationToken);

        if (result.Success) Cache(cacheKey, result);
        return result;
    }
```

#### 4.9.2 Prompt Construction

The system prompt stays constant. The user prompt varies per insight type and includes serialised data.

```csharp
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
```

#### 4.9.3 Player Report Insights

```csharp
    public async Task<AiInsightResult> GetPlayerReportInsightsAsync(
        int playerId, int? courseId = null, int? holesPlayed = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled()) return DisabledResult();

        var cacheKey = $"report_{playerId}_{courseId}_{holesPlayed}";
        if (TryGetCached(cacheKey, out var cached)) return cached;

        var reportTask = _reportService.GetPlayerReportViewModelAsync(
            playerId, courseId, holesPlayed, null, null, null);
        var scoringTask = _reportService.GetScoringDistributionAsync(
            playerId, courseId, holesPlayed);
        var parTask = _reportService.GetPerformanceByParAsync(
            playerId, courseId, holesPlayed);

        await Task.WhenAll(reportTask, scoringTask, parTask);

        var report = await reportTask;
        var scoring = await scoringTask;
        var par = await parTask;

        if (report?.PerformanceData == null || !report.PerformanceData.Any())
            return new AiInsightResult { Success = true, Content = "Not enough data for analysis." };

        var systemPrompt = BuildSystemPrompt();
        var userPrompt = BuildPlayerReportPrompt(report, scoring, par);

        var result = await _aiRouting.RouteCompletionAsync(
            systemPrompt, userPrompt, cancellationToken: cancellationToken);

        if (result.Success) Cache(cacheKey, result);
        return result;
    }
```

#### 4.9.4 Club and Course Insights

Similar pattern: gather data via `IReportService` + `IGolfClubService`/`IGolfCourseService`, build prompt, call routing service. The club prompt includes aggregate stats across all courses at that club. The course prompt includes hole-by-hole averages if available.

#### 4.9.5 Chat Method

The chat method takes a free-form user message and conversation history. It gathers the user's overall stats as context and sends the conversation to the AI provider.

```csharp
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

        // Create or resume a persistent chat session
        AiChatSession? session;
        if (sessionId.HasValue)
            session = await _chatService.GetSessionAsync(sessionId.Value, userId);
        else
            session = await _chatService.CreateSessionAsync(userId, userMessage);

        if (session == null)
            return new AiInsightResult { Success = false, ErrorMessage = "Chat session not found." };

        // Persist the user message
        await _chatService.AddMessageAsync(session.AiChatSessionId, "user", userMessage);

        var stats = await _reportService.GetDashboardStatsAsync(userId);

        var systemPrompt = """
            You are a friendly golf coach assistant. The user can ask you questions about
            their golf performance. You have access to their stats provided below.
            Answer conversationally. If you don't have enough data to answer, say so.
            Keep responses concise (under 200 words).
            """ + $"\n\nPlayer context:\n{BuildBriefPlayerContext(player, stats)}";

        // Build prompt from persistent history
        var history = await _chatService.GetMessagesAsync(session.AiChatSessionId, 20);
        var fullPrompt = new StringBuilder();
        foreach (var msg in history.TakeLast(10))
            fullPrompt.AppendLine($"[{msg.Role}]: {msg.Content}");

        var stopwatch = Stopwatch.StartNew();
        var result = await _aiRouting.RouteCompletionAsync(
            systemPrompt, fullPrompt.ToString(),
            maxTokens: 300, cancellationToken: cancellationToken);
        stopwatch.Stop();

        // Persist assistant response
        if (result.Success)
            await _chatService.AddMessageAsync(session.AiChatSessionId, "assistant", result.Content);

        // Audit log
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

        // Return result with session ID so client can continue the conversation
        result.ChatSessionId = session.AiChatSessionId;
        return result;
    }
```

#### 4.9.6 Caching — Data-Watermark Strategy

Instead of simple time-based cache expiry, insights are cached against a **data watermark** — the timestamp of the user's most recent round. If no new rounds have been played since the last insight was generated, the cached insight is returned without calling the AI provider.

See **Section 3.6** for the full data freshness design.

```csharp
    private bool IsEnabled() =>
        _configuration.GetValue<bool>("AiInsights:Enabled");

    private static AiInsightResult DisabledResult() =>
        new() { Success = false, ErrorMessage = "AI Insights are not enabled." };

    private static readonly ConcurrentDictionary<string, CachedInsight> _cache = new();

    private record CachedInsight(
        AiInsightResult Result,
        DateTime DataWatermark,
        DateTime GeneratedAt);

    private bool TryGetCached(string key, DateTime? lastDataChange, out AiInsightResult result)
    {
        if (_cache.TryGetValue(key, out var cached)
            && lastDataChange.HasValue
            && cached.DataWatermark >= lastDataChange.Value)
        {
            result = cached.Result;
            
            // Check staleness
            var staleMonths = _configuration.GetValue<int>("AiInsights:StaleInsightMonths");
            var age = DateTime.UtcNow - cached.GeneratedAt;
            if (staleMonths > 0 && age.TotalDays > staleMonths * 30)
            {
                result.StaleMessage = $"This insight is {(int)(age.TotalDays / 30)} months old \u2014 go play some more golf!";
            }
            return true;
        }
        result = default!;
        return false;
    }

    private static void Cache(string key, AiInsightResult result, DateTime dataWatermark) =>
        _cache[key] = new CachedInsight(result, dataWatermark, DateTime.UtcNow);
}
```

---

## 5. API Controller

### 5.1 InsightsController

Follows the existing controller pattern: extends `BaseApiController`, uses `[Route("api/[controller]")]`, try/catch with `StatusCode(500)` error handling.

```csharp
// GolfTrackerApp.Web/Controllers/InsightsController.cs

[Route("api/[controller]")]
public class InsightsController : BaseApiController
{
    private readonly IAiInsightService _insightService;
    private readonly IAiChatService _chatService;
    private readonly ILogger<InsightsController> _logger;

    public InsightsController(
        IAiInsightService insightService,
        IAiChatService chatService,
        ILogger<InsightsController> logger)
    {
        _insightService = insightService;
        _chatService = chatService;
        _logger = logger;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<AiInsightResult>> GetDashboardInsights(
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _insightService.GetDashboardInsightsAsync(userId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard insights");
            return StatusCode(500, "Failed to generate insights.");
        }
    }

    [HttpGet("player-report/{playerId:int}")]
    public async Task<ActionResult<AiInsightResult>> GetPlayerReportInsights(
        int playerId,
        [FromQuery] int? courseId = null,
        [FromQuery] int? holesPlayed = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _insightService.GetPlayerReportInsightsAsync(
                playerId, courseId, holesPlayed, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting player report insights for {PlayerId}", playerId);
            return StatusCode(500, "Failed to generate insights.");
        }
    }

    [HttpGet("club/{clubId:int}")]
    public async Task<ActionResult<AiInsightResult>> GetClubInsights(
        int clubId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _insightService.GetClubInsightsAsync(userId, clubId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting club insights for {ClubId}", clubId);
            return StatusCode(500, "Failed to generate insights.");
        }
    }

    [HttpGet("course/{courseId:int}")]
    public async Task<ActionResult<AiInsightResult>> GetCourseInsights(
        int courseId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _insightService.GetCourseInsightsAsync(
                userId, courseId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting course insights for {CourseId}", courseId);
            return StatusCode(500, "Failed to generate insights.");
        }
    }

    [HttpPost("chat")]
    public async Task<ActionResult<AiInsightResult>> Chat(
        [FromBody] AiChatRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _insightService.ChatAsync(
                userId, request.Message, request.SessionId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing AI chat");
            return StatusCode(500, "Failed to process chat message.");
        }
    }

    [HttpGet("sessions")]
    public async Task<ActionResult<List<AiChatSession>>> GetChatSessions(
        [FromQuery] int limit = 20)
    {
        try
        {
            var userId = GetCurrentUserId();
            var sessions = await _chatService.GetSessionsAsync(userId, limit);
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat sessions");
            return StatusCode(500, "Failed to load chat sessions.");
        }
    }

    [HttpGet("sessions/{sessionId:int}")]
    public async Task<ActionResult<AiChatSession>> GetChatSession(int sessionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var session = await _chatService.GetSessionAsync(sessionId, userId);
            if (session == null) return NotFound();
            return Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat session {SessionId}", sessionId);
            return StatusCode(500, "Failed to load chat session.");
        }
    }
}
```

### 5.2 Chat Request DTO

```csharp
// Add to GolfTrackerApp.Web/Models/AiChatMessage.cs

public class AiChatRequest
{
    public string Message { get; set; } = string.Empty;
    public int? SessionId { get; set; }                         // null = start new session
}
```

---

## 6. DI Registration

### 6.1 Web — Program.cs

Add after the existing service registrations:

```csharp
// AI Insights services
builder.Services.AddScoped<IAiInsightService, AiInsightService>();
builder.Services.AddScoped<IAiRoutingService, AiRoutingService>();
builder.Services.AddScoped<IAiAuditService, AiAuditService>();
builder.Services.AddScoped<IAiChatService, AiChatService>();
builder.Services.AddScoped<IAiProviderSettingsService, AiProviderSettingsService>();
builder.Services.AddHttpClient("AiProvider_OpenAI");
builder.Services.AddHttpClient("AiProvider_Anthropic");
builder.Services.AddHttpClient("AiProvider_Gemini");
builder.Services.AddHttpClient("AiProvider_Grok");
builder.Services.AddHttpClient("AiProvider_Mistral");
builder.Services.AddHttpClient("AiProvider_DeepSeek");
builder.Services.AddHttpClient("AiProvider_MetaLlama");
builder.Services.AddHttpClient("AiProvider_Manus");
```

### 6.2 Mobile — MauiProgram.cs

Add after the existing API service registrations:

```csharp
builder.Services.AddHttpClient<IInsightsApiService, InsightsApiService>(httpClientBuilder)
    .ConfigurePrimaryHttpMessageHandler(certHandler);
```

---

## 7. Web UI Integration

### 7.1 Dashboard AI Widget

Add a new row to [Home.razor](../GolfTrackerApp.Web/Components/Pages/Home.razor) after the existing "Round Insights" row. Only rendered when AI is enabled and the user has rounds.

```razor
@* AI Insights Widget — add after the Scoring Distribution + Round Insights row *@
@if (aiInsightEnabled && dashboardStats?.TotalRounds > 0)
{
    <MudItem xs="12">
        <MudCard Class="dashboard-card" Elevation="2">
            <MudCardHeader>
                <CardHeaderContent>
                    <MudText Typo="Typo.h6">
                        <MudIcon Icon="@Icons.Material.Filled.AutoAwesome" Class="mr-2" />
                        AI Insights
                    </MudText>
                </CardHeaderContent>
                <CardHeaderActions>
                    <MudIconButton Icon="@Icons.Material.Filled.Refresh"
                                   OnClick="RefreshAiInsights"
                                   Disabled="@aiLoading"
                                   Size="Size.Small" />
                </CardHeaderActions>
            </MudCardHeader>
            <MudCardContent>
                @if (aiLoading)
                {
                    <MudProgressLinear Indeterminate="true" Color="Color.Primary" />
                    <MudText Typo="Typo.caption" Class="mt-2">Analysing your game...</MudText>
                }
                else if (aiInsight?.Success == true)
                {
                    <MudText Typo="Typo.body2" Style="white-space: pre-line;">
                        @aiInsight.Content
                    </MudText>
                    <MudText Typo="Typo.caption" Color="Color.Tertiary" Class="mt-2">
                        Powered by @aiInsight.ProviderUsed
                    </MudText>
                }
                else if (aiInsight != null)
                {
                    <MudText Typo="Typo.body2" Color="Color.Warning">
                        @(aiInsight.ErrorMessage ?? "Unable to generate insights right now.")
                    </MudText>
                }
            </MudCardContent>
        </MudCard>
    </MudItem>
}
```

**Code-behind additions** to [Home.razor](../GolfTrackerApp.Web/Components/Pages/Home.razor):

```csharp
@inject IAiInsightService AiInsightService
@inject IConfiguration Configuration

// Fields
private bool aiInsightEnabled;
private bool aiLoading;
private AiInsightResult? aiInsight;

// In OnInitializedAsync, after existing data loads:
aiInsightEnabled = Configuration.GetValue<bool>("AiInsights:Enabled");
if (aiInsightEnabled && dashboardStats?.TotalRounds > 0)
{
    _ = LoadAiInsights(); // Fire-and-forget, don't block dashboard render
}

private async Task LoadAiInsights()
{
    aiLoading = true;
    StateHasChanged();
    try
    {
        aiInsight = await AiInsightService.GetDashboardInsightsAsync(currentUserId);
    }
    catch (Exception ex)
    {
        aiInsight = new AiInsightResult
        {
            Success = false,
            ErrorMessage = "Could not load AI insights."
        };
    }
    finally
    {
        aiLoading = false;
        StateHasChanged();
    }
}

private async Task RefreshAiInsights()
{
    // Clear cache for this user's dashboard and re-fetch
    await LoadAiInsights();
}
```

### 7.2 Player Report AI Panel

Add to [PlayerReport.razor](../GolfTrackerApp.Web/Components/Pages/Players/PlayerReport.razor) below the summary strip, inside a `MudExpansionPanel`:

```razor
@if (aiInsightEnabled)
{
    <MudExpansionPanels Class="mt-4">
        <MudExpansionPanel Text="AI Analysis" Icon="@Icons.Material.Filled.AutoAwesome"
                           IsInitiallyExpanded="false"
                           OnClick="LoadPlayerReportInsights">
            @if (aiLoading)
            {
                <MudProgressLinear Indeterminate="true" />
            }
            else if (aiInsight?.Success == true)
            {
                <MudText Typo="Typo.body2" Style="white-space: pre-line;">
                    @aiInsight.Content
                </MudText>
            }
            else if (aiInsight != null)
            {
                <MudAlert Severity="Severity.Warning">@aiInsight.ErrorMessage</MudAlert>
            }
        </MudExpansionPanel>
    </MudExpansionPanels>
}
```

The insight is loaded **on-demand** when the user expands the panel (not on page load) to avoid unnecessary AI calls.

### 7.3 Club and Course Detail Pages

Same pattern as above — add an expandable panel in [GolfClubDetails.razor](../GolfTrackerApp.Web/Components/Pages/GolfClubs/GolfClubDetails.razor) and [GolfCourseDetails.razor](../GolfTrackerApp.Web/Components/Pages/GolfCourses/GolfCourseDetails.razor). Only shown when the user is authenticated and has played rounds at that club/course.

---

## 8. Mobile Integration

### 8.1 InsightsApiService

Follows the existing mobile API service pattern (same as `DashboardApiService`):

```csharp
// GolfTrackerApp.Mobile/Services/Api/InsightsApiService.cs

public interface IInsightsApiService
{
    Task<AiInsightResult?> GetDashboardInsightsAsync();
    Task<AiInsightResult?> GetPlayerReportInsightsAsync(int playerId,
        int? courseId = null, int? holesPlayed = null);
    Task<AiInsightResult?> GetClubInsightsAsync(int clubId);
    Task<AiInsightResult?> GetCourseInsightsAsync(int courseId);
    Task<AiInsightResult?> ChatAsync(string message, int? sessionId = null);
    Task<List<AiChatSessionSummary>?> GetChatSessionsAsync();
    Task<AiChatSessionDetail?> GetChatSessionAsync(int sessionId);
}

public class InsightsApiService : IInsightsApiService
{
    private readonly HttpClient _httpClient;
    private readonly AuthenticationStateService _authService;
    private readonly ILogger<InsightsApiService> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public InsightsApiService(
        HttpClient httpClient,
        AuthenticationStateService authService,
        ILogger<InsightsApiService> logger)
    {
        _httpClient = httpClient;
        _authService = authService;
        _logger = logger;
    }

    private void EnsureAuthorizationHeader()
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authService.Token);
    }

    public async Task<AiInsightResult?> GetDashboardInsightsAsync()
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.GetAsync("api/insights/dashboard");
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) return null;
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AiInsightResult>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dashboard insights");
            return null;
        }
    }

    public async Task<AiInsightResult?> GetPlayerReportInsightsAsync(
        int playerId, int? courseId = null, int? holesPlayed = null)
    {
        try
        {
            EnsureAuthorizationHeader();
            var url = $"api/insights/player-report/{playerId}";
            var queryParams = new List<string>();
            if (courseId.HasValue) queryParams.Add($"courseId={courseId}");
            if (holesPlayed.HasValue) queryParams.Add($"holesPlayed={holesPlayed}");
            if (queryParams.Any()) url += "?" + string.Join("&", queryParams);

            var response = await _httpClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) return null;
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AiInsightResult>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching player report insights");
            return null;
        }
    }

    public async Task<AiInsightResult?> ChatAsync(
        string message, int? sessionId = null)
    {
        try
        {
            EnsureAuthorizationHeader();
            var request = new { Message = message, SessionId = sessionId };
            var content = JsonContent.Create(request, options: _jsonOptions);
            var response = await _httpClient.PostAsync("api/insights/chat", content);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) return null;
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AiInsightResult>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending chat message");
            return null;
        }
    }

    // GetClubInsightsAsync and GetCourseInsightsAsync follow the same pattern
    // with urls: api/insights/club/{clubId} and api/insights/course/{courseId}

    // GetChatSessionsAsync and GetChatSessionAsync follow the same GET pattern
    // with urls: api/insights/sessions and api/insights/sessions/{sessionId}
}
```

### 8.2 Mobile AiInsightResult DTO

Create in `GolfTrackerApp.Mobile/Models/` (following the existing pattern of duplicate mobile DTOs):

```csharp
// GolfTrackerApp.Mobile/Models/AiInsightResult.cs

public class AiInsightResult
{
    public bool Success { get; set; }
    public string Content { get; set; } = string.Empty;
    public string ProviderUsed { get; set; } = string.Empty;
    public string ModelUsed { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public string? ErrorMessage { get; set; }
    public int? ChatSessionId { get; set; }
}

public class AiChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
```

### 8.3 Mobile Dashboard Widget

```razor
@* GolfTrackerApp.Mobile/Components/Dashboard/AiInsightsWidget.razor *@

<MudCard Class="ai-insights-card" Elevation="1">
    <MudCardHeader>
        <CardHeaderContent>
            <MudText Typo="Typo.subtitle1">
                <MudIcon Icon="@Icons.Material.Filled.AutoAwesome"
                         Size="Size.Small" Class="mr-1" />
                AI Insights
            </MudText>
        </CardHeaderContent>
        <CardHeaderActions>
            <MudIconButton Icon="@Icons.Material.Filled.Refresh"
                           OnClick="OnRefresh"
                           Disabled="@Loading"
                           Size="Size.Small" />
        </CardHeaderActions>
    </MudCardHeader>
    <MudCardContent>
        @if (Loading)
        {
            <MudProgressLinear Indeterminate="true" Color="Color.Primary" />
            <MudText Typo="Typo.caption">Analysing your game...</MudText>
        }
        else if (Insight?.Success == true)
        {
            <MudText Typo="Typo.body2" Style="white-space: pre-line; font-size: 0.85rem;">
                @Insight.Content
            </MudText>
            <MudText Typo="Typo.caption" Color="Color.Tertiary" Class="mt-1"
                     Style="font-size: 0.7rem;">
                Powered by @Insight.ProviderUsed
            </MudText>
        }
        else if (Insight != null)
        {
            <MudText Typo="Typo.caption" Color="Color.Warning">
                Could not load insights.
            </MudText>
        }
    </MudCardContent>
</MudCard>

@code {
    [Parameter, EditorRequired] public AiInsightResult? Insight { get; set; }
    [Parameter] public bool Loading { get; set; }
    [Parameter] public EventCallback OnRefresh { get; set; }
}
```

### 8.4 Mobile AI Chat Page

Add [AiChatPage.razor](../GolfTrackerApp.Mobile/Components/Pages/AiChatPage.razor) and register it in [App.razor](../GolfTrackerApp.Mobile/Components/App.razor) with the existing switch-based routing pattern:

```csharp
// In App.razor switch statement, add:
case "ai-chat":
    <AiChatPage OnNavigate="NavigateTo" />
    break;
```

Add a navigation icon to the bottom nav bar (or a menu entry) for the AI chat page.

The chat page structure:

```razor
@* GolfTrackerApp.Mobile/Components/Pages/AiChatPage.razor *@

<div class="chat-container">
    <div class="chat-messages" @ref="messagesContainer">
        @foreach (var msg in messages)
        {
            <div class="chat-bubble @(msg.Role == "user" ? "user-bubble" : "assistant-bubble")">
                <MudText Typo="Typo.body2">@msg.Content</MudText>
                <MudText Typo="Typo.caption" Color="Color.Tertiary">
                    @msg.Timestamp.ToString("HH:mm")
                </MudText>
            </div>
        }
        @if (isTyping)
        {
            <div class="chat-bubble assistant-bubble">
                <MudProgressCircular Size="Size.Small" Indeterminate="true" />
            </div>
        }
    </div>

    <div class="chat-input-bar">
        <MudTextField @bind-Value="currentMessage"
                      Placeholder="Ask about your golf game..."
                      Variant="Variant.Outlined"
                      Adornment="Adornment.End"
                      AdornmentIcon="@Icons.Material.Filled.Send"
                      OnAdornmentClick="SendMessage"
                      OnKeyDown="HandleKeyDown"
                      Disabled="@isTyping"
                      FullWidth="true" />
    </div>
</div>

@code {
    [Parameter] public EventCallback<string> OnNavigate { get; set; }

    @inject IInsightsApiService InsightsService

    private List<AiChatMessage> messages = new();
    private string currentMessage = string.Empty;
    private bool isTyping;
    private int? currentSessionId;
    private ElementReference messagesContainer;

    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(currentMessage) || isTyping) return;

        var userMsg = new AiChatMessage
        {
            Role = "user",
            Content = currentMessage.Trim(),
            Timestamp = DateTime.Now
        };
        messages.Add(userMsg);
        var messageText = currentMessage.Trim();
        currentMessage = string.Empty;
        isTyping = true;
        StateHasChanged();

        var result = await InsightsService.ChatAsync(messageText, currentSessionId);

        if (result?.Success == true)
        {
            messages.Add(new AiChatMessage
            {
                Role = "assistant",
                Content = result.Content,
                Timestamp = DateTime.Now
            });
            // Track session ID for follow-up messages
            if (result.ChatSessionId.HasValue)
                currentSessionId = result.ChatSessionId;
        }
        else
        {
            messages.Add(new AiChatMessage
            {
                Role = "assistant",
                Content = "Sorry, I couldn't process that right now. Please try again.",
                Timestamp = DateTime.Now
            });
        }

        isTyping = false;
        StateHasChanged();
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey)
            await SendMessage();
    }
}
```

---

## 9. CSS Additions

### 9.1 Web

Create `wwwroot/css/components/ai-insights.css` and add it to the CSS imports. Use the same design language as the existing dashboard:

- Dark gradient card header consistent with `dashboard-card`
- `AutoAwesome` icon accent colour (e.g., gold/amber `#FFB800`)
- Loading state matches existing `MudProgressLinear` usage
- Chat bubbles: user = primary colour, assistant = surface variant

### 9.2 Mobile

Add styles inline or in the component's scoped CSS, following the existing mobile widget pattern (see `HeroStatsWidget.razor` for reference). Keep font sizes at mobile scale (0.7–0.85rem for captions).

---

## 10. Implementation Phases

### Phase 1 — Core Infrastructure (Foundation)

**Goal**: Get one AI provider working end-to-end with dashboard insights.

| Step | Task | Files | Status |
|------|------|-------|--------|
| 1.1 | Create `AiProviderConfig`, `AiInsightResult`, `AiChatMessage` models | `Models/AiProviderConfig.cs`, `Models/AiInsightResult.cs`, `Models/AiChatMessage.cs` | ✅ Done |
| 1.2 | Create `AiAuditLog`, `AiChatSession`, `AiChatSessionMessage` entity models | `Models/AiAuditLog.cs`, `Models/AiChatSession.cs`, `Models/AiChatSessionMessage.cs` | ✅ Done |
| 1.3 | Add DbSets + OnModelCreating config to `ApplicationDbContext` | `Data/ApplicationDbContext.cs` | ✅ Done |
| 1.4 | Create EF Core migration + update `EnsureNewTablesExistAsync()` | Migration + `Program.cs` | ✅ Done |
| 1.5 | Create `IAiProviderService` interface | `Services/IAiProviderService.cs` | ✅ Done |
| 1.6 | Implement `OpenAiProviderService` | `Services/AiProviders/OpenAiProviderService.cs` | ✅ Done |
| 1.7 | Create `IAiRoutingService` + `AiRoutingService` (initially single provider) | `Services/IAiRoutingService.cs`, `Services/AiRoutingService.cs` | ✅ Done |
| 1.8 | Create `IAiAuditService` + `AiAuditService` | `Services/IAiAuditService.cs`, `Services/AiAuditService.cs` | ✅ Done |
| 1.9 | Create `IAiChatService` + `AiChatService` | `Services/IAiChatService.cs`, `Services/AiChatService.cs` | ✅ Done |
| 1.10 | Create `IAiInsightService` + `AiInsightService` (dashboard method only) | `Services/IAiInsightService.cs`, `Services/AiInsightService.cs` | ✅ Done |
| 1.11 | Add AI config section to `appsettings.json` + `appsettings.Development.json` | Config files | ✅ Done |
| 1.12 | Register all services in `Program.cs` | `Program.cs` | ✅ Done |
| 1.13 | Add AI insight widget to web `Home.razor` | `Components/Pages/Home.razor` | ✅ Done |
| 1.14 | Test end-to-end: dashboard loads → AI widget appears → insight generated + audit row written | Manual testing | ✅ Done |

### Phase 2 — Provider Settings + Admin + Data Freshness

**Goal**: Move provider enabled/disabled to database, add admin UI, implement data-watermark caching.

| Step | Task | Files | Status |
|------|------|-------|--------|
| 2.1 | Implement `AnthropicProviderService` | `Services/AiProviders/AnthropicProviderService.cs` | ✅ Done |
| 2.2 | Implement `GeminiProviderService` | `Services/AiProviders/GeminiProviderService.cs` | ✅ Done |
| 2.3 | Implement `GrokProviderService` (OpenAI-compatible) | `Services/AiProviders/GrokProviderService.cs` | ✅ Done |
| 2.4 | Implement `MistralProviderService` (OpenAI-compatible) | `Services/AiProviders/MistralProviderService.cs` | ✅ Done |
| 2.5 | Implement `DeepSeekProviderService` (OpenAI-compatible) | `Services/AiProviders/DeepSeekProviderService.cs` | ✅ Done |
| 2.6 | Implement MetaLlama (placeholder) + Manus (full async task-based provider) | `Services/AiProviders/MetaLlama*.cs`, `Manus*.cs` | ✅ Done |
| 2.7 | Test failover: disable primary → verify secondary picks up | Manual testing | ✅ Done |
| 2.8 | Test circuit breaker: verify failed provider is skipped temporarily | Manual testing | ✅ Done |
| 2.9 | Create `AiProviderSettings` entity model | `Models/AiProviderSettings.cs` | ✅ Done |
| 2.10 | Add `AiProviderSettings` DbSet + OnModelCreating config | `Data/ApplicationDbContext.cs` | ✅ Done |
| 2.11 | Create migration + update `EnsureNewTablesExistAsync()` for `AiProviderSettings` | Migration + `Program.cs` | ✅ Done |
| 2.12 | Create `IAiProviderSettingsService` + `AiProviderSettingsService` | `Services/IAiProviderSettingsService.cs`, `Services/AiProviderSettingsService.cs` | ✅ Done |
| 2.13 | Add startup seeding of `AiProviderSettings` from `appsettings.json` | `Program.cs` | ✅ Done |
| 2.14 | Refactor `AiRoutingService` to merge DB settings (enabled/priority) with config (model/endpoint/key) | `Services/AiRoutingService.cs` | ✅ Done |
| 2.15 | Remove `Enabled` from `appsettings.json` provider entries + `AiProviderConfig` model | `appsettings.json`, `Models/AiProviderConfig.cs` | ✅ Done |
| 2.16 | Create admin AI Providers page (`/admin/ai-providers`) | `Components/Pages/Admin/AiProviders.razor` | ✅ Done |
| 2.17 | Create admin AI Usage dashboard (`/admin/ai-usage`) | `Components/Pages/Admin/AiUsage.razor` | ✅ Done |
| 2.18 | Add admin nav links for AI pages | `Components/Layout/NavMenu.razor` | ✅ Done |
| 2.19 | Implement data-watermark caching in `AiInsightService` (replace time-based cache) | `Services/AiInsightService.cs` | ✅ Done |
| 2.20 | Add `StaleMessage` + `GeneratedAt` properties to `AiInsightResult` | `Models/AiInsightResult.cs` | ✅ Done |
| 2.21 | Add `StaleInsightMonths` config + staleness message rendering in `Home.razor` | `appsettings.json`, `Components/Pages/Home.razor` | ✅ Done |
| 2.22 | Test admin provider toggle → verify routing respects DB state | Manual testing | ✅ Done |
| 2.23 | Test data freshness: add round → insight refreshes; no round → cached insight reused | Manual testing | ✅ Done |

### Phase 3 — Insights Controller + Mobile API

**Goal**: Mobile app can request AI insights via API.

| Step | Task | Files | Status |
|------|------|-------|--------|
| 3.1 | Create `InsightsController` | `Controllers/InsightsController.cs` | ✅ Done |
| 3.2 | Create `IInsightsApiService` + `InsightsApiService` (mobile) | `Mobile/Services/Api/InsightsApiService.cs` | ✅ Done |
| 3.3 | Create mobile `AiInsightResult` + `AiChatMessage` DTOs | `Mobile/Models/AiInsightResult.cs` | ✅ Done |
| 3.4 | Register mobile service in `MauiProgram.cs` | `MauiProgram.cs` | ✅ Done |
| 3.5 | Create `AiInsightsWidget.razor` (mobile dashboard widget) | `Mobile/Components/Dashboard/AiInsightsWidget.razor` | ✅ Done |
| 3.6 | Integrate widget into mobile `Home.razor` | `Mobile/Components/Pages/Home.razor` | ✅ Done |
| 3.7 | Test mobile dashboard with AI insights | Manual testing | ⬜ Pending |

### Phase 4 — Player Report + Club/Course Insights

**Goal**: AI analysis on report and detail pages.

| Step | Task | Files | Status |
|------|------|-------|--------|
| 4.1 | Implement `GetPlayerReportInsightsAsync` in `AiInsightService` | `Services/AiInsightService.cs` | ✅ Done |
| 4.2 | Implement `GetClubInsightsAsync` in `AiInsightService` | `Services/AiInsightService.cs` | ✅ Done |
| 4.3 | Implement `GetCourseInsightsAsync` in `AiInsightService` | `Services/AiInsightService.cs` | ✅ Done |
| 4.4 | Add AI panel to web `PlayerReport.razor` | `Components/Pages/Players/PlayerReport.razor` | ✅ Done |
| 4.5 | Add AI panel to web `GolfClubDetails.razor` | `Components/Pages/GolfClubs/GolfClubDetails.razor` | ✅ Done |
| 4.6 | Add AI panel to web `GolfCourseDetails.razor` | `Components/Pages/GolfCourses/GolfCourseDetails.razor` | ✅ Done |
| 4.7 | Add corresponding API endpoints to `InsightsController` | `Controllers/InsightsController.cs` | ✅ Done |
| 4.8 | Integrate into mobile report/detail pages | Mobile pages | ✅ Done |

### Phase 5 — AI Chat (Persistent Sessions)

**Goal**: Free-form conversational AI with persistent session history.

| Step | Task | Files | Status |
|------|------|-------|--------|
| 5.1 | Implement `ChatAsync` in `AiInsightService` (with session persistence + audit) | `Services/AiInsightService.cs` | ⬜ Pending |
| 5.2 | Add chat endpoints to `InsightsController` (`POST chat`, `GET sessions`, `GET sessions/{id}`) | `Controllers/InsightsController.cs` | ⬜ Pending |
| 5.3 | Create web AI Chat page with session list sidebar | `Components/Pages/AiChat.razor` | ⬜ Pending |
| 5.4 | Create mobile `AiChatPage.razor` with session resume | `Mobile/Components/Pages/AiChatPage.razor` | ⬜ Pending |
| 5.5 | Add mobile chat session endpoints to `InsightsApiService` | `Mobile/Services/Api/InsightsApiService.cs` | ⬜ Pending |
| 5.6 | Register chat page in mobile `App.razor` routing | `Mobile/Components/App.razor` | ⬜ Pending |
| 5.7 | Add navigation entry for chat | Mobile nav | ⬜ Pending |

### Phase 6 — Polish + Production Readiness

**Goal**: Production-ready with cost controls and observability.

| Step | Task | Status |
|------|------|--------|
| 6.1 | Verify rate limiting works via audit log queries (already wired in Phase 1) | ⬜ Pending |
| 6.2 | Add CSS styling for AI widgets (web + mobile) | ⬜ Pending |
| 6.3 | Tune prompts based on real output quality | ⬜ Pending |
| 6.4 | Add "AI Insights" toggle in user settings (optional) | ⬜ Pending |
| 6.5 | Test with production data volume | ⬜ Pending |
| 6.6 | Configure production API keys via environment variables | ⬜ Pending |
| 6.7 | Review audit logs for prompt quality and token usage patterns | ⬜ Pending |
| 6.8 | Implement audit log retention cleanup (scheduled task or manual) | ⬜ Pending |
| 6.9 | Verify admin AI Providers page works with production provider set | ⬜ Pending |
| 6.10 | Verify admin AI Usage dashboard shows accurate stats | ⬜ Pending |

---

## 11. Data Available for AI Prompts

The following data is available via existing services — no new database queries needed:

| Data | Source Service Method | Used In |
|---|---|---|
| Overall stats (rounds, best, avg, streak) | `IReportService.GetDashboardStatsAsync(userId)` | Dashboard, Chat |
| Scoring distribution (eagle→triple+) | `IReportService.GetScoringDistributionAsync(playerId)` | Dashboard, Report |
| Performance by par (3/4/5 averages) | `IReportService.GetPerformanceByParAsync(playerId)` | Dashboard, Report |
| Recent course history | `IReportService.GetCourseHistoryAsync(userId)` | Dashboard |
| Playing partner head-to-head | `IReportService.GetPlayingPartnerSummaryAsync(userId)` | Dashboard |
| Performance trend (score vs par over time) | `IReportService.GetPlayerPerformanceSummaryAsync(userId)` | Report |
| Player comparison stats | `IReportService.GetPlayerComparisonAsync(...)` | Report |
| Round detail with hole-by-hole scores | `IReportService.GetRoundDetailAsync(roundId, playerId)` | Chat |
| Course-specific performance | `IReportService.GetPlayerPerformanceForCourseAsync(...)` | Course detail |
| Club-specific performance | `IReportService.GetPlayerPerformanceForClubAsync(...)` | Club detail |
| Club-specific scoring distribution | `IReportService.GetScoringDistributionForClubAsync(...)` | Club detail |
| All player rounds | `IRoundService.GetRoundsForPlayerAsync(playerId, ...)` | Chat |
| Recent rounds at course | `IRoundService.GetRecentRoundsForCourseAsync(...)` | Course detail |
| Recent rounds at club | `IRoundService.GetRecentRoundsForClubAsync(...)` | Club detail |

---

## 12. Database Schema Changes

Four new tables support audit logging, rate limiting, persistent chat history, and provider settings management.

### 12.1 New Entity Models

```csharp
// GolfTrackerApp.Web/Models/AiProviderSettings.cs

public class AiProviderSettings
{
    public int AiProviderSettingsId { get; set; }                    // PK
    [Required][StringLength(50)] public string ProviderName { get; set; } = string.Empty; // e.g. "Anthropic"
    public bool Enabled { get; set; }                                // Admin toggle
    public int Priority { get; set; }                                // Admin-adjustable priority
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [StringLength(450)] public string? UpdatedByUserId { get; set; } // FK → ApplicationUser (who toggled it)
}
```

```csharp
// GolfTrackerApp.Web/Models/AiAuditLog.cs

public class AiAuditLog
{
    public int AiAuditLogId { get; set; }                           // PK
    [Required] public string ApplicationUserId { get; set; } = string.Empty; // FK → ApplicationUser
    public virtual ApplicationUser? ApplicationUser { get; set; }
    [Required] public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public int ResponseTimeMs { get; set; }
    [Required][StringLength(50)] public string InsightType { get; set; } = string.Empty; // "Dashboard", "PlayerReport", "Club", "Course", "Chat"
    [StringLength(50)] public string? ProviderName { get; set; }     // e.g. "OpenAI", "Anthropic"
    [StringLength(50)] public string? ModelUsed { get; set; }        // e.g. "gpt-4o-mini"
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public bool Success { get; set; }
    [StringLength(500)] public string? ErrorMessage { get; set; }
    public string? PromptSent { get; set; }                          // Full prompt (nullable — can be disabled for privacy)
    public string? ResponseReceived { get; set; }                    // Full response text
    public int? AiChatSessionId { get; set; }                        // FK → AiChatSession (for chat calls)
    public virtual AiChatSession? AiChatSession { get; set; }
}
```

```csharp
// GolfTrackerApp.Web/Models/AiChatSession.cs

public class AiChatSession
{
    public int AiChatSessionId { get; set; }                         // PK
    [Required] public string ApplicationUserId { get; set; } = string.Empty; // FK → ApplicationUser
    public virtual ApplicationUser? ApplicationUser { get; set; }
    [StringLength(100)] public string? Title { get; set; }           // Auto-generated from first message
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
    public bool IsArchived { get; set; }
    public virtual ICollection<AiChatSessionMessage> Messages { get; set; } = new List<AiChatSessionMessage>();
    public virtual ICollection<AiAuditLog> AuditLogs { get; set; } = new List<AiAuditLog>();
}
```

```csharp
// GolfTrackerApp.Web/Models/AiChatSessionMessage.cs

public class AiChatSessionMessage
{
    public int AiChatSessionMessageId { get; set; }                  // PK
    [Required] public int AiChatSessionId { get; set; }              // FK → AiChatSession
    public virtual AiChatSession? AiChatSession { get; set; }
    [Required][StringLength(20)] public string Role { get; set; } = string.Empty; // "user" or "assistant"
    [Required] public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

### 12.2 Schema Diagram

```
ApplicationUser
    ├── 1:N → AiAuditLog          (ApplicationUserId)
    └── 1:N → AiChatSession        (ApplicationUserId)
                  ├── 1:N → AiChatSessionMessage
                  └── 1:N → AiAuditLog  (optional link)

AiProviderSettings  (standalone — no FK to ApplicationUser)
    ProviderName (unique), Enabled, Priority, UpdatedAt, UpdatedByUserId
```

### 12.3 ApplicationDbContext Changes

Add to `ApplicationDbContext`:

```csharp
public DbSet<AiAuditLog> AiAuditLogs { get; set; }
public DbSet<AiChatSession> AiChatSessions { get; set; }
public DbSet<AiChatSessionMessage> AiChatSessionMessages { get; set; }
public DbSet<AiProviderSettings> AiProviderSettings { get; set; }
```

Add to `OnModelCreating`:

```csharp
// AI Audit Log
modelBuilder.Entity<AiAuditLog>(entity =>
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
    entity.HasIndex(a => a.RequestedAt); // For admin reporting
});

// AI Chat Session
modelBuilder.Entity<AiChatSession>(entity =>
{
    entity.HasOne(s => s.ApplicationUser)
        .WithMany()
        .HasForeignKey(s => s.ApplicationUserId)
        .OnDelete(DeleteBehavior.Cascade);

    entity.HasIndex(s => new { s.ApplicationUserId, s.LastMessageAt });
});

// AI Chat Session Message
modelBuilder.Entity<AiChatSessionMessage>(entity =>
{
    entity.HasOne(m => m.AiChatSession)
        .WithMany(s => s.Messages)
        .HasForeignKey(m => m.AiChatSessionId)
        .OnDelete(DeleteBehavior.Cascade);

    entity.HasIndex(m => new { m.AiChatSessionId, m.Timestamp });
});

// AI Provider Settings
modelBuilder.Entity<AiProviderSettings>(entity =>
{
    entity.HasIndex(s => s.ProviderName).IsUnique();
});
```

### 12.4 Migration Steps

Per the dual-provider rules in ARCHITECTURE.md Section 5.1:

**1. Create EF Core migration (SQLite/dev):**
```bash
cd GolfTrackerApp.Web
dotnet ef migrations add AddAiInsightsTables
```

**2. Update `EnsureNewTablesExistAsync()` in Program.cs (SQL Server/prod):**

```csharp
// AiChatSessions
if (!await TableExistsAsync(connection, "AiChatSessions"))
{
    await new SqlCommand(@"
        CREATE TABLE AiChatSessions (
            AiChatSessionId INT IDENTITY(1,1) PRIMARY KEY,
            ApplicationUserId NVARCHAR(450) NOT NULL,
            Title NVARCHAR(100) NULL,
            CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
            LastMessageAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
            IsArchived BIT NOT NULL DEFAULT 0,
            CONSTRAINT FK_AiChatSessions_AspNetUsers
                FOREIGN KEY (ApplicationUserId) REFERENCES AspNetUsers(Id)
                ON DELETE CASCADE
        );
        CREATE INDEX IX_AiChatSessions_UserId_LastMessage
            ON AiChatSessions (ApplicationUserId, LastMessageAt);
    ", connection).ExecuteNonQueryAsync();
}

// AiChatSessionMessages
if (!await TableExistsAsync(connection, "AiChatSessionMessages"))
{
    await new SqlCommand(@"
        CREATE TABLE AiChatSessionMessages (
            AiChatSessionMessageId INT IDENTITY(1,1) PRIMARY KEY,
            AiChatSessionId INT NOT NULL,
            Role NVARCHAR(20) NOT NULL,
            Content NVARCHAR(MAX) NOT NULL,
            Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
            CONSTRAINT FK_AiChatSessionMessages_Session
                FOREIGN KEY (AiChatSessionId) REFERENCES AiChatSessions(AiChatSessionId)
                ON DELETE CASCADE
        );
        CREATE INDEX IX_AiChatSessionMessages_SessionId_Timestamp
            ON AiChatSessionMessages (AiChatSessionId, Timestamp);
    ", connection).ExecuteNonQueryAsync();
}

// AiAuditLogs
if (!await TableExistsAsync(connection, "AiAuditLogs"))
{
    await new SqlCommand(@"
        CREATE TABLE AiAuditLogs (
            AiAuditLogId INT IDENTITY(1,1) PRIMARY KEY,
            ApplicationUserId NVARCHAR(450) NOT NULL,
            RequestedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
            ResponseTimeMs INT NOT NULL DEFAULT 0,
            InsightType NVARCHAR(50) NOT NULL,
            ProviderName NVARCHAR(50) NULL,
            ModelUsed NVARCHAR(50) NULL,
            PromptTokens INT NOT NULL DEFAULT 0,
            CompletionTokens INT NOT NULL DEFAULT 0,
            TotalTokens INT NOT NULL DEFAULT 0,
            Success BIT NOT NULL DEFAULT 0,
            ErrorMessage NVARCHAR(500) NULL,
            PromptSent NVARCHAR(MAX) NULL,
            ResponseReceived NVARCHAR(MAX) NULL,
            AiChatSessionId INT NULL,
            CONSTRAINT FK_AiAuditLogs_AspNetUsers
                FOREIGN KEY (ApplicationUserId) REFERENCES AspNetUsers(Id)
                ON DELETE CASCADE,
            CONSTRAINT FK_AiAuditLogs_AiChatSession
                FOREIGN KEY (AiChatSessionId) REFERENCES AiChatSessions(AiChatSessionId)
                ON DELETE NO ACTION
        );
        CREATE INDEX IX_AiAuditLogs_UserId_RequestedAt
            ON AiAuditLogs (ApplicationUserId, RequestedAt);
        CREATE INDEX IX_AiAuditLogs_RequestedAt
            ON AiAuditLogs (RequestedAt);
    ", connection).ExecuteNonQueryAsync();
}

// AiProviderSettings
if (!await TableExistsAsync(connection, \"AiProviderSettings\"))
{
    await new SqlCommand(@\"
        CREATE TABLE AiProviderSettings (
            AiProviderSettingsId INT IDENTITY(1,1) PRIMARY KEY,
            ProviderName NVARCHAR(50) NOT NULL,
            Enabled BIT NOT NULL DEFAULT 0,
            Priority INT NOT NULL DEFAULT 99,
            UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
            UpdatedByUserId NVARCHAR(450) NULL
        );
        CREATE UNIQUE INDEX IX_AiProviderSettings_ProviderName
            ON AiProviderSettings (ProviderName);
    \", connection).ExecuteNonQueryAsync();
}
```

Note: `AiAuditLogs.AiChatSessionId` uses `ON DELETE NO ACTION` (not CASCADE) to avoid multiple cascade paths through `AspNetUsers` → `AiChatSessions` → `AiAuditLogs` vs `AspNetUsers` → `AiAuditLogs`.

### 12.5 IAiAuditService

A dedicated service for audit logging and rate-limit enforcement, keeping these concerns out of the insight service.

```csharp
// GolfTrackerApp.Web/Services/IAiAuditService.cs

public interface IAiAuditService
{
    Task LogAsync(AiAuditLog entry);
    Task<bool> IsRateLimitedAsync(string userId);
    Task<int> GetUsageCountAsync(string userId, TimeSpan window);
    Task<int> GetTotalTokensUsedAsync(string userId, TimeSpan window);
}
```

```csharp
// GolfTrackerApp.Web/Services/AiAuditService.cs

public class AiAuditService : IAiAuditService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiAuditService> _logger;

    public AiAuditService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IConfiguration configuration,
        ILogger<AiAuditService> logger)
    {
        _contextFactory = contextFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task LogAsync(AiAuditLog entry)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.AiAuditLogs.Add(entry);
        await context.SaveChangesAsync();
    }

    public async Task<bool> IsRateLimitedAsync(string userId)
    {
        var limit = _configuration.GetValue<int>("AiInsights:RateLimitPerUserPerHour");
        if (limit <= 0) return false;
        var count = await GetUsageCountAsync(userId, TimeSpan.FromHours(1));
        return count >= limit;
    }

    public async Task<int> GetUsageCountAsync(string userId, TimeSpan window)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var since = DateTime.UtcNow - window;
        return await context.AiAuditLogs
            .AsNoTracking()
            .CountAsync(a => a.ApplicationUserId == userId
                          && a.RequestedAt >= since
                          && a.Success);
    }

    public async Task<int> GetTotalTokensUsedAsync(string userId, TimeSpan window)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var since = DateTime.UtcNow - window;
        return await context.AiAuditLogs
            .AsNoTracking()
            .Where(a => a.ApplicationUserId == userId
                     && a.RequestedAt >= since
                     && a.Success)
            .SumAsync(a => a.TotalTokens);
    }
}
```

### 12.6 IAiChatService

A dedicated service for persistent chat session management.

```csharp
// GolfTrackerApp.Web/Services/IAiChatService.cs

public interface IAiChatService
{
    Task<List<AiChatSession>> GetSessionsAsync(string userId, int limit = 20);
    Task<AiChatSession?> GetSessionAsync(int sessionId, string userId);
    Task<AiChatSession> CreateSessionAsync(string userId, string firstMessage);
    Task AddMessageAsync(int sessionId, string role, string content);
    Task<List<AiChatSessionMessage>> GetMessagesAsync(int sessionId, int limit = 50);
    Task ArchiveSessionAsync(int sessionId, string userId);
}
```

```csharp
// GolfTrackerApp.Web/Services/AiChatService.cs

public class AiChatService : IAiChatService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<AiChatService> _logger;

    public AiChatService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<AiChatService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<List<AiChatSession>> GetSessionsAsync(string userId, int limit = 20)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AiChatSessions
            .AsNoTracking()
            .Where(s => s.ApplicationUserId == userId && !s.IsArchived)
            .OrderByDescending(s => s.LastMessageAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<AiChatSession?> GetSessionAsync(int sessionId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AiChatSessions
            .AsNoTracking()
            .Include(s => s.Messages.OrderBy(m => m.Timestamp))
            .FirstOrDefaultAsync(s => s.AiChatSessionId == sessionId
                                   && s.ApplicationUserId == userId);
    }

    public async Task<AiChatSession> CreateSessionAsync(string userId, string firstMessage)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var session = new AiChatSession
        {
            ApplicationUserId = userId,
            Title = firstMessage.Length > 80
                ? firstMessage[..80] + "..."
                : firstMessage
        };
        context.AiChatSessions.Add(session);
        await context.SaveChangesAsync();
        return session;
    }

    public async Task AddMessageAsync(int sessionId, string role, string content)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.AiChatSessionMessages.Add(new AiChatSessionMessage
        {
            AiChatSessionId = sessionId,
            Role = role,
            Content = content
        });
        // Update session timestamp
        var session = await context.AiChatSessions.FindAsync(sessionId);
        if (session != null) session.LastMessageAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task<List<AiChatSessionMessage>> GetMessagesAsync(int sessionId, int limit = 50)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AiChatSessionMessages
            .AsNoTracking()
            .Where(m => m.AiChatSessionId == sessionId)
            .OrderBy(m => m.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task ArchiveSessionAsync(int sessionId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var session = await context.AiChatSessions
            .FirstOrDefaultAsync(s => s.AiChatSessionId == sessionId
                                   && s.ApplicationUserId == userId);
        if (session != null)
        {
            session.IsArchived = true;
            await context.SaveChangesAsync();
        }
    }
}
```

### 12.7 Configuration Options for Audit Logging

Add to the `AiInsights` config section:

```json
{
  "AiInsights": {
    "AuditLogging": {
      "Enabled": true,
      "LogPrompts": true,
      "LogResponses": true,
      "RetentionDays": 90
    }
  }
}
```

- `LogPrompts` / `LogResponses`: Can be disabled in production if prompt/response storage raises privacy concerns (the structured metadata — provider, tokens, timing — is always logged)
- `RetentionDays`: For a future scheduled cleanup task

### 12.8 How Audit Logging Integrates with AiInsightService

The `AiInsightService` wraps every AI call with audit logging. The updated flow:

```csharp
// In each insight method (dashboard, report, club, course, chat):
if (await _auditService.IsRateLimitedAsync(userId))
    return new AiInsightResult { Success = false, ErrorMessage = "Rate limit reached. Try again later." };

var stopwatch = Stopwatch.StartNew();
var result = await _aiRouting.RouteCompletionAsync(systemPrompt, userPrompt, ...);
stopwatch.Stop();

await _auditService.LogAsync(new AiAuditLog
{
    ApplicationUserId = userId,
    InsightType = "Dashboard",  // or "PlayerReport", "Club", "Course", "Chat"
    ProviderName = result.ProviderUsed,
    ModelUsed = result.ModelUsed,
    PromptTokens = result.PromptTokens,
    CompletionTokens = result.CompletionTokens,
    TotalTokens = result.TokensUsed,
    ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
    Success = result.Success,
    ErrorMessage = result.ErrorMessage,
    PromptSent = logPrompts ? userPrompt : null,
    ResponseReceived = logResponses ? result.Content : null,
    AiChatSessionId = chatSessionId  // null for non-chat calls
});
```

This means `AiInsightResult` gains additional properties to capture provider detail (see updated model in Section 4.3).

---

## 13. Testing Strategy

### Unit Tests
- `AiRoutingService`: Mock `IAiProviderService` implementations, test failover ordering, circuit breaker behaviour, configuration edge cases (no providers enabled, all providers fail)
- `AiInsightService`: Mock `IAiRoutingService` + `IReportService` + `IAiAuditService` + `IAiChatService`, verify correct data is gathered and prompts are constructed properly, test caching behaviour, test disabled state, verify audit log is written on every call
- `AiAuditService`: Test rate limiting logic with boundary values, verify token counting queries
- `AiChatService`: Test session creation, message persistence, session retrieval with message ordering, archive behaviour
- Individual providers: Mock `HttpClient` responses, test JSON parsing for each provider format

### Integration Tests
- End-to-end: Configure one real provider (with low token limits), verify insight is returned and displayed, verify audit row is written to database
- Controller tests: Verify auth is enforced, error handling returns 500 not stack traces
- Mobile API flow: Verify `InsightsApiService` correctly calls endpoints and deserialises responses
- Chat persistence: Start chat → close page → reopen → verify history is loaded from database

### Manual Testing
- Verify insights are relevant and accurate (read the AI output)
- Test failover by disabling the primary provider
- Test rate limiting by exceeding the limit (verify count from `AiAuditLogs`)
- Test with users who have 0 rounds, 1 round, 100+ rounds
- Test on mobile (Android emulator + iOS simulator)

---

## 14. Security Considerations

| Concern | Mitigation |
|---|---|
| API keys exposed to client | Keys only on server; mobile calls `api/insights/*` endpoints, never AI providers directly |
| Prompt injection via user data | User data (names, course names) is inserted into structured prompts, not as raw instructions. System prompt is server-controlled |
| Cost runaway | Rate limiting per user per hour (enforced via `AiAuditLog` queries); token limits per request; caching to avoid duplicate calls; audit log provides token usage visibility |
| Unauthorised access | All insight endpoints require JWT auth via `BaseApiController` |
| Data leakage | Each user only sees insights generated from their own data; `GetCurrentUserId()` scopes all queries |
| Audit / compliance | Every AI call is logged with full metadata (provider, tokens, timing, success/failure). Prompt/response storage is configurable via `AuditLogging:LogPrompts` / `LogResponses` |
| Data sent to third parties | Audit log records exactly what data was sent to each AI provider, enabling compliance review |

---

## 15. Admin Area — AI Management

The admin area (existing `[Authorize(Roles = "Admin")]` section) gains a new "AI Providers" page for managing AI provider settings and monitoring usage.

### 15.1 Admin Pages

#### 15.1.1 AI Provider Management (`/admin/ai-providers`)

Allows administrators to enable/disable providers and adjust failover priority without code changes or redeployment.

Each provider is displayed as a card row with:
- **Position number** (1, 2, 3…) showing current priority
- **Up/down arrow buttons** to swap with adjacent provider — saves immediately via `UpdatePrioritiesAsync`
- **Provider name** and **model** (read-only, from `appsettings.json`)
- **API Key chip** — green "API Key" if configured, red "No Key" if missing
- **Enabled toggle** — `MudSwitch`, disabled when no API key is configured
- **Last Updated** timestamp

```razor
@* Simplified structure — see AiProviders.razor for full implementation *@
<div class="d-flex flex-column gap-2 mb-4">
    @for (int i = 0; i < providerViewModels.Count; i++)
    {
        var index = i;
        var vm = providerViewModels[index];
        <MudPaper Elevation="2" Class="pa-3 d-flex align-center gap-3">
            <MudText>@(index + 1)</MudText>
            <MudButtonGroup>
                <MudIconButton Icon="KeyboardArrowUp" Disabled="@(index == 0)"
                               OnClick="@(() => MoveProvider(index, -1))" />
                <MudIconButton Icon="KeyboardArrowDown" Disabled="@(index == providerViewModels.Count - 1)"
                               OnClick="@(() => MoveProvider(index, 1))" />
            </MudButtonGroup>
            <MudText><strong>@vm.ProviderName</strong></MudText>
            <MudText>@vm.Model</MudText>
            <MudChip Color="@(vm.HasApiKey ? Color.Success : Color.Error)">...</MudChip>
            <MudSwitch Value="vm.Enabled" Disabled="@(!vm.HasApiKey)" ... />
            <MudText>@vm.UpdatedAt</MudText>
        </MudPaper>
    }
</div>
```

Key behaviours:
- Providers without an API key configured show "No Key" chip and the Enabled switch is disabled
- Up/down arrows swap the provider with its neighbour and auto-save all priorities via `UpdatePrioritiesAsync`
- Enabled toggle saved immediately via `UpdateAsync` — no "Save" button needed
- Shows model name from `appsettings.json` (read-only in admin UI, changed via config)
- Arrow buttons are disabled at list boundaries (up disabled for first, down disabled for last)

#### 15.1.2 AI Usage Dashboard (`/admin/ai-usage`)

Read-only dashboard showing AI usage statistics from the `AiAuditLogs` table.

| Widget | Data Source |
|---|---|
| Total AI calls (24h / 7d / 30d) | `COUNT(*)` from `AiAuditLogs` grouped by time window |
| Token usage (24h / 7d / 30d) | `SUM(TotalTokens)` from `AiAuditLogs` |
| Success rate | `COUNT(Success=true) / COUNT(*)` percentage |
| Provider distribution | Pie chart of calls per provider |
| Average response time | `AVG(ResponseTimeMs)` per provider |
| Top users by token consumption | Top 10 users by total tokens in the last 30 days |
| Error log | Recent failed AI calls with error messages |

### 15.2 Navigation

Add to `NavMenu.razor` under the existing Admin section:

```razor
<AuthorizeView Roles="Admin">
    <MudListSubheader>Admin</MudListSubheader>
    <MudNavLink Href="admin/datamigration" Icon="@Icons.Material.Filled.Storage">Data Migration</MudNavLink>
    <MudNavLink Href="admin/ai-providers" Icon="@Icons.Material.Filled.SmartToy">AI Providers</MudNavLink>
    <MudNavLink Href="admin/ai-usage" Icon="@Icons.Material.Filled.Analytics">AI Usage</MudNavLink>
</AuthorizeView>
```

### 15.3 Service Layer

```csharp
// GolfTrackerApp.Web/Services/IAiProviderSettingsService.cs

public interface IAiProviderSettingsService
{
    Task<List<AiProviderSettings>> GetAllAsync();
    Task<AiProviderSettings?> GetByNameAsync(string providerName);
    Task UpdateAsync(AiProviderSettings settings, string updatedByUserId);
    Task UpdatePrioritiesAsync(List<(int Id, int Priority)> priorities, string updatedByUserId);
    Task SeedFromConfigAsync(); // Called on startup
}
```

---

## 16. Future Enhancements (Out of Scope for Initial Implementation)

- **User preference for AI provider**: Let users choose their preferred provider
- **Streaming responses**: Use Server-Sent Events for real-time chat output
- **Round-specific AI analysis**: "Analyse this round" button on individual round detail pages
- **AI-suggested practice plans**: Weekly practice recommendations based on weakness analysis
- **Comparative insights**: "How do you compare to other players at this course?"
- **Voice input**: Speech-to-text for mobile chat
