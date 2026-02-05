using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Api.Models.Options;
using Microsoft.Extensions.Options;

namespace Api.Services;

public sealed class XaiChatClient : IXaiChatClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly XaiOptions _options;
    private readonly ILogger<XaiChatClient> _logger;

    public XaiChatClient(
        HttpClient httpClient,
        IOptions<XaiOptions> options,
        ILogger<XaiChatClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        }

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }
    }

    public async Task<XaiChatCompletionResult> CreateChatCompletionAsync(
        string model,
        IReadOnlyList<(string Role, string Content)> messages,
        XaiChatOptions? options,
        CancellationToken cancellationToken)
    {
        var opts = options ?? new XaiChatOptions();

        var payload = new Dictionary<string, object>
        {
            ["model"] = model,
            ["messages"] = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
            ["temperature"] = opts.Temperature
        };

        if (opts.MaxTokens.HasValue)
        {
            payload["max_tokens"] = opts.MaxTokens.Value;
        }

        if (opts.JsonMode)
        {
            payload["response_format"] = new { type = "json_object" };
        }

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        string responseBody;

        try
        {
            response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);
            responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "xAI API request timed out. Model: {Model}", model);
            throw new HttpRequestException("xAI API request timed out", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "xAI API network error. Model: {Model}, Message: {Message}", model, ex.Message);
            throw;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "xAI API error. StatusCode: {StatusCode}, Model: {Model}, Response: {Response}",
                (int)response.StatusCode,
                model,
                responseBody.Length > 500 ? responseBody[..500] + "..." : responseBody);

            throw new HttpRequestException(
                $"xAI API error: {(int)response.StatusCode} {response.ReasonPhrase}. Response: {responseBody}",
                null,
                response.StatusCode);
        }

        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            // Safe JSON parsing with TryGetProperty
            string contentText = string.Empty;
            if (root.TryGetProperty("choices", out var choices) &&
                choices.GetArrayLength() > 0 &&
                choices[0].TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var contentElement))
            {
                contentText = contentElement.GetString() ?? string.Empty;
            }
            else
            {
                _logger.LogWarning(
                    "xAI API returned unexpected response structure. Response: {Response}",
                    responseBody.Length > 500 ? responseBody[..500] + "..." : responseBody);
            }

            // Parse token usage if available
            int? promptTokens = null;
            int? completionTokens = null;
            int? totalTokens = null;

            if (root.TryGetProperty("usage", out var usage))
            {
                if (usage.TryGetProperty("prompt_tokens", out var pt))
                    promptTokens = pt.GetInt32();
                if (usage.TryGetProperty("completion_tokens", out var ct))
                    completionTokens = ct.GetInt32();
                if (usage.TryGetProperty("total_tokens", out var tt))
                    totalTokens = tt.GetInt32();
            }

            _logger.LogDebug(
                "xAI API success. Model: {Model}, PromptTokens: {PromptTokens}, CompletionTokens: {CompletionTokens}",
                model, promptTokens, completionTokens);

            return new XaiChatCompletionResult(contentText, promptTokens, completionTokens, totalTokens);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex,
                "xAI API returned invalid JSON. Response: {Response}",
                responseBody.Length > 500 ? responseBody[..500] + "..." : responseBody);
            throw new HttpRequestException("xAI API returned invalid JSON response", ex);
        }
    }
}
