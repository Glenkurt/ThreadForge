using Api.Models.DTOs;

namespace Api.Services;

/// <summary>
/// Service for improving existing tweet drafts using AI.
/// </summary>
public interface ITweetImproverService
{
    /// <summary>
    /// Improves an existing tweet draft based on the specified criteria.
    /// </summary>
    /// <param name="request">The improvement request containing the draft and options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The improved tweet with alternatives and explanation.</returns>
    Task<ImproveTweetResponseDto> ImproveAsync(
        ImproveTweetRequestDto request,
        CancellationToken cancellationToken);
}
