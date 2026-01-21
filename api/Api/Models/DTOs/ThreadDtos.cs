using System;

namespace Api.Models.DTOs;

public sealed record GenerateThreadRequestDto(
    string Topic,
    string? Tone,
    string? Audience,
    int TweetCount,
    string[]? KeyPoints,
    string? Feedback);

public sealed record GenerateThreadResponseDto(
    Guid Id,
    string[] Tweets,
    DateTime CreatedAt,
    string Provider,
    string Model);
