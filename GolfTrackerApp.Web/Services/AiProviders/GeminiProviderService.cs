using System.Net.Http.Json;
using System.Text.Json;
using GolfTrackerApp.Web.Models;
using Microsoft.Extensions.Logging;

namespace GolfTrackerApp.Web.Services.AiProviders
{
    public class GeminiProviderService : IAiProviderService
    {
        private readonly HttpClient _httpClient;
        private readonly AiProviderConfig _config;
        private readonly ILogger<GeminiProviderService> _logger;

        public string ProviderName => _config.Name;

        public GeminiProviderService(
            HttpClient httpClient,
            AiProviderConfig config,
            ILogger<GeminiProviderService> logger)
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
            var url = $"{_config.Endpoint}/{_config.Model}:generateContent?key={_config.ApiKey}";

            var requestBody = new
            {
                systemInstruction = new
                {
                    parts = new[] { new { text = systemPrompt } }
                },
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = userPrompt } }
                    }
                },
                generationConfig = new
                {
                    maxOutputTokens = maxTokens,
                    temperature = temperature
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(requestBody)
            };

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_config.TimeoutSeconds));

            var response = await _httpClient.SendAsync(request, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cts.Token);
                _logger.LogWarning("Gemini API error {StatusCode}: {Body}", (int)response.StatusCode, errorBody);
                response.EnsureSuccessStatusCode();
            }

            var json = await response.Content.ReadAsStringAsync(cts.Token);
            using var doc = JsonDocument.Parse(json);

            var content = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;

            // Gemini provides token counts in usageMetadata
            int promptTokens = 0, completionTokens = 0;
            if (doc.RootElement.TryGetProperty("usageMetadata", out var usage))
            {
                if (usage.TryGetProperty("promptTokenCount", out var pt))
                    promptTokens = pt.GetInt32();
                if (usage.TryGetProperty("candidatesTokenCount", out var ct))
                    completionTokens = ct.GetInt32();
            }

            return new AiInsightResult
            {
                Success = true,
                Content = content,
                ProviderUsed = ProviderName,
                ModelUsed = _config.Model,
                TokensUsed = promptTokens + completionTokens,
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens
            };
        }
    }
}
