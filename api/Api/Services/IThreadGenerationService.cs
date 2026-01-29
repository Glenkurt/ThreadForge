using Api.Models.DTOs;

namespace Api.Services;

public interface IThreadGenerationService
{
    Task<GenerateThreadResponseDto> GenerateAsync(
        GenerateThreadRequestDto request,
        string clientId,
        CancellationToken cancellationToken);

    Task<RegenerateTweetResponseDto> RegenerateSingleTweetAsync(
        string[] existingTweets,
        int tweetIndex,
        string? feedback,
        string? tone,
        int maxChars,
        CancellationToken cancellationToken);
}
