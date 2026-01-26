using Api.Models.DTOs;

namespace Api.Services;

public interface IProfileAnalysisService
{
    Task<ProfileAnalysisResponseDto> AnalyzeAsync(
        ProfileAnalysisRequestDto request,
        string clientId,
        CancellationToken cancellationToken);
}
