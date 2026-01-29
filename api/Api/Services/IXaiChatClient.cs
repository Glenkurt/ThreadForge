namespace Api.Services;

/// <summary>
/// Result from xAI chat completion including token usage.
/// </summary>
public sealed record XaiChatCompletionResult(
    string Content,
    int? PromptTokens,
    int? CompletionTokens,
    int? TotalTokens);

/// <summary>
/// Options for chat completion requests.
/// </summary>
public sealed record XaiChatOptions(
    double Temperature = 0.7,
    int? MaxTokens = null,
    bool JsonMode = true);

public interface IXaiChatClient
{
    Task<XaiChatCompletionResult> CreateChatCompletionAsync(
        string model,
        IReadOnlyList<(string Role, string Content)> messages,
        XaiChatOptions? options,
        CancellationToken cancellationToken);
}
