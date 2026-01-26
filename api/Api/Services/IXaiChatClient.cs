namespace Api.Services;

/// <summary>
/// Result from xAI chat completion including token usage.
/// </summary>
public sealed record XaiChatCompletionResult(
    string Content,
    int? PromptTokens,
    int? CompletionTokens,
    int? TotalTokens);

public interface IXaiChatClient
{
    Task<XaiChatCompletionResult> CreateChatCompletionAsync(
        string model,
        IReadOnlyList<(string Role, string Content)> messages,
        CancellationToken cancellationToken);
}
