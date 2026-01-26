namespace Api.Models.DTOs;

/// <summary>
/// Error response returned for failed requests.
/// </summary>
public sealed record ErrorResponseDto(
    /// <summary>
    /// Human-readable error message.
    /// </summary>
    /// <example>Invalid request parameters</example>
    string Message,

    /// <summary>
    /// Detailed validation errors (if applicable).
    /// </summary>
    /// <example>["Topic is required", "TweetCount must be between 3 and 25"]</example>
    string[]? Errors = null
);
