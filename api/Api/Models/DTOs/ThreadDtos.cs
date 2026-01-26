using System;

namespace Api.Models.DTOs;

/// <summary>
/// Style preferences for fine-grained control over thread formatting.
/// </summary>
public sealed record StylePreferencesDto(
    /// <summary>
    /// Whether to use emojis. Null means AI decides.
    /// </summary>
    /// <example>true</example>
    bool? UseEmojis = null,

    /// <summary>
    /// Whether to include X/N numbering at the end of each tweet.
    /// </summary>
    /// <example>true</example>
    bool? UseNumbering = true,

    /// <summary>
    /// Maximum characters per tweet (200-280). Default is 260.
    /// </summary>
    /// <example>260</example>
    int? MaxCharsPerTweet = 260,

    /// <summary>
    /// Style for the first tweet hook: bold, question, story, or stat.
    /// </summary>
    /// <example>bold</example>
    string? HookStrength = null,

    /// <summary>
    /// Style for the final CTA: soft, direct, or question.
    /// </summary>
    /// <example>direct</example>
    string? CtaType = null);

/// <summary>
/// Request parameters for thread generation.
/// </summary>
public sealed record GenerateThreadRequestDto(
    /// <summary>
    /// Main subject for the Twitter thread (1-500 characters).
    /// </summary>
    /// <example>How to build in public as an indie hacker</example>
    string Topic,

    /// <summary>
    /// Writing style for the thread. Supported values: indie_hacker, professional, humorous, motivational, educational, provocative, storytelling, clear_practical.
    /// </summary>
    /// <example>indie_hacker</example>
    string? Tone,

    /// <summary>
    /// Target audience description (max 100 characters).
    /// </summary>
    /// <example>Technical founders building SaaS products</example>
    string? Audience,

    /// <summary>
    /// Number of tweets to generate (3-25).
    /// </summary>
    /// <example>5</example>
    int TweetCount,

    /// <summary>
    /// Specific points to include in the thread (max 20 items).
    /// </summary>
    /// <example>["Share revenue numbers", "Be authentic", "Post daily"]</example>
    string[]? KeyPoints,

    /// <summary>
    /// Feedback for regenerating a previous thread (max 1000 characters).
    /// </summary>
    /// <example>Make it more actionable and less theoretical</example>
    string? Feedback,

    /// <summary>
    /// Brand voice description, dos/don'ts, favorite phrases, vocabulary (max 1500 characters).
    /// </summary>
    /// <example>Use casual, first-person voice. Avoid jargon. Favorite phrases: 'here's the thing', 'let me explain'.</example>
    string? BrandGuidelines = null,

    /// <summary>
    /// Example threads for few-shot prompting to match style (max 3 items, each max 5000 characters).
    /// </summary>
    /// <example>["1/ Building in public changed everything for me...\n\n2/ Here's why...", "1/ The #1 mistake I see founders make..."]</example>
    string[]? ExampleThreads = null,

    /// <summary>
    /// Fine-grained style preferences for formatting.
    /// </summary>
    StylePreferencesDto? StylePreferences = null);

/// <summary>
/// Response containing the generated Twitter thread.
/// </summary>
public sealed record GenerateThreadResponseDto(
    /// <summary>
    /// Unique identifier for the generated thread.
    /// </summary>
    Guid Id,

    /// <summary>
    /// Array of generated tweets, each up to 280 characters.
    /// </summary>
    string[] Tweets,

    /// <summary>
    /// UTC timestamp when the thread was generated.
    /// </summary>
    DateTime CreatedAt,

    /// <summary>
    /// AI provider used for generation (e.g., "xai").
    /// </summary>
    string Provider,

    /// <summary>
    /// AI model used for generation (e.g., "grok-2-latest").
    /// </summary>
    string Model);
