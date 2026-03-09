using System.Net.Http.Json;
using System.Text.Json;
using GolfTrackerApp.Web.Models;
using Microsoft.Extensions.Logging;

namespace GolfTrackerApp.Web.Services.AiProviders
{
    /// <summary>
    /// Manus uses an async task-based API (OpenAI Responses API compatible).
    /// Tasks are created, then polled until completion.
    /// Auth via API_KEY header (not Bearer token).
    /// </summary>
    public class ManusProviderService : IAiProviderService
    {
        private readonly HttpClient _httpClient;
        private readonly AiProviderConfig _config;
        private readonly ILogger<ManusProviderService> _logger;

        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);

        public string ProviderName => _config.Name;

        public ManusProviderService(
            HttpClient httpClient,
            AiProviderConfig config,
            ILogger<ManusProviderService> logger)
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
            var endpoint = string.IsNullOrEmpty(_config.Endpoint)
                ? "https://api.manus.im/v1/responses"
                : _config.Endpoint.TrimEnd('/') + "/v1/responses";

            var requestBody = new
            {
                input = new object[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "input_text", text = $"{systemPrompt}\n\n{userPrompt}" }
                        }
                    }
                },
                task_mode = "agent",
                agent_profile = string.IsNullOrEmpty(_config.Model) ? "manus-1.6-lite" : _config.Model,
                hide_in_task_list = true
            };

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(requestBody)
            };
            request.Headers.Add("API_KEY", _config.ApiKey);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_config.TimeoutSeconds));

            // Step 1: Create the task
            var response = await _httpClient.SendAsync(request, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cts.Token);
                _logger.LogWarning("Manus API error {StatusCode}: {Body}", (int)response.StatusCode, errorBody);
                response.EnsureSuccessStatusCode();
            }

            var createJson = await response.Content.ReadAsStringAsync(cts.Token);
            using var createDoc = JsonDocument.Parse(createJson);
            var taskId = createDoc.RootElement.GetProperty("id").GetString()
                ?? throw new InvalidOperationException("Manus task creation returned no id");
            var status = createDoc.RootElement.GetProperty("status").GetString() ?? "";

            _logger.LogInformation("Manus task created: {TaskId}, status: {Status}", taskId, status);

            // Step 2: Poll until completed
            var retrieveUrl = endpoint.TrimEnd('/');
            // If endpoint ends with /v1/responses, use /v1/responses/{id}
            var pollUrl = $"{retrieveUrl}/{taskId}";

            while (status == "running" || status == "pending")
            {
                cts.Token.ThrowIfCancellationRequested();
                await Task.Delay(PollInterval, cts.Token);

                var pollRequest = new HttpRequestMessage(HttpMethod.Get, pollUrl);
                pollRequest.Headers.Add("API_KEY", _config.ApiKey);

                var pollResponse = await _httpClient.SendAsync(pollRequest, cts.Token);
                if (!pollResponse.IsSuccessStatusCode)
                {
                    var errorBody = await pollResponse.Content.ReadAsStringAsync(cts.Token);
                    _logger.LogWarning("Manus poll error {StatusCode}: {Body}", (int)pollResponse.StatusCode, errorBody);
                    pollResponse.EnsureSuccessStatusCode();
                }

                var pollJson = await pollResponse.Content.ReadAsStringAsync(cts.Token);
                using var pollDoc = JsonDocument.Parse(pollJson);
                status = pollDoc.RootElement.GetProperty("status").GetString() ?? "";

                if (status == "completed")
                {
                    return ExtractResult(pollDoc);
                }

                if (status == "error")
                {
                    return new AiInsightResult
                    {
                        Success = false,
                        ErrorMessage = "Manus task failed",
                        ProviderUsed = ProviderName
                    };
                }

                _logger.LogDebug("Manus task {TaskId} still {Status}", taskId, status);
            }

            // If we got here with "completed" on creation (unlikely but possible)
            if (status == "completed")
            {
                return ExtractResult(createDoc);
            }

            return new AiInsightResult
            {
                Success = false,
                ErrorMessage = $"Manus task ended with unexpected status: {status}",
                ProviderUsed = ProviderName
            };
        }

        private AiInsightResult ExtractResult(JsonDocument doc)
        {
            var root = doc.RootElement;
            var content = "";

            if (root.TryGetProperty("output", out var output) && output.ValueKind == JsonValueKind.Array)
            {
                foreach (var msg in output.EnumerateArray())
                {
                    var role = msg.TryGetProperty("role", out var r) ? r.GetString() : "";
                    if (role != "assistant") continue;

                    if (msg.TryGetProperty("content", out var contentArr) && contentArr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in contentArr.EnumerateArray())
                        {
                            if (item.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                            {
                                var t = text.GetString();
                                if (!string.IsNullOrEmpty(t))
                                    content = t; // Take the last text content from assistant
                            }
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(content))
            {
                return new AiInsightResult
                {
                    Success = false,
                    ErrorMessage = "Manus task completed but no text output found",
                    ProviderUsed = ProviderName
                };
            }

            var model = root.TryGetProperty("model", out var m) ? m.GetString() ?? "" : "";
            var tokens = 0;
            if (root.TryGetProperty("metadata", out var meta)
                && meta.TryGetProperty("credit_usage", out var credits))
            {
                int.TryParse(credits.GetString(), out tokens);
            }

            return new AiInsightResult
            {
                Success = true,
                Content = content,
                ProviderUsed = ProviderName,
                ModelUsed = model,
                TokensUsed = tokens
            };
        }
    }
}
