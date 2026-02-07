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

    // Hook type detailed descriptions with examples
    private static readonly Dictionary<string, string> HookDescriptions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["bold"] = @"BOLD/CONTRARIAN HOOK: Make a provocative statement that challenges assumptions.
Start with 'Unpopular opinion:', 'Hot take:', or just state something controversial.
Example: 'Most productivity advice is keeping you poor. Here's what actually works:'
Example: 'I deleted my to-do list 6 months ago. Best decision I ever made.'",

        ["question"] = @"QUESTION HOOK: Ask something that creates instant self-reflection.
The reader should feel called out or deeply curious. Make it specific, not generic.
Example: 'Why do you check your phone 96 times a day but can't finish a single project?'
Example: 'What would you build if you knew you couldn't fail? Most people never ask this.'",

        ["story"] = @"STORY HOOK: Drop the reader into the middle of action. Create immediate tension.
Start with a moment of conflict, failure, or transformation. No setup - just action.
        Example: 'My startup hit $0 in the bank last Tuesday. What I did next saved everything.'
        Example: 'The email said ""You're fired."" Three months later, I was making 5x my salary.'",

        ["stat"] = @"STAT/NUMBER HOOK: Lead with a specific, surprising number. Concrete beats vague.
Use exact figures, timeframes, or percentages. Make them believe through specificity.
Example: 'I analyzed 1,847 viral tweets. 94% followed this exact pattern:'
Example: '$0 to $127,000 ARR in 9 months. No funding. No team. Here's the playbook:'"
    };

    // Temperature by tone - controls creativity vs focus
    private static readonly Dictionary<string, double> ToneTemperatures = new(StringComparer.OrdinalIgnoreCase)
    {
        ["indie_hacker"] = 0.7,    // Balanced creativity
        ["professional"] = 0.5,    // More focused, less random
        ["humorous"] = 0.9,        // High creativity for jokes
        ["motivational"] = 0.7,    // Balanced
        ["educational"] = 0.5,     // Focused, accurate
        ["provocative"] = 0.8,     // Creative for hot takes
        ["storytelling"] = 0.8,    // Creative for narratives
        ["clear_practical"] = 0.5  // Focused, actionable
    };

    private static readonly string[] ValidHookStrengths = ["bold", "question", "story", "stat"];
    private static readonly string[] ValidCtaTypes = ["soft", "direct", "question"];

    private readonly AppDbContext _db;
    private readonly IXaiChatClient _xai;
    private readonly XaiOptions _xaiOptions;
    private readonly IWebSearchService _webSearch;
    private readonly IThreadQualityService _qualityService;
    private readonly ILogger<ThreadGenerationService> _logger;

    public ThreadGenerationService(
        AppDbContext db,
        IXaiChatClient xai,
        IOptions<XaiOptions> xaiOptions,
        IWebSearchService webSearch,
        IThreadQualityService qualityService,
        ILogger<ThreadGenerationService> logger)
    {
        _db = db;
        _xai = xai;
        _xaiOptions = xaiOptions.Value;
        _webSearch = webSearch;
        _qualityService = qualityService;
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

        // Web research enrichment (optional, fails silently)
        string? webResearchContext = null;
        if (request.UseWebResearch)
        {
            webResearchContext = await RunWebResearchPipelineAsync(request.Topic, cancellationToken);
        }

        var system = BuildSystemPrompt();
        var user = BuildUserPrompt(request, maxChars, webResearchContext);

        // Get temperature based on tone
        var temperature = GetTemperatureForTone(request.Tone);

        var result = await _xai.CreateChatCompletionAsync(
            model,
            new List<(string Role, string Content)>
            {
                ("system", system),
                ("user", user)
            },
            new XaiChatOptions(Temperature: temperature),
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

        var (tweets, hashtags) = ExtractThreadContent(result.Content);
        var useNumbering = request.StylePreferences?.UseNumbering ?? true;

        ValidateGeneratedTweets(tweets, request.TweetCount, maxChars, useNumbering);

        var outputJson = JsonSerializer.Serialize(new { tweets, hashtags }, JsonOptions);

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

        try
        {
            _db.ThreadDrafts.Add(draft);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist thread draft {DraftId}", draft.Id);
            throw new InvalidOperationException("Thread generation failed. Unable to save draft.");
        }

        // Analyze thread quality
        var qualityReport = _qualityService.Analyze(tweets, request.Tone);
        var quality = new ThreadQualityDto(
            qualityReport.HookScore,
            qualityReport.CtaScore,
            qualityReport.OverallScore,
            qualityReport.Warnings,
            qualityReport.Suggestions);

        _logger.LogInformation(
            "Thread quality: Hook={HookScore}, CTA={CtaScore}, Overall={OverallScore}, Warnings={WarningCount}",
            quality.HookScore, quality.CtaScore, quality.OverallScore, quality.Warnings.Length);

        return new GenerateThreadResponseDto(
            draft.Id,
            tweets,
            draft.CreatedAt,
            draft.Provider,
            draft.Model,
            hashtags.Length > 0 ? hashtags : null,
            quality);
    }

    private async Task<string?> RunWebResearchPipelineAsync(string topic, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting web research pipeline for topic: {Topic}",
                topic.Length > 100 ? topic[..100] + "..." : topic);

            // Step 1: Generate optimized search queries
            var queries = await _webSearch.GenerateSearchQueriesAsync(topic, cancellationToken);
            if (queries.Length == 0)
            {
                _logger.LogWarning("No search queries generated. Skipping web research.");
                return null;
            }

            // Step 2: Execute searches in parallel
            var searchTasks = queries.Select(q => _webSearch.SearchAsync(q, cancellationToken));
            var searchResults = await Task.WhenAll(searchTasks);

            var allResults = searchResults.SelectMany(r => r).ToList();
            if (allResults.Count == 0)
            {
                _logger.LogWarning("No search results found. Skipping web research.");
                return null;
            }

            _logger.LogInformation(
                "Web search completed: {QueryCount} queries, {ResultCount} results",
                queries.Length, allResults.Count);

            // Step 3: Synthesize results into structured context
            var synthesis = await _webSearch.SynthesizeAsync(topic, allResults, cancellationToken);
            if (string.IsNullOrWhiteSpace(synthesis))
            {
                _logger.LogWarning("Research synthesis returned empty. Skipping web research.");
                return null;
            }

            _logger.LogInformation("Web research pipeline completed successfully");
            return synthesis;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Web research pipeline failed. Falling back to standard generation.");
            return null;
        }
    }

    private static void ValidateGeneratedTweets(
        string[] tweets,
        int requestedCount,
        int maxChars,
        bool useNumbering)
    {
        if (tweets.Length != requestedCount)
        {
            throw new InvalidOperationException("Thread generation failed. Try again.");
        }

        for (var i = 0; i < tweets.Length; i++)
        {
            var tweet = tweets[i];

            if (string.IsNullOrWhiteSpace(tweet))
            {
                throw new InvalidOperationException("Thread generation failed. Try again.");
            }

            if (tweet.Length > maxChars)
            {
                throw new InvalidOperationException("Thread generation failed. Try again.");
            }

            if (useNumbering)
            {
                var expectedSuffix = $" {i + 1}/{requestedCount}";
                if (!tweet.EndsWith(expectedSuffix, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Thread generation failed. Try again.");
                }
            }
        }
    }

    private static void ValidateRequest(GenerateThreadRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Topic))
        {
            throw new ArgumentException("Topic is required");
        }

        if (request.Topic.Length > 1000)
        {
            throw new ArgumentException("Topic must not exceed 1000 characters");
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
            You are ThreadForge, an elite X/Twitter ghostwriter who creates viral threads. Your threads get millions of impressions because you understand what makes people STOP scrolling.

            OUTPUT FORMAT: Return ONLY valid JSON:
            {"tweets":["tweet1","tweet2",...], "hashtags":["tag1","tag2","tag3"]}

            HASHTAG RULES:
            - Generate 2-4 relevant hashtags (without # symbol)
            - Use popular, searchable tags related to the topic
            - Include one broad tag (e.g., "buildinpublic") and specific ones
            - No branded hashtags, no spam tags
            - Good examples: buildinpublic, indiehackers, saas, startups, productivity

            No markdown, no explanations outside the JSON.

            HOOK MASTERY (Tweet 1 is EVERYTHING):
            The first tweet determines if anyone reads the rest. Use these proven patterns:

            Pattern 1 - Curiosity Gap: Tease a surprising outcome without revealing it
            "I mass-unfollowed 2,000 people yesterday. Here's the uncomfortable truth I discovered:"

            Pattern 2 - Contrarian Take: Challenge what everyone believes
            "Unpopular opinion: Your morning routine is destroying your productivity."

            Pattern 3 - Specific Numbers: Concrete results create credibility
            "I went from 0 to $47,000/mo in 11 months. No ads. No audience. Just this strategy:"

            Pattern 4 - Story Loop: Start mid-action, create tension
            "My co-founder called me at 2am. 'We're done.' But what happened next changed everything."

            Pattern 5 - Direct Challenge: Make it personal
            "You're losing 3 hours every day and don't even realize it. Let me show you where:"

            THREAD STRUCTURE:
            - Hook (Tweet 1): Pattern interrupt. Make them NEED to read more.
            - Body: Each tweet must earn the next click. End tweets mid-thought when possible.
            - Closer: Strong CTA that feels natural, not salesy.

            WRITING RULES:
            - Every tweet ≤ specified max characters
            - Short sentences. Punch hard.
            - One idea per tweet. White space is your friend.
            - Use "you" more than "I" - make it about the reader
            - Specific > generic (say "$4,847" not "thousands")
            - Active voice. Present tense when possible.
            - No fluff words: very, really, just, actually, basically
            """;
    }

    private static string BuildUserPrompt(GenerateThreadRequestDto request, int maxChars, string? webResearchContext = null)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Write a VIRAL X/Twitter thread that makes people stop scrolling.");
        sb.AppendLine("The hook must be impossible to ignore. Every tweet must earn the next click.");
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

        // Hook strength - now with detailed guidance
        sb.AppendLine();
        sb.AppendLine("HOOK REQUIREMENT (Critical - this determines if anyone reads your thread):");
        var hookType = prefs?.HookStrength?.ToLowerInvariant() ?? "bold";
        if (HookDescriptions.TryGetValue(hookType, out var hookGuide))
        {
            sb.AppendLine(hookGuide);
        }
        else
        {
            sb.AppendLine(HookDescriptions["bold"]); // Default to bold
        }
        sb.AppendLine();

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

        // Web research context (injected from enrichment pipeline)
        if (!string.IsNullOrWhiteSpace(webResearchContext))
        {
            sb.AppendLine("WEB RESEARCH CONTEXT (use these facts, stats, and insights to make the thread more concrete and data-driven):");
            sb.AppendLine(webResearchContext);
            sb.AppendLine();
            sb.AppendLine("IMPORTANT: Weave the above research naturally into the thread. Cite specific numbers and examples. Do NOT just list facts — integrate them into engaging tweets.");
            sb.AppendLine();
        }

        // Universal rules - stronger emphasis on engagement
        sb.AppendLine("THREAD RULES (Follow exactly):");
        sb.AppendLine($"1. Every tweet MUST be under {maxChars} characters. No exceptions.");
        sb.AppendLine("2. Tweet 1 = pattern interrupt. If it doesn't make someone stop scrolling, rewrite it.");
        sb.AppendLine("3. Each tweet should create tension for the next. End mid-thought when possible.");
        sb.AppendLine("4. Be specific: '$4,231' not 'thousands', '47 days' not 'a few weeks'.");
        sb.AppendLine("5. Short sentences. One idea per tweet. White space makes it readable.");
        sb.AppendLine("6. Cut fluff: remove 'very', 'really', 'just', 'actually', 'basically'.");
        sb.AppendLine("7. Use 'you' and 'your' - make it about the reader, not yourself.");
        sb.AppendLine("8. Final tweet: CTA that feels earned, not forced.");

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

    private static double GetTemperatureForTone(string? tone)
    {
        if (string.IsNullOrWhiteSpace(tone))
        {
            return 0.7; // Default balanced temperature
        }

        return ToneTemperatures.TryGetValue(tone, out var temp) ? temp : 0.7;
    }

    private static string Capitalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        return char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
    }

    private static (string[] Tweets, string[] Hashtags) ExtractThreadContent(string raw)
    {
        string[] tweets = [];
        string[] hashtags = [];

        if (string.IsNullOrWhiteSpace(raw))
        {
            return (["(No output)"], []);
        }

        // Try strict JSON first
        try
        {
            using var doc = JsonDocument.Parse(raw);

            if (doc.RootElement.TryGetProperty("tweets", out var tweetsEl) && tweetsEl.ValueKind == JsonValueKind.Array)
            {
                tweets = tweetsEl.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => (e.GetString() ?? string.Empty).Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToArray();
            }

            if (doc.RootElement.TryGetProperty("hashtags", out var hashtagsEl) && hashtagsEl.ValueKind == JsonValueKind.Array)
            {
                hashtags = hashtagsEl.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => (e.GetString() ?? string.Empty).Trim().TrimStart('#'))
                    .Where(s => !string.IsNullOrWhiteSpace(s) && s.Length <= 30)
                    .Take(5)
                    .ToArray();
            }

            if (tweets.Length > 0)
            {
                return (tweets, hashtags);
            }
        }
        catch
        {
            // ignore and fallback
        }

        // Fallback: split by lines and strip numbering (no hashtags in fallback)
        tweets = raw
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Select(l => l.TrimStart('-', '•', '*', ' '))
            .Select(l => l.StartsWith("#") ? l : l)
            .Select(l => StripLeadingNumbering(l))
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToArray();

        return (tweets, []);
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

    public async Task<RegenerateTweetResponseDto> RegenerateSingleTweetAsync(
        string[] existingTweets,
        int tweetIndex,
        string? feedback,
        string? tone,
        int maxChars,
        CancellationToken cancellationToken)
    {
        if (tweetIndex < 1 || tweetIndex > existingTweets.Length)
        {
            throw new ArgumentException($"Tweet index must be between 1 and {existingTweets.Length}");
        }

        var model = string.IsNullOrWhiteSpace(_xaiOptions.Model) ? "grok-2-latest" : _xaiOptions.Model;
        var temperature = GetTemperatureForTone(tone);
        var totalTweets = existingTweets.Length;

        // Build context from surrounding tweets
        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine("You are rewriting ONE tweet within an existing thread. Output ONLY the new tweet text, nothing else.");
        contextBuilder.AppendLine();
        contextBuilder.AppendLine("EXISTING THREAD:");
        for (var i = 0; i < existingTweets.Length; i++)
        {
            var marker = i + 1 == tweetIndex ? " <-- REWRITE THIS ONE" : "";
            contextBuilder.AppendLine($"Tweet {i + 1}: {existingTweets[i]}{marker}");
        }
        contextBuilder.AppendLine();

        // Position-specific instructions
        string positionGuidance;
        if (tweetIndex == 1)
        {
            positionGuidance = "This is the HOOK tweet. It must grab attention and make readers NEED to continue. Use a pattern interrupt.";
        }
        else if (tweetIndex == totalTweets)
        {
            positionGuidance = "This is the FINAL tweet. It must have a clear call-to-action that feels earned.";
        }
        else
        {
            positionGuidance = $"This is a BODY tweet. It should flow from tweet {tweetIndex - 1} and lead into tweet {tweetIndex + 1}.";
        }

        contextBuilder.AppendLine($"POSITION: {positionGuidance}");
        contextBuilder.AppendLine();
        contextBuilder.AppendLine($"CONSTRAINTS:");
        contextBuilder.AppendLine($"- Maximum {maxChars} characters INCLUDING the {tweetIndex}/{totalTweets} suffix");
        contextBuilder.AppendLine($"- End with: {tweetIndex}/{totalTweets}");
        contextBuilder.AppendLine("- NEVER truncate. Every sentence must be complete.");
        contextBuilder.AppendLine("- Keep the same general topic but improve the writing.");

        if (!string.IsNullOrWhiteSpace(feedback))
        {
            contextBuilder.AppendLine();
            contextBuilder.AppendLine($"USER FEEDBACK: {feedback}");
        }

        if (!string.IsNullOrWhiteSpace(tone))
        {
            var toneDesc = GetToneDescription(tone);
            contextBuilder.AppendLine();
            contextBuilder.AppendLine($"TONE: {toneDesc}");
        }

        contextBuilder.AppendLine();
        contextBuilder.AppendLine("Output ONLY the rewritten tweet text. No quotes, no explanation, no JSON.");

        var result = await _xai.CreateChatCompletionAsync(
            model,
            new List<(string Role, string Content)>
            {
                ("user", contextBuilder.ToString())
            },
            new XaiChatOptions(Temperature: temperature, JsonMode: false),
            cancellationToken);

        var newTweet = result.Content.Trim().Trim('"');

        // Validate the tweet
        if (string.IsNullOrWhiteSpace(newTweet))
        {
            throw new InvalidOperationException("Failed to regenerate tweet. Try again.");
        }

        if (newTweet.Length > maxChars)
        {
            throw new InvalidOperationException("Regenerated tweet exceeds character limit. Try again.");
        }

        // Ensure numbering suffix
        var expectedSuffix = $" {tweetIndex}/{totalTweets}";
        if (!newTweet.EndsWith(expectedSuffix, StringComparison.Ordinal))
        {
            // Try to add it if there's room
            if (newTweet.Length + expectedSuffix.Length <= maxChars)
            {
                newTweet = newTweet.TrimEnd() + expectedSuffix;
            }
        }

        _logger.LogInformation(
            "Single tweet regenerated: index={Index}, chars={Chars}",
            tweetIndex, newTweet.Length);

        return new RegenerateTweetResponseDto(newTweet, tweetIndex);
    }

}
