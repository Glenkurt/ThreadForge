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

    public XaiChatClient(HttpClient httpClient, IOptions<XaiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

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
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            model,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
            temperature = 0.7,
            // xAI is OpenAI-compatible; request strict JSON if supported
            response_format = new { type = "json_object" }
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"xAI request failed: {(int)response.StatusCode} {response.ReasonPhrase}",
                null,
                response.StatusCode);
        }

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        var contentText = root
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

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

        return new XaiChatCompletionResult(contentText, promptTokens, completionTokens, totalTokens);
    }
}
