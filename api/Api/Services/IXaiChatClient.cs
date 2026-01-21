namespace Api.Services;

public interface IXaiChatClient
{
    Task<string> CreateChatCompletionAsync(
        string model,
        IReadOnlyList<(string Role, string Content)> messages,
        CancellationToken cancellationToken);
}
