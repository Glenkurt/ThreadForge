namespace Api.Models.DTOs;

/// <summary>
/// DTO for the global brand guideline text.
/// </summary>
public sealed record BrandGuidelineDto(
    /// <summary>
    /// Brand guideline text.
    /// </summary>
    string Text
);