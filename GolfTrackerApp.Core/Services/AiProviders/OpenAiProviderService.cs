using System.Net.Http.Json;
using System.Text.Json;
using GolfTrackerApp.Web.Models;
using Microsoft.Extensions.Logging;

namespace GolfTrackerApp.Web.Services.AiProviders
{
    public class OpenAiProviderService : IAiProviderService
    {
        private readonly HttpClient _httpClient;
        private readonly AiProviderConfig _config;
        private readonly ILogger<OpenAiProviderService> _logger;

        public string ProviderName => _config.Name;

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
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cts.Token);
                _logger.LogWarning("OpenAI API error {StatusCode}: {Body}", (int)response.StatusCode, errorBody);
                response.EnsureSuccessStatusCode();
            }

            var json = await response.Content.ReadAsStringAsync(cts.Token);
            using var doc = JsonDocument.Parse(json);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            var usage = doc.RootElement.GetProperty("usage");
            var promptTokens = usage.GetProperty("prompt_tokens").GetInt32();
            var completionTokens = usage.GetProperty("completion_tokens").GetInt32();
            var totalTokens = usage.GetProperty("total_tokens").GetInt32();

            return new AiInsightResult
            {
                Success = true,
                Content = content,
                ProviderUsed = ProviderName,
                ModelUsed = _config.Model,
                TokensUsed = totalTokens,
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens
            };
        }
    }
}
