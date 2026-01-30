using System.Text;
using System.Text.Json;
using Api.Models.DTOs;
using Api.Models.Options;
using Microsoft.Extensions.Options;

namespace Api.Services;

public sealed class TweetImproverService : ITweetImproverService
{
    private const int TwitterCharLimit = 280;
    private const int MaxDraftLength = 500;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // Tone descriptions for consistent voice
    private static readonly Dictionary<string, string> ToneDescriptions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["indie_hacker"] = "Casual, transparent, no-BS voice. First-person, share real experiences, motivational but realistic.",
        ["professional"] = "Clear, structured, authoritative but approachable. Polished language.",
        ["humorous"] = "Witty, playful, internet-native humor. Light sarcasm when appropriate.",
        ["motivational"] = "Inspiring, energetic, encouraging action and positivity.",
        ["educational"] = "Teacher-like, clear explanations, helpful and informative.",
        ["provocative"] = "Bold, contrarian, challenges conventional wisdom with strong statements.",
        ["storytelling"] = "Narrative-driven, uses personal anecdotes, builds intrigue.",
        ["clear_practical"] = "Straightforward, actionable, focuses on practical value."
    };

    // Improvement type prompts
    private static readonly Dictionary<string, string> ImprovementPrompts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["more_engaging"] = @"Make this tweet IMPOSSIBLE to scroll past:
- Add a hook that creates curiosity or tension
- Use 'you' to speak directly to the reader
- Include a question, bold claim, or surprising angle
- Make people WANT to engage (like, reply, retweet)",

        ["more_concise"] = @"Make this tweet PUNCHY and TIGHT:
- Cut every unnecessary word ruthlessly
- Remove filler words: very, really, just, actually, basically, that
- Use short sentences that hit hard
- One clear idea, maximum impact in minimum words",

        ["more_clear"] = @"Make this tweet CRYSTAL CLEAR:
- Simplify complex language
- Use concrete examples instead of abstractions
- Structure the message logically
- Anyone should understand this instantly",

        ["more_viral"] = @"Optimize this tweet for MAXIMUM VIRALITY:
- Use proven viral patterns (curiosity gap, contrarian take, specific numbers)
- Make it highly shareable and quotable
- Create an emotional reaction (surprise, agreement, inspiration)
- Add elements that encourage replies and discussion",

        ["more_professional"] = @"Make this tweet POLISHED and PROFESSIONAL:
- Use proper grammar and clear structure
- Remove slang and overly casual language
- Sound authoritative and credible
- Maintain a confident but approachable tone",

        ["more_casual"] = @"Make this tweet CONVERSATIONAL and FRIENDLY:
