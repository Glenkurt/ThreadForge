using Api.Models.DTOs;

namespace Api.Services;

public interface IThreadGenerationService
{
    Task<GenerateThreadResponseDto> GenerateAsync(
        GenerateThreadRequestDto request,
        string clientId,
        CancellationToken cancellationToken);
}
