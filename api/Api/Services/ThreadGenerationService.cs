using System.Text.Json;
using System.Linq;
using Api.Data;
using Api.Models.DTOs;
using Api.Models.Entities;
using Api.Models.Options;
using Microsoft.Extensions.Options;

namespace Api.Services;

public sealed class ThreadGenerationService : IThreadGenerationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _db;
    private readonly IXaiChatClient _xai;
    private readonly XaiOptions _xaiOptions;

    public ThreadGenerationService(AppDbContext db, IXaiChatClient xai, IOptions<XaiOptions> xaiOptions)
    {
        _db = db;
        _xai = xai;
        _xaiOptions = xaiOptions.Value;
    }

    public async Task<GenerateThreadResponseDto> GenerateAsync(
        GenerateThreadRequestDto request,
        string clientId,
        CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        var model = string.IsNullOrWhiteSpace(_xaiOptions.Model) ? "grok-2-latest" : _xaiOptions.Model;

        var promptJson = JsonSerializer.Serialize(request, JsonOptions);

        var system =
            "You are ThreadForge. Generate a Twitter/X thread as JSON only. " +
            "Return exactly: {\"tweets\":[\"...\",\"...\"]}. No extra keys, no markdown.";

        var user = BuildUserPrompt(request);

        // IMPORTANT: Do not log the prompt.
        var raw = await _xai.CreateChatCompletionAsync(
            model,
            new List<(string Role, string Content)>
            {
                ("system", system),
                ("user", user)
            },
            cancellationToken);

        var tweets = ExtractTweets(raw);
        tweets = EnforceTweetLength(tweets, 280).ToArray();

        var outputJson = JsonSerializer.Serialize(new { tweets }, JsonOptions);

        var draft = new ThreadDraft
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            PromptJson = promptJson,
            OutputJson = outputJson,
            Provider = "xai",
            Model = model,
            CreatedAt = DateTime.UtcNow
        };

        _db.ThreadDrafts.Add(draft);
        await _db.SaveChangesAsync(cancellationToken);

        return new GenerateThreadResponseDto(
            draft.Id,
            tweets,
            draft.CreatedAt,
            draft.Provider,
            draft.Model);
    }

    private static void ValidateRequest(GenerateThreadRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Topic))
        {
            throw new ArgumentException("Topic is required");
        }

        if (request.Topic.Length > 500)
        {
            throw new ArgumentException("Topic is too long");
        }

        if (request.TweetCount is < 3 or > 25)
        {
            throw new ArgumentException("TweetCount must be between 3 and 25");
        }

        if (request.KeyPoints is { Length: > 20 })
        {
            throw new ArgumentException("Too many key points");
        }

        if (request.Feedback is { Length: > 1000 })
        {
            throw new ArgumentException("Feedback is too long");
        }
    }

    private static string BuildUserPrompt(GenerateThreadRequestDto request)
    {
        var tone = string.IsNullOrWhiteSpace(request.Tone) ? "clear, practical" : request.Tone;
        var audience = string.IsNullOrWhiteSpace(request.Audience) ? "builders" : request.Audience;
        var keyPoints = request.KeyPoints is { Length: > 0 }
            ? "\nKey points:\n- " + string.Join("\n- ", request.KeyPoints)
            : string.Empty;

        var feedback = string.IsNullOrWhiteSpace(request.Feedback)
            ? string.Empty
            : $"\nRegeneration feedback: {request.Feedback}";

        return
            $"Topic: {request.Topic}\n" +
            $"Audience: {audience}\n" +
            $"Tone: {tone}\n" +
            $"Tweet count: {request.TweetCount}\n" +
            keyPoints +
            feedback +
            "\n\nRules: Each tweet must be <= 280 characters. Include a strong hook in tweet 1 and a concise CTA in the last tweet.";
    }

    private static string[] ExtractTweets(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return ["(No output)"];
        }

        // Try strict JSON first
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("tweets", out var tweetsEl) && tweetsEl.ValueKind == JsonValueKind.Array)
            {
                return tweetsEl.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => (e.GetString() ?? string.Empty).Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToArray();
            }
        }
        catch
        {
            // ignore and fallback
        }

        // Fallback: split by lines and strip numbering
        return raw
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Select(l => l.TrimStart('-', '•', '*', ' '))
            .Select(l => l.StartsWith("#") ? l : l)
            .Select(l => StripLeadingNumbering(l))
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToArray();
    }

    private static string StripLeadingNumbering(string line)
    {
        // e.g. "1) foo", "2. bar", "3 - baz"
        var i = 0;
        while (i < line.Length && char.IsDigit(line[i])) i++;
        if (i == 0) return line;

        while (i < line.Length && (line[i] == '.' || line[i] == ')' || line[i] == '-' || char.IsWhiteSpace(line[i]))) i++;
        return i < line.Length ? line[i..].Trim() : string.Empty;
    }

    private static IEnumerable<string> EnforceTweetLength(IEnumerable<string> tweets, int maxLen)
    {
        foreach (var tweet in tweets)
        {
            if (tweet.Length <= maxLen)
            {
                yield return tweet;
                continue;
            }

            foreach (var part in SplitToMaxLength(tweet, maxLen))
            {
                yield return part;
            }
        }
    }

    private static IEnumerable<string> SplitToMaxLength(string text, int maxLen)
    {
        var remaining = text.Trim();

        while (remaining.Length > maxLen)
        {
            var cut = remaining.LastIndexOf(' ', maxLen);
            if (cut < maxLen * 0.6)
            {
                cut = maxLen;
            }

            var part = remaining[..cut].Trim();
            if (!string.IsNullOrWhiteSpace(part))
            {
                yield return part;
            }

            remaining = remaining[cut..].Trim();
        }

        if (!string.IsNullOrWhiteSpace(remaining))
        {
            yield return remaining;
        }
    }
}