- Sound like you're talking to a friend
- Use natural, everyday language
- Add personality and warmth
- Keep it relatable and down-to-earth"
    };

    private readonly IXaiChatClient _xai;
    private readonly XaiOptions _xaiOptions;
    private readonly ILogger<TweetImproverService> _logger;

    public TweetImproverService(
        IXaiChatClient xai,
        IOptions<XaiOptions> xaiOptions,
        ILogger<TweetImproverService> logger)
    {
        _xai = xai;
        _xaiOptions = xaiOptions.Value;
        _logger = logger;
    }

    public async Task<ImproveTweetResponseDto> ImproveAsync(
        ImproveTweetRequestDto request,
        CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        var model = string.IsNullOrWhiteSpace(_xaiOptions.Model) ? "grok-2-latest" : _xaiOptions.Model;

        var systemPrompt = BuildSystemPrompt();
        var userPrompt = BuildUserPrompt(request);

        // Use moderate temperature for balanced creativity
        var temperature = 0.7;

        var result = await _xai.CreateChatCompletionAsync(
            model,
            new List<(string Role, string Content)>
            {
                ("system", systemPrompt),
                ("user", userPrompt)
            },
            new XaiChatOptions(Temperature: temperature),
            cancellationToken);

        // Log token usage
        if (result.TotalTokens.HasValue)
        {
            _logger.LogInformation(
                "Tweet improvement completed. Tokens: prompt={PromptTokens}, completion={CompletionTokens}, total={TotalTokens}",
                result.PromptTokens,
                result.CompletionTokens,
                result.TotalTokens);
        }

        var parsed = ParseResponse(result.Content, request.Draft);

        _logger.LogInformation(
            "Tweet improved: original_chars={OriginalChars}, improved_chars={ImprovedChars}, within_limit={WithinLimit}",
            request.Draft.Length,
            parsed.CharacterCount,
            parsed.IsWithinLimit);

        return parsed with { Model = model };
    }

    private static void ValidateRequest(ImproveTweetRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Draft))
        {
            throw new ArgumentException("Draft is required");
        }

        if (request.Draft.Length > MaxDraftLength)
        {
            throw new ArgumentException($"Draft must not exceed {MaxDraftLength} characters");
        }

        if (!string.IsNullOrWhiteSpace(request.ImprovementType) &&
            !ImprovementTypes.All.Contains(request.ImprovementType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Improvement type must be one of: {string.Join(", ", ImprovementTypes.All)}");
        }

        if (request.PreserveElements is { Length: > 200 })
        {
            throw new ArgumentException("Preserve elements must not exceed 200 characters");
        }

        if (request.AdditionalInstructions is { Length: > 300 })
        {
            throw new ArgumentException("Additional instructions must not exceed 300 characters");
        }
    }

    private static string BuildSystemPrompt()
    {
        return """
            You are TweetForge, an expert tweet writer who transforms mediocre drafts into scroll-stopping tweets.

            Your expertise:
            - You understand what makes people stop scrolling on Twitter/X
            - You know viral patterns: curiosity gaps, contrarian takes, specific numbers, story hooks
            - You write concise, punchy copy that fits the 280 character limit
            - You preserve the original intent while dramatically improving the delivery

            OUTPUT FORMAT: Return ONLY valid JSON:
            {
                "improved": "The primary improved version of the tweet",
                "alternatives": ["Alternative version 1", "Alternative version 2"],
                "explanation": "Brief explanation of what was changed and why (1-2 sentences)"
            }

            RULES:
            1. The "improved" tweet MUST be 280 characters or less
            2. Each "alternative" MUST be 280 characters or less
            3. Preserve the core message and intent of the original
            4. Make meaningful improvements, not just minor word swaps
            5. No hashtags unless the original had them
            6. No markdown, no explanations outside the JSON
            """;
    }

    private string BuildUserPrompt(ImproveTweetRequestDto request)
    {
        var sb = new StringBuilder();

        sb.AppendLine("ORIGINAL TWEET DRAFT:");
        sb.AppendLine($"\"{request.Draft}\"");
        sb.AppendLine();

        // Improvement type
        var improvementType = request.ImprovementType ?? "more_engaging";
        if (ImprovementPrompts.TryGetValue(improvementType, out var improvementGuide))
        {
            sb.AppendLine("IMPROVEMENT GOAL:");
            sb.AppendLine(improvementGuide);
            sb.AppendLine();
        }

        // Tone
        if (!string.IsNullOrWhiteSpace(request.Tone))
        {
            var toneDesc = ToneDescriptions.TryGetValue(request.Tone, out var desc)
                ? desc
                : request.Tone;
            sb.AppendLine($"TARGET TONE: {toneDesc}");
            sb.AppendLine();
        }

        // Preserve elements
        if (!string.IsNullOrWhiteSpace(request.PreserveElements))
        {
            sb.AppendLine($"MUST PRESERVE: {request.PreserveElements}");
            sb.AppendLine();
        }

        // Additional instructions
        if (!string.IsNullOrWhiteSpace(request.AdditionalInstructions))
        {
            sb.AppendLine($"ADDITIONAL INSTRUCTIONS: {request.AdditionalInstructions}");
            sb.AppendLine();
        }

        sb.AppendLine("CONSTRAINTS:");
        sb.AppendLine($"- Maximum 280 characters per tweet (original is {request.Draft.Length} chars)");
        sb.AppendLine("- Keep the core message intact");
        sb.AppendLine("- Make it feel natural, not forced");
        sb.AppendLine("- Provide 2 alternative versions with different approaches");

        return sb.ToString();
    }

    private static ImproveTweetResponseDto ParseResponse(string raw, string originalDraft)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException("Failed to improve tweet. Empty response.");
        }

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            var improved = root.TryGetProperty("improved", out var improvedEl)
                ? improvedEl.GetString()?.Trim() ?? ""
                : "";

            var alternatives = root.TryGetProperty("alternatives", out var altEl) && altEl.ValueKind == JsonValueKind.Array
                ? altEl.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString()?.Trim() ?? "")
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Take(2)
                    .ToArray()
                : Array.Empty<string>();

            var explanation = root.TryGetProperty("explanation", out var expEl)
                ? expEl.GetString()?.Trim() ?? "Improved for better engagement."
                : "Improved for better engagement.";

            if (string.IsNullOrWhiteSpace(improved))
            {
                throw new InvalidOperationException("Failed to parse improved tweet.");
            }

            return new ImproveTweetResponseDto(
                Original: originalDraft,
                Improved: improved,
                Alternatives: alternatives,
                Explanation: explanation,
                CharacterCount: improved.Length,
                IsWithinLimit: improved.Length <= TwitterCharLimit,
                Model: "");
        }
        catch (JsonException)
        {
            // Fallback: try to extract content from raw text
            var lines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith('{') && !l.StartsWith('}'))
                .ToArray();

            if (lines.Length > 0)
            {
                var improved = lines[0].Trim('"', ' ');
                return new ImproveTweetResponseDto(
                    Original: originalDraft,
                    Improved: improved,
                    Alternatives: Array.Empty<string>(),
                    Explanation: "Improved for better engagement.",
                    CharacterCount: improved.Length,
                    IsWithinLimit: improved.Length <= TwitterCharLimit,
                    Model: "");
            }

            throw new InvalidOperationException("Failed to parse AI response. Please try again.");
        }
    }
}
