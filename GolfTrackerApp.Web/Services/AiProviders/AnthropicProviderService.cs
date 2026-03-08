using System.Net.Http.Json;
using System.Text.Json;
using GolfTrackerApp.Web.Models;
using Microsoft.Extensions.Logging;

namespace GolfTrackerApp.Web.Services.AiProviders
{
    public class AnthropicProviderService : IAiProviderService
    {
        private readonly HttpClient _httpClient;
        private readonly AiProviderConfig _config;
        private readonly ILogger<AnthropicProviderService> _logger;

        public string ProviderName => _config.Name;

        public AnthropicProviderService(
            HttpClient httpClient,
            AiProviderConfig config,
            ILogger<AnthropicProviderService> logger)
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
                max_tokens = maxTokens,
                system = systemPrompt,
                messages = new[]
                {
                    new { role = "user", content = userPrompt }
                },
                temperature = temperature
            };

            var request = new HttpRequestMessage(HttpMethod.Post, _config.Endpoint)
            {
                Content = JsonContent.Create(requestBody)
            };
            request.Headers.Add("x-api-key", _config.ApiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_config.TimeoutSeconds));

            var response = await _httpClient.SendAsync(request, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cts.Token);
                _logger.LogWarning("Anthropic API error {StatusCode}: {Body}", (int)response.StatusCode, errorBody);
                response.EnsureSuccessStatusCode();
            }

            var json = await response.Content.ReadAsStringAsync(cts.Token);
            using var doc = JsonDocument.Parse(json);

            var content = doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;

            var usage = doc.RootElement.GetProperty("usage");
            var promptTokens = usage.GetProperty("input_tokens").GetInt32();
            var completionTokens = usage.GetProperty("output_tokens").GetInt32();

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
