using System.Text.Json;
using System.Text.RegularExpressions;
using Api.Models.DTOs;
using Api.Models.Options;
using Microsoft.Extensions.Options;

namespace Api.Services;

public sealed partial class ProfileAnalysisService : IProfileAnalysisService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IXaiChatClient _xai;
    private readonly XaiOptions _xaiOptions;
    private readonly ILogger<ProfileAnalysisService> _logger;

    public ProfileAnalysisService(
        IXaiChatClient xai, 
        IOptions<XaiOptions> xaiOptions,
        ILogger<ProfileAnalysisService> logger)
    {
        _xai = xai;
        _xaiOptions = xaiOptions.Value;
        _logger = logger;
    }

    public async Task<ProfileAnalysisResponseDto> AnalyzeAsync(
        ProfileAnalysisRequestDto request,
        string clientId,
        CancellationToken cancellationToken)
    {
        var username = ValidateAndNormalizeUsername(request.Username);
        
        // Since we can't actually scrape Twitter without API credentials,
        // we'll generate a brand analysis based on the username using AI
        // In a real implementation, you'd fetch tweets here using a scraper or API
        
        var model = string.IsNullOrWhiteSpace(_xaiOptions.Model) ? "grok-2-latest" : _xaiOptions.Model;

        var prompt = BuildAnalysisPrompt(username);

        var system = @"You are a brand strategist analyzing a Twitter profile. Generate a comprehensive brand description document as JSON.

Return ONLY valid JSON matching this exact structure (no markdown, no extra text):
{
  ""overview"": ""2-3 paragraphs summarizing the brand"",
  ""brandVoice"": {
    ""tone"": ""Description of overall tone"",
    ""style"": ""Description of writing style"",
    ""personality"": ""Description of personality traits""
  },
  ""targetAudience"": {
    ""primary"": ""Primary audience description"",
    ""demographics"": ""Age, profession, background"",
    ""painPoints"": [""pain point 1"", ""pain point 2"", ""pain point 3""]
  },
  ""contentPillars"": [""topic 1"", ""topic 2"", ""topic 3""],
  ""contentPatterns"": {
    ""format"": ""Format distribution"",
    ""length"": ""Typical length"",
    ""structure"": ""Common structure""
  },
  ""engagementInsights"": {
    ""topPerformingContent"": [""type 1"", ""type 2""],
    ""callToActionStyle"": ""CTA style description"",
    ""postingFrequency"": ""Frequency description""
  },
  ""uniqueDifferentiators"": [""differentiator 1"", ""differentiator 2""],
  ""recommendedStrategy"": {
    ""contentTypes"": [""type 1"", ""type 2""],
    ""toneGuidance"": ""Guidance on tone"",
    ""topicsToExplore"": [""topic 1"", ""topic 2"", ""topic 3""]
  }
}";

        var result = await _xai.CreateChatCompletionAsync(
            model,
            new List<(string Role, string Content)>
            {
                ("system", system),
                ("user", prompt)
            },
            cancellationToken);

        var brandDescription = ParseBrandDescription(result.Content);

        return new ProfileAnalysisResponseDto(
            username,
            $"https://twitter.com/{username}",
            DateTime.UtcNow,
            50, // Simulated tweet count since we can't actually fetch
            brandDescription);
    }

    private static string ValidateAndNormalizeUsername(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username is required");
        }

        // Strip @ symbol if present
        var normalized = username.TrimStart('@').Trim();

        if (normalized.Length == 0 || normalized.Length > 15)
        {
            throw new ArgumentException("Username must be 1-15 characters");
        }

        if (!UsernameRegex().IsMatch(normalized))
        {
            throw new ArgumentException("Username can only contain letters, numbers, and underscores");
        }

        return normalized;
    }

    private static string BuildAnalysisPrompt(string username)
    {
        return $@"Analyze the Twitter profile @{username} and create a comprehensive brand description.

Based on the username and what you know about typical accounts with similar names, generate a realistic and detailed brand analysis. Consider:
- What type of content would this account likely post?
- Who is their target audience?
- What topics do they cover?
- What makes them unique?

Be specific and actionable in your analysis. Create a complete brand profile that could be used for content strategy.";
    }

    private BrandDescriptionDto ParseBrandDescription(string raw)
    {
        try
        {
            // Try to extract JSON from the response
            var jsonMatch = JsonExtractRegex().Match(raw);
            var jsonStr = jsonMatch.Success ? jsonMatch.Value : raw;
            
            var parsed = JsonSerializer.Deserialize<BrandDescriptionJson>(jsonStr, JsonOptions);
            
            if (parsed == null)
            {
                throw new InvalidOperationException("Failed to parse brand description");
            }

            return new BrandDescriptionDto(
                parsed.Overview ?? "Unable to generate overview",
                new BrandVoiceDto(
                    parsed.BrandVoice?.Tone ?? "Professional",
                    parsed.BrandVoice?.Style ?? "Informative",
                    parsed.BrandVoice?.Personality ?? "Authoritative"
                ),
                new TargetAudienceDto(
                    parsed.TargetAudience?.Primary ?? "General audience",
                    parsed.TargetAudience?.Demographics ?? "Various demographics",
                    parsed.TargetAudience?.PainPoints ?? new[] { "Information seeking", "Staying updated" }
                ),
                parsed.ContentPillars ?? new[] { "General topics" },
                new ContentPatternsDto(
                    parsed.ContentPatterns?.Format ?? "Mixed content",
                    parsed.ContentPatterns?.Length ?? "Variable length",
                    parsed.ContentPatterns?.Structure ?? "Standard structure"
                ),
                new EngagementInsightsDto(
                    parsed.EngagementInsights?.TopPerformingContent ?? new[] { "Informative content" },
                    parsed.EngagementInsights?.CallToActionStyle ?? "Direct engagement",
                    parsed.EngagementInsights?.PostingFrequency ?? "Regular posting"
                ),
                parsed.UniqueDifferentiators ?? new[] { "Unique perspective" },
                new RecommendedStrategyDto(
                    parsed.RecommendedStrategy?.ContentTypes ?? new[] { "Educational content" },
                    parsed.RecommendedStrategy?.ToneGuidance ?? "Maintain authenticity",
                    parsed.RecommendedStrategy?.TopicsToExplore ?? new[] { "Related topics" }
                )
            );
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI response as JSON, using fallback");
            return CreateFallbackBrandDescription();
        }
    }

    private static BrandDescriptionDto CreateFallbackBrandDescription()
    {
        return new BrandDescriptionDto(
            "This profile appears to focus on sharing valuable content with their audience. They maintain an active presence and engage with their community regularly.",
            new BrandVoiceDto("Professional and approachable", "Clear and concise", "Helpful and informative"),
            new TargetAudienceDto(
                "Professionals and enthusiasts interested in their niche",
                "Age 25-45, digitally savvy, growth-oriented",
                ["Finding reliable information", "Building skills", "Staying current"]
            ),
            ["Industry insights", "Best practices", "Trends and updates"],
            new ContentPatternsDto(
                "Mix of threads and single tweets",
                "Concise, focused messages",
                "Hook, value, call-to-action"
            ),
            new EngagementInsightsDto(
                ["How-to content", "Industry insights", "Personal stories"],
                "Questions and direct engagement prompts",
                "Daily to weekly posting"
            ),
            ["Authentic voice", "Consistent presence"],
            new RecommendedStrategyDto(
                ["Educational threads", "Behind-the-scenes content"],
                "Stay authentic and provide value",
                ["Adjacent topics", "Collaborations", "Community building"]
            )
        );
    }

    [GeneratedRegex(@"^[a-zA-Z0-9_]+$")]
    private static partial Regex UsernameRegex();

    [GeneratedRegex(@"\{[\s\S]*\}")]
    private static partial Regex JsonExtractRegex();

    // Internal classes for JSON parsing
    private sealed class BrandDescriptionJson
    {
        public string? Overview { get; set; }
        public BrandVoiceJson? BrandVoice { get; set; }
        public TargetAudienceJson? TargetAudience { get; set; }
        public string[]? ContentPillars { get; set; }
        public ContentPatternsJson? ContentPatterns { get; set; }
        public EngagementInsightsJson? EngagementInsights { get; set; }
        public string[]? UniqueDifferentiators { get; set; }
        public RecommendedStrategyJson? RecommendedStrategy { get; set; }
    }

    private sealed class BrandVoiceJson
    {
        public string? Tone { get; set; }
        public string? Style { get; set; }
        public string? Personality { get; set; }
    }

    private sealed class TargetAudienceJson
    {
        public string? Primary { get; set; }
        public string? Demographics { get; set; }
        public string[]? PainPoints { get; set; }
    }

    private sealed class ContentPatternsJson
    {
        public string? Format { get; set; }
        public string? Length { get; set; }
        public string? Structure { get; set; }
    }

    private sealed class EngagementInsightsJson
    {
        public string[]? TopPerformingContent { get; set; }
        public string? CallToActionStyle { get; set; }
        public string? PostingFrequency { get; set; }
    }

    private sealed class RecommendedStrategyJson
    {
        public string[]? ContentTypes { get; set; }
        public string? ToneGuidance { get; set; }
        public string[]? TopicsToExplore { get; set; }
    }
}
