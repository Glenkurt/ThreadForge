namespace Api.Models.DTOs;

/// <summary>
/// A single organic search result from Serper API.
/// </summary>
public sealed record SerperSearchResult(
    string Title,
    string Snippet,
    string Link);

