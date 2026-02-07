using Api.Models.DTOs;

namespace Api.Services;

public interface IWebSearchService
{
    /// <summary>
    /// Uses a lightweight LLM to generate 2-3 optimized Google search queries from a user topic.
    /// </summary>
    Task<string[]> GenerateSearchQueriesAsync(string topic, CancellationToken cancellationToken);

    /// <summary>
    /// Calls the Serper API to perform a Google search and extract organic results.
    /// </summary>
    Task<List<SerperSearchResult>> SearchAsync(string query, CancellationToken cancellationToken);

    /// <summary>
    /// Uses a lightweight LLM to synthesize raw search results into a structured research context.
    /// </summary>
    Task<string> SynthesizeAsync(string topic, List<SerperSearchResult> results, CancellationToken cancellationToken);
}
