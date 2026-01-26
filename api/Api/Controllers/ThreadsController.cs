using System.Text.Json;
using Api.Data;
using Api.Models.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

/// <summary>
/// Controller for AI-powered Twitter thread generation.
/// </summary>
[ApiController]
[Route("api/v1/threads")]
public sealed class ThreadsController : ControllerBase
{
    private readonly IThreadGenerationService _threadGeneration;
    private readonly AppDbContext _db;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ThreadsController(IThreadGenerationService threadGeneration, AppDbContext db)
    {
        _threadGeneration = threadGeneration;
        _db = db;
    }

    /// <summary>
    /// Generate an AI-powered Twitter thread based on topic and parameters.
    /// </summary>
    /// <param name="request">Thread generation parameters including topic, tone, and tweet count.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Generated thread with tweets and metadata.</returns>
    /// <response code="200">Thread generated successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="429">Rate limit exceeded.</response>
    /// <response code="500">Thread generation failed.</response>
    // MVP: unauthenticated endpoint; client is identified by X-Client-Id
    [HttpPost("generate")]
    [EnableRateLimiting("threadgen")]
    [ProducesResponseType(typeof(GenerateThreadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GenerateThreadResponseDto>> Generate(
        [FromBody] GenerateThreadRequestDto request,
        CancellationToken cancellationToken)
    {
        var clientId = Request.Headers["X-Client-Id"].ToString();
        if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128)
        {
            clientId = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        try
        {
            var result = await _threadGeneration.GenerateAsync(request, clientId, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto(ex.Message));
        }
    }

    /// <summary>
    /// List previously generated threads (global history for MVP).
    /// </summary>
    /// <param name="limit">Maximum number of items to return (default 20, max 100).</param>
    /// <param name="offset">Number of items to skip (default 0).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("history")]
    [ProducesResponseType(typeof(ThreadHistoryListItemDto[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ThreadHistoryListItemDto[]>> GetHistory(
        [FromQuery] int? limit,
        [FromQuery] int? offset,
        CancellationToken cancellationToken)
    {
        var resolvedLimit = limit ?? 20;
        var resolvedOffset = offset ?? 0;

        if (resolvedLimit < 1)
        {
            return BadRequest(new ErrorResponseDto("Limit must be at least 1"));
        }

        if (resolvedLimit > 100)
        {
            return BadRequest(new ErrorResponseDto("Limit must not exceed 100"));
        }

        if (resolvedOffset < 0)
        {
            return BadRequest(new ErrorResponseDto("Offset must be 0 or greater"));
        }

        var drafts = await _db.ThreadDrafts
            .AsNoTracking()
            .OrderByDescending(d => d.CreatedAt)
            .Skip(resolvedOffset)
            .Take(resolvedLimit)
            .ToListAsync(cancellationToken);

        var items = drafts
            .Select(d =>
            {
                var topic = ExtractStringProperty(d.PromptJson, "topic");
                var tweets = ExtractTweetsArray(d.OutputJson);

                var topicPreview = TruncateWithEllipsis(topic, 120);
                var firstTweetPreview = TruncateWithEllipsis(tweets.FirstOrDefault() ?? string.Empty, 120);

                return new ThreadHistoryListItemDto(
                    d.Id,
                    d.CreatedAt,
                    topicPreview,
                    tweets.Length,
                    firstTweetPreview,
                    d.Provider,
                    d.Model);
            })
            .ToArray();

        return Ok(items);
    }

    /// <summary>
    /// Get a previously generated thread by id (global history for MVP).
    /// </summary>
    /// <param name="id">Thread id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("history/{id:guid}")]
    [ProducesResponseType(typeof(ThreadHistoryDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ThreadHistoryDetailDto>> GetHistoryById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var draft = await _db.ThreadDrafts
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (draft is null)
        {
            return NotFound(new ErrorResponseDto("Thread not found"));
        }

        var requestElement = ParseJsonElement(draft.PromptJson);
        var tweets = ExtractTweetsArray(draft.OutputJson);

        return Ok(new ThreadHistoryDetailDto(
            draft.Id,
            draft.CreatedAt,
            requestElement,
            tweets,
            draft.Provider,
            draft.Model));
    }

    private static string ExtractStringProperty(string json, string propertyName)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return string.Empty;
            }

            if (doc.RootElement.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
            {
                return prop.GetString() ?? string.Empty;
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string[] ExtractTweetsArray(string outputJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(outputJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return Array.Empty<string>();
            }

            if (!doc.RootElement.TryGetProperty("tweets", out var tweetsElement) || tweetsElement.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<string>();
            }

            return tweetsElement
                .EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString() ?? string.Empty)
                .ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static JsonElement ParseJsonElement(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }
        catch
        {
            return JsonSerializer.Deserialize<JsonElement>("{}", JsonOptions);
        }
    }

    private static string TruncateWithEllipsis(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || maxLength <= 0)
        {
            return string.Empty;
        }

        if (value.Length <= maxLength)
        {
            return value;
        }

        return value.Substring(0, maxLength) + "…";
    }
}
