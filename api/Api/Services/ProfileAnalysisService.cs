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

        var profileBio = ValidateProfileBio(request.ProfileBio);
        var recentTweets = ValidateRecentTweets(request.RecentTweets);

        var model = string.IsNullOrWhiteSpace(_xaiOptions.Model) ? "grok-2-latest" : _xaiOptions.Model;

        var prompt = BuildAnalysisPrompt(username, profileBio, recentTweets);

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
            $"https://x.com/{username}",
            DateTime.UtcNow,
            recentTweets.Length,
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

    private static string ValidateProfileBio(string? profileBio)
    {
        var trimmed = profileBio?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new ArgumentException("Please paste the profile bio");
        }

        if (trimmed.Length > 400)
        {
            throw new ArgumentException("Profile bio must not exceed 400 characters");
        }

        return trimmed;
    }

    private static string[] ValidateRecentTweets(string[]? recentTweets)
    {
        if (recentTweets is null || recentTweets.Length == 0)
        {
            throw new ArgumentException("Please paste at least 5 recent tweets");
        }

        var cleaned = recentTweets
            .Select(t => t?.Trim() ?? string.Empty)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToArray();

        if (cleaned.Length < 5)
        {
            throw new ArgumentException("Please paste at least 5 recent tweets");
        }

        if (cleaned.Length > 30)
        {
            throw new ArgumentException("Please paste no more than 30 tweets");
        }

        foreach (var tweet in cleaned)
        {
            if (tweet.Length > 500)
            {
                throw new ArgumentException("Each tweet must not exceed 500 characters");
            }
        }

        return cleaned;
    }

    private static string BuildAnalysisPrompt(string username, string profileBio, string[] recentTweets)
    {
        var tweetsBlock = string.Join("\n", recentTweets.Select(t => $"- {t}"));

        return $@"Analyze the X profile @{username} and create a comprehensive brand description based ONLY on the provided bio and tweets.

Do NOT invent or assume any facts (follower count, engagement, posting frequency, etc.). If information is missing, state it as unknown.

Profile bio:
{profileBio}

Recent tweets (use these as the sole source of truth):
{tweetsBlock}

Return a complete brand profile that could be used for content strategy.";
    }

    private BrandDescriptionDto ParseBrandDescription(string raw)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<BrandDescriptionJson>(raw, JsonOptions);
            
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
            _logger.LogWarning(ex, "Failed to parse AI response as JSON");
            throw new InvalidOperationException("Brand analysis failed. Try again.");
        }
    }

    [GeneratedRegex(@"^[a-zA-Z0-9_]+$")]
    private static partial Regex UsernameRegex();


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
