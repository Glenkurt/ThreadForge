using System.Text;
using System.Text.Json;
using Api.Models.DTOs;
using Api.Models.Options;
using Microsoft.Extensions.Options;

namespace Api.Services;

public sealed class WebSearchService : IWebSearchService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IXaiChatClient _xai;
    private readonly SerperOptions _serperOptions;
    private readonly XaiOptions _xaiOptions;
    private readonly ILogger<WebSearchService> _logger;

    public WebSearchService(
        HttpClient httpClient,
        IXaiChatClient xai,
        IOptions<SerperOptions> serperOptions,
        IOptions<XaiOptions> xaiOptions,
        ILogger<WebSearchService> logger)
    {
        _httpClient = httpClient;
        _xai = xai;
        _serperOptions = serperOptions.Value;
        _xaiOptions = xaiOptions.Value;
        _logger = logger;
    }

    public async Task<string[]> GenerateSearchQueriesAsync(string topic, CancellationToken cancellationToken)
    {
        var lightModel = string.IsNullOrWhiteSpace(_xaiOptions.LightModel)
            ? "grok-3-mini-fast"
            : _xaiOptions.LightModel;

        var systemPrompt = """
            You are a search query optimizer. Given a topic, generate 2-3 Google search queries
            that will find the most relevant, recent, and factual information about the topic.

            Focus on queries that will surface:
            - Recent statistics and data
            - Expert opinions and analysis
            - Trends and developments
            - Concrete examples and case studies

            OUTPUT FORMAT: Return ONLY valid JSON:
            {"queries":["query1","query2","query3"]}

            No markdown, no explanations outside the JSON.
            """;

        var userPrompt = $"Generate optimized Google search queries for this topic: {topic}";

        var result = await _xai.CreateChatCompletionAsync(
            lightModel,
            new List<(string Role, string Content)>
            {
                ("system", systemPrompt),
                ("user", userPrompt)
            },
            new XaiChatOptions(Temperature: 0.3, MaxTokens: 200, JsonMode: true),
            cancellationToken);

        try
        {
            using var doc = JsonDocument.Parse(result.Content);
            if (doc.RootElement.TryGetProperty("queries", out var queriesEl) &&
                queriesEl.ValueKind == JsonValueKind.Array)
            {
                var queries = queriesEl.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString()!)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Take(3)
                    .ToArray();

                _logger.LogInformation(
                    "Generated {Count} search queries for topic: {Topic}",
                    queries.Length, topic.Length > 100 ? topic[..100] + "..." : topic);

                return queries;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse search queries from LLM response");
        }

        // Fallback: use the topic directly as a single query
        _logger.LogWarning("Falling back to raw topic as search query");
        return [topic];
    }

    public async Task<List<SerperSearchResult>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_serperOptions.ApiKey))
        {
            _logger.LogWarning("Serper API key is not configured. Skipping web search.");
            return [];
        }

        var requestBody = JsonSerializer.Serialize(new { q = query, num = 10 }, JsonOptions);
        using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        var baseUrl = string.IsNullOrWhiteSpace(_serperOptions.BaseUrl)
            ? "https://google.serper.dev"
            : _serperOptions.BaseUrl.TrimEnd('/');

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/search");
        request.Headers.Add("X-API-KEY", _serperOptions.ApiKey);
        request.Content = content;

        HttpResponseMessage response;
        string responseBody;

        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
            responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Serper API request timed out for query: {Query}", query);
            return [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Serper API network error for query: {Query}", query);
            return [];
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Serper API error. StatusCode: {StatusCode}, Query: {Query}, Response: {Response}",
                (int)response.StatusCode, query,
                responseBody.Length > 300 ? responseBody[..300] + "..." : responseBody);
            return [];
        }

        _logger.LogInformation("Serper API call completed for query: {Query}", query);

        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var results = new List<SerperSearchResult>();

            if (doc.RootElement.TryGetProperty("organic", out var organic) &&
                organic.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in organic.EnumerateArray())
                {
                    var title = item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                    var snippet = item.TryGetProperty("snippet", out var s) ? s.GetString() ?? "" : "";
                    var link = item.TryGetProperty("link", out var l) ? l.GetString() ?? "" : "";

                    if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(snippet))
                    {
                        results.Add(new SerperSearchResult(title, snippet, link));
                    }
                }
            }

            _logger.LogInformation(
                "Parsed {Count} organic results for query: {Query}",
                results.Count, query);

            return results;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse Serper response for query: {Query}", query);
            return [];
        }
    }

    public async Task<string> SynthesizeAsync(
        string topic,
        List<SerperSearchResult> results,
        CancellationToken cancellationToken)
    {
        if (results.Count == 0)
        {
            return string.Empty;
        }

        var lightModel = string.IsNullOrWhiteSpace(_xaiOptions.LightModel)
            ? "grok-3-mini-fast"
            : _xaiOptions.LightModel;

        var systemPrompt = """
            You are a research analyst. Given a topic and a set of web search results (titles + snippets),
            extract and organize the most useful information for writing a compelling Twitter thread.

            Your synthesis must include (when available):
            - KEY FACTS & STATISTICS: Specific numbers, percentages, dates
            - TRENDS & INSIGHTS: What's changing, emerging patterns
            - CONTRARIAN VIEWS: Debates, opposing perspectives
            - CONCRETE EXAMPLES: Real companies, people, case studies
            - NOTABLE SOURCES: Which sources are most authoritative

            Be concise and factual. Organize with clear headings.
            Do NOT write a thread — just provide structured raw material.
            Output plain text with clear section headings.
            """;

        var userPromptBuilder = new StringBuilder();
        userPromptBuilder.AppendLine($"Topic: {topic}");
        userPromptBuilder.AppendLine();
        userPromptBuilder.AppendLine("SEARCH RESULTS:");
        userPromptBuilder.AppendLine();

        foreach (var result in results)
        {
            userPromptBuilder.AppendLine($"Title: {result.Title}");
            userPromptBuilder.AppendLine($"Snippet: {result.Snippet}");
            userPromptBuilder.AppendLine($"Source: {result.Link}");
            userPromptBuilder.AppendLine();
        }

        userPromptBuilder.AppendLine("Please synthesize the above search results into structured research context.");

        var llmResult = await _xai.CreateChatCompletionAsync(
            lightModel,
            new List<(string Role, string Content)>
            {
                ("system", systemPrompt),
                ("user", userPromptBuilder.ToString())
            },
            new XaiChatOptions(Temperature: 0.3, MaxTokens: 1500, JsonMode: false),
            cancellationToken);

        _logger.LogInformation(
            "Research synthesis completed. Length: {Length} chars, Tokens: {Tokens}",
            llmResult.Content.Length, llmResult.TotalTokens);

        return llmResult.Content;
    }
}
