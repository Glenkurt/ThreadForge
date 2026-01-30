namespace Api.Models.DTOs;

/// <summary>
/// Improvement type options for tweet enhancement.
/// </summary>
public static class ImprovementTypes
{
    public const string MoreEngaging = "more_engaging";
    public const string MoreConcise = "more_concise";
    public const string MoreClear = "more_clear";
    public const string MoreViral = "more_viral";
    public const string MoreProfessional = "more_professional";
    public const string MoreCasual = "more_casual";

    public static readonly string[] All =
    [
        MoreEngaging,
        MoreConcise,
        MoreClear,
        MoreViral,
        MoreProfessional,
        MoreCasual
    ];

    public static readonly Dictionary<string, string> Descriptions = new(StringComparer.OrdinalIgnoreCase)
    {
        [MoreEngaging] = "Make it more engaging and attention-grabbing with hooks, questions, or bold statements",
        [MoreConcise] = "Make it shorter and punchier while keeping the core message",
        [MoreClear] = "Make it clearer and easier to understand, simplify complex ideas",
        [MoreViral] = "Optimize for maximum shares and engagement with viral patterns",
        [MoreProfessional] = "Make it more polished and professional in tone",
        [MoreCasual] = "Make it more casual, friendly, and conversational"
    };
}

/// <summary>
/// Request to improve an existing tweet draft.
/// </summary>
public sealed record ImproveTweetRequestDto(
    /// <summary>
    /// The original tweet draft to improve (1-500 characters).
    /// </summary>
    /// <example>Just launched my new app. Check it out and let me know what you think.</example>
    string Draft,

    /// <summary>
    /// Type of improvement to apply. Options: more_engaging, more_concise, more_clear, more_viral, more_professional, more_casual.
    /// </summary>
    /// <example>more_engaging</example>
    string? ImprovementType = null,

    /// <summary>
    /// Target tone for the improved tweet.
    /// </summary>
    /// <example>indie_hacker</example>
    string? Tone = null,

    /// <summary>
    /// Key elements to preserve from the original (max 200 characters).
    /// </summary>
    /// <example>Keep the mention of the app launch</example>
    string? PreserveElements = null,

    /// <summary>
    /// Additional instructions for improvement (max 300 characters).
    /// </summary>
    /// <example>Add a question to encourage replies</example>
    string? AdditionalInstructions = null);

/// <summary>
/// Response containing the improved tweet versions.
/// </summary>
public sealed record ImproveTweetResponseDto(
    /// <summary>
    /// The original tweet draft.
    /// </summary>
    string Original,

    /// <summary>
    /// Primary improved version.
    /// </summary>
    string Improved,

    /// <summary>
    /// Alternative improved versions (2 variants).
    /// </summary>
    string[] Alternatives,

    /// <summary>
    /// Explanation of what was changed.
    /// </summary>
    string Explanation,

    /// <summary>
    /// Character count of the improved version.
    /// </summary>
    int CharacterCount,

    /// <summary>
    /// Whether the improved version is within Twitter's limit.
    /// </summary>
    bool IsWithinLimit,

    /// <summary>
    /// AI model used for generation.
    /// </summary>
    string Model);
