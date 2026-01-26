namespace Api.Models.DTOs;

/// <summary>
/// Request for analyzing a Twitter profile.
/// </summary>
public sealed record ProfileAnalysisRequestDto(
    /// <summary>
    /// Twitter username (with or without @ symbol, 1-15 characters).
    /// </summary>
    /// <example>threadforge</example>
    string Username
);

/// <summary>
/// Response containing the analyzed Twitter profile brand description.
/// </summary>
public sealed record ProfileAnalysisResponseDto(
    /// <summary>
    /// Analyzed Twitter username (without @ symbol).
    /// </summary>
    string Username,

    /// <summary>
    /// Full Twitter profile URL.
    /// </summary>
    string ProfileUrl,

    /// <summary>
    /// UTC timestamp of the analysis.
    /// </summary>
    DateTime AnalyzedAt,

    /// <summary>
    /// Number of tweets analyzed.
    /// </summary>
    int TweetCount,

    /// <summary>
    /// Comprehensive brand description document.
    /// </summary>
    BrandDescriptionDto BrandDescription
);

/// <summary>
/// Complete brand description generated from profile analysis.
/// </summary>
public sealed record BrandDescriptionDto(
    /// <summary>
    /// 2-3 paragraph overview of the brand.
    /// </summary>
    string Overview,

    /// <summary>
    /// Brand voice characteristics.
    /// </summary>
    BrandVoiceDto BrandVoice,

    /// <summary>
    /// Target audience information.
    /// </summary>
    TargetAudienceDto TargetAudience,

    /// <summary>
    /// Main topics the account posts about (3-5 items).
    /// </summary>
    string[] ContentPillars,

    /// <summary>
    /// Content format and structure patterns.
    /// </summary>
    ContentPatternsDto ContentPatterns,

    /// <summary>
    /// Engagement and performance insights.
    /// </summary>
    EngagementInsightsDto EngagementInsights,

    /// <summary>
    /// Unique differentiators that make this account stand out.
    /// </summary>
    string[] UniqueDifferentiators,

    /// <summary>
    /// Recommended content strategy.
    /// </summary>
    RecommendedStrategyDto RecommendedStrategy
);

/// <summary>
/// Brand voice characteristics.
/// </summary>
public sealed record BrandVoiceDto(
    /// <summary>
    /// Overall tone (e.g., "Professional yet approachable").
    /// </summary>
    string Tone,

    /// <summary>
    /// Writing style (e.g., "Direct, actionable, data-driven").
    /// </summary>
    string Style,

    /// <summary>
    /// Personality traits (e.g., "Helpful expert, builds trust").
    /// </summary>
    string Personality
);

/// <summary>
/// Target audience information.
/// </summary>
public sealed record TargetAudienceDto(
    /// <summary>
    /// Primary audience description.
    /// </summary>
    string Primary,

    /// <summary>
    /// Demographic information.
    /// </summary>
    string Demographics,

    /// <summary>
    /// Pain points the audience has (3-5 items).
    /// </summary>
    string[] PainPoints
);

/// <summary>
/// Content format and structure patterns.
/// </summary>
public sealed record ContentPatternsDto(
    /// <summary>
    /// Content format distribution (threads, tweets, polls).
    /// </summary>
    string Format,

    /// <summary>
    /// Typical content length.
    /// </summary>
    string Length,

    /// <summary>
    /// Common structure or formula in content.
    /// </summary>
    string Structure
);

/// <summary>
/// Engagement and performance insights.
/// </summary>
public sealed record EngagementInsightsDto(
    /// <summary>
    /// Types of content that perform best.
    /// </summary>
    string[] TopPerformingContent,

    /// <summary>
    /// Call-to-action style used.
    /// </summary>
    string CallToActionStyle,

    /// <summary>
    /// Posting frequency.
    /// </summary>
    string PostingFrequency
);

/// <summary>
/// Recommended content strategy.
/// </summary>
public sealed record RecommendedStrategyDto(
    /// <summary>
    /// Recommended content types based on brand.
    /// </summary>
    string[] ContentTypes,

    /// <summary>
    /// Guidance on maintaining brand voice.
    /// </summary>
    string ToneGuidance,

    /// <summary>
    /// Related topics to explore.
    /// </summary>
    string[] TopicsToExplore
);
