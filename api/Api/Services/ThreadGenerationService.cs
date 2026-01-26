using System.Text.Json;
using System.Linq;
using System.Text;
using Api.Data;
using Api.Models.DTOs;
using Api.Models.Entities;
using Api.Models.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Api.Services;

public sealed class ThreadGenerationService : IThreadGenerationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // Tone expansion mapping - short codes to full descriptions
    private static readonly Dictionary<string, string> ToneDescriptions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["indie_hacker"] = "Casual, transparent, no-BS voice. Use first-person, share real numbers and failures, motivational but realistic. Favorite phrases: 'here's what actually happened', 'shipped this in a weekend', 'MRR update'.",
        ["professional"] = "Clear, structured, authoritative but approachable. Use bullet-style lists inside tweets, data-driven examples.",
        ["humorous"] = "Witty, sarcastic when appropriate, internet-native humor. Use meme references lightly.",
        ["motivational"] = "Inspiring, energetic, lots of questions to the reader. Encourage action and positivity.",
        ["educational"] = "Teacher-like, step-by-step explanations, uses analogies and examples to clarify concepts.",
        ["provocative"] = "Bold, contrarian, challenges conventional wisdom. Uses strong statements to spark engagement.",
        ["storytelling"] = "Narrative-driven, uses personal anecdotes, builds tension and resolution across tweets.",
        ["clear_practical"] = "Straightforward, actionable, step-by-step. Focus on practical advice over theory."
    };

    private static readonly string[] ValidHookStrengths = ["bold", "question", "story", "stat"];
    private static readonly string[] ValidCtaTypes = ["soft", "direct", "question"];

    private readonly AppDbContext _db;
    private readonly IXaiChatClient _xai;
    private readonly XaiOptions _xaiOptions;
    private readonly ILogger<ThreadGenerationService> _logger;

    public ThreadGenerationService(
        AppDbContext db,
        IXaiChatClient xai,
        IOptions<XaiOptions> xaiOptions,
        ILogger<ThreadGenerationService> logger)
    {
        _db = db;
        _xai = xai;
        _xaiOptions = xaiOptions.Value;
        _logger = logger;
    }

    public async Task<GenerateThreadResponseDto> GenerateAsync(
        GenerateThreadRequestDto request,
        string clientId,
        CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        var model = string.IsNullOrWhiteSpace(_xaiOptions.Model) ? "grok-2-latest" : _xaiOptions.Model;
        var maxChars = request.StylePreferences?.MaxCharsPerTweet ?? 260;

        var promptJson = JsonSerializer.Serialize(request, JsonOptions);

        var system = BuildSystemPrompt();
        var user = BuildUserPrompt(request, maxChars);

        var result = await _xai.CreateChatCompletionAsync(
            model,
            new List<(string Role, string Content)>
            {
                ("system", system),
                ("user", user)
            },
            cancellationToken);

        // Log token usage
        if (result.TotalTokens.HasValue)
        {
            _logger.LogInformation(
                "Thread generation completed. Tokens: prompt={PromptTokens}, completion={CompletionTokens}, total={TotalTokens}",
                result.PromptTokens,
                result.CompletionTokens,
                result.TotalTokens);
        }
        else
        {
            _logger.LogDebug("Thread generation completed. Token usage not available.");
        }

        var tweets = ExtractTweets(result.Content);
        tweets = EnforceTweetLength(tweets, maxChars).ToArray();

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
            throw new ArgumentException("Feedback must not exceed 1000 characters");
        }

        if (request.Tone is { Length: > 100 })
        {
            throw new ArgumentException("Tone must not exceed 100 characters");
        }

        if (request.BrandGuidelines is { Length: > 1500 })
        {
            throw new ArgumentException("Brand guidelines must not exceed 1500 characters");
        }

        if (request.ExampleThreads is { Length: > 3 })
        {
            throw new ArgumentException("Example threads must not exceed 3 items");
        }

        if (request.ExampleThreads is not null)
        {
            foreach (var example in request.ExampleThreads)
            {
                if (example?.Length > 5000)
                {
                    throw new ArgumentException("Each example thread must not exceed 5000 characters");
                }
            }
        }

        if (request.StylePreferences is not null)
        {
            var prefs = request.StylePreferences;

            if (prefs.MaxCharsPerTweet.HasValue)
            {
                if (prefs.MaxCharsPerTweet < 200)
                {
                    throw new ArgumentException("Max characters per tweet must be at least 200");
                }
                if (prefs.MaxCharsPerTweet > 280)
                {
                    throw new ArgumentException("Max characters per tweet must not exceed 280");
                }
            }

            if (!string.IsNullOrWhiteSpace(prefs.HookStrength) &&
                !ValidHookStrengths.Contains(prefs.HookStrength, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Hook strength must be one of: bold, question, story, stat");
            }

            if (!string.IsNullOrWhiteSpace(prefs.CtaType) &&
                !ValidCtaTypes.Contains(prefs.CtaType, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException("CTA type must be one of: soft, direct, question");
            }
        }
    }

    private static string BuildSystemPrompt()
    {
        return """
            You are ThreadForge, an expert X/Twitter thread writer. Your only job is to output strictly valid JSON in the exact format: {"tweets":["tweet1","tweet2",...]} with no extra keys, no markdown, no explanations, no numbering outside the tweet text unless specifically requested.

            Rules you must follow:
            - Every tweet must be ≤ the specified max characters (default 260).
            - Tweet 1 must have a strong, attention-grabbing hook.
            - Last tweet must end with a clear, concise call-to-action.
            - Tweets must connect smoothly and read as a natural thread.
            - Cover all provided key points comprehensively.
            - Follow any brand guidelines, style preferences, and examples exactly.
            - Use natural line breaks within tweets for readability.
            - Be conversational, authentic, and valuable to the reader.
            """;
    }

    private static string BuildUserPrompt(GenerateThreadRequestDto request, int maxChars)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Write an engaging X/Twitter thread.");
        sb.AppendLine();

        // Topic
        sb.AppendLine($"Topic: {request.Topic}");
        sb.AppendLine();

        // Audience
        var audience = string.IsNullOrWhiteSpace(request.Audience) ? "builders and makers" : request.Audience;
        sb.AppendLine($"Audience: {audience}");
        sb.AppendLine();

        // Tone - expand to full description
        var toneDescription = GetToneDescription(request.Tone);
        sb.AppendLine($"Tone: {toneDescription}");
        sb.AppendLine();

        // Tweet count
        sb.AppendLine($"Tweet count: {request.TweetCount}");
        sb.AppendLine();

        // Key points
        if (request.KeyPoints is { Length: > 0 })
        {
            sb.AppendLine("Key points to cover (distribute naturally across the thread):");
            foreach (var point in request.KeyPoints)
            {
                if (!string.IsNullOrWhiteSpace(point))
                {
                    sb.AppendLine($"- {point}");
                }
            }
            sb.AppendLine();
        }

        // Brand guidelines
        if (!string.IsNullOrWhiteSpace(request.BrandGuidelines))
        {
            sb.AppendLine("Brand guidelines (follow strictly):");
            sb.AppendLine(request.BrandGuidelines);
            sb.AppendLine();
        }

        // Example threads
        if (request.ExampleThreads is { Length: > 0 })
        {
            sb.AppendLine("Learn from these example threads (match style, voice, structure, and formatting closely):");
            for (var i = 0; i < request.ExampleThreads.Length; i++)
            {
                var example = request.ExampleThreads[i];
                if (!string.IsNullOrWhiteSpace(example))
                {
                    sb.AppendLine($"Example {i + 1}:");
                    sb.AppendLine(example);
                    sb.AppendLine();
                }
            }
        }

        // Style preferences
        sb.AppendLine("Style preferences:");
        var prefs = request.StylePreferences;

        // Numbering
        var useNumbering = prefs?.UseNumbering ?? true;
        sb.AppendLine(useNumbering
            ? $"- Thread numbering: Include X/{request.TweetCount} at the end of each tweet"
            : "- Thread numbering: Do not number tweets");

        // Emojis
        if (prefs?.UseEmojis == true)
        {
            sb.AppendLine("- Emojis: Use relevant emojis sparingly to enhance engagement");
        }
        else if (prefs?.UseEmojis == false)
        {
            sb.AppendLine("- Emojis: No emojis");
        }
        else
        {
            sb.AppendLine("- Emojis: Use emojis at your discretion");
        }

        // Max chars
        sb.AppendLine($"- Max chars per tweet: {maxChars}");

        // Hook strength
        var hookStrength = string.IsNullOrWhiteSpace(prefs?.HookStrength)
            ? "Strong hook"
            : $"{Capitalize(prefs.HookStrength)} style hook";
        sb.AppendLine($"- First tweet hook: {hookStrength}");

        // CTA type
        var ctaType = string.IsNullOrWhiteSpace(prefs?.CtaType)
            ? "Clear call to action"
            : $"{Capitalize(prefs.CtaType)} call to action";
        sb.AppendLine($"- Final CTA: {ctaType}");
        sb.AppendLine();

        // Feedback
        if (!string.IsNullOrWhiteSpace(request.Feedback))
        {
            sb.AppendLine("Previous feedback / regeneration instructions:");
            sb.AppendLine(request.Feedback);
            sb.AppendLine();
        }

        // Universal rules
        sb.AppendLine("Universal rules:");
        sb.AppendLine($"- Keep each tweet under {maxChars} characters and highly readable.");
        sb.AppendLine("- Use short paragraphs and line breaks within tweets.");
        sb.AppendLine("- Make it conversational, authentic, and valuable.");
        sb.AppendLine("- End the last tweet with the CTA.");

        return sb.ToString();
    }

    private static string GetToneDescription(string? tone)
    {
        if (string.IsNullOrWhiteSpace(tone))
        {
            return ToneDescriptions["clear_practical"];
        }

        return ToneDescriptions.TryGetValue(tone, out var description)
            ? description
            : tone; // Pass through custom tones as-is
    }

    private static string Capitalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        return char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
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

            var parts = SplitToMaxLength(tweet, maxLen).ToList();
            for (var i = 0; i < parts.Count; i++)
            {
                var part = parts[i];
                // Add continuity marker to all parts except the last
                if (i < parts.Count - 1)
                {
                    yield return part + " 🧵";
                }
                else
                {
                    yield return part;
                }
            }
        }
    }

    private static IEnumerable<string> SplitToMaxLength(string text, int maxLen)
    {
        // Reserve 2 chars for " 🧵" marker on non-final parts
        var effectiveMax = maxLen - 2;
        var remaining = text.Trim();

        while (remaining.Length > maxLen)
        {
            var cut = remaining.LastIndexOf(' ', effectiveMax);
            if (cut < effectiveMax * 0.6)
            {
                cut = effectiveMax;
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
