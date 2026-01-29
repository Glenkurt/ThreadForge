namespace Api.Services;

/// <summary>
/// Quality analysis report for a generated thread.
/// </summary>
public sealed record ThreadQualityReport(
    int HookScore,
    int CtaScore,
    int OverallScore,
    string[] Warnings,
    string[] Suggestions);

/// <summary>
/// Service for analyzing thread quality and providing improvement suggestions.
/// </summary>
public interface IThreadQualityService
{
    /// <summary>
    /// Analyzes a generated thread and returns quality scores and suggestions.
    /// </summary>
    ThreadQualityReport Analyze(string[] tweets, string? tone);
}
