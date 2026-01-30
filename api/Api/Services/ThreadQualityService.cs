using System.Text.RegularExpressions;

namespace Api.Services;

public sealed partial class ThreadQualityService : IThreadQualityService
{
    // Hook strength indicators
    private static readonly string[] PowerVerbs = [
        "stop", "quit", "never", "always", "discovered", "realized", "learned",
        "built", "made", "created", "launched", "shipped", "changed", "transformed",
        "doubled", "tripled", "10x", "failed", "lost", "won", "broke"
    ];

    private static readonly string[] CuriosityPhrases = [
        "here's what", "here's how", "here's why", "this is how", "this is why",
        "the truth", "the secret", "the real reason", "what happened next",
        "nobody talks about", "most people don't", "unpopular opinion",
        "hot take", "controversial", "i was wrong"
    ];

    private static readonly string[] CtaIndicators = [
        "follow", "retweet", "like", "share", "comment", "dm", "link in bio",
        "check out", "subscribe", "join", "sign up", "download", "try",
        "let me know", "what do you think", "agree?", "thoughts?"
    ];

    public ThreadQualityReport Analyze(string[] tweets, string? tone)
    {
        if (tweets.Length == 0)
        {
            return new ThreadQualityReport(0, 0, 0, ["No tweets to analyze"], []);
        }

        var warnings = new List<string>();
        var suggestions = new List<string>();

        // Analyze hook (first tweet)
        var hookScore = AnalyzeHook(tweets[0], warnings, suggestions);

        // Analyze CTA (last tweet)
        var ctaScore = AnalyzeCta(tweets[^1], warnings, suggestions);

        // Check for duplicates
        CheckDuplicates(tweets, warnings);

        // Check emoji usage based on tone
        CheckEmojiUsage(tweets, tone, warnings, suggestions);

        // Calculate overall score
        var overallScore = (hookScore * 2 + ctaScore + 70) / 4; // Hook weighted more

        return new ThreadQualityReport(hookScore, ctaScore, overallScore, [.. warnings], [.. suggestions]);
    }

    private static int AnalyzeHook(string hook, List<string> warnings, List<string> suggestions)
    {
        var score = 50; // Base score
        var hookLower = hook.ToLowerInvariant();

        // Check for numbers (specific > vague)
        if (NumberPattern().IsMatch(hook))
        {
            score += 15;
        }
        else
        {
            suggestions.Add("Add specific numbers to your hook (e.g., '$47K', '3 months', '10x')");
        }

        // Check for power verbs
        var hasPowerVerb = PowerVerbs.Any(v => hookLower.Contains(v));
        if (hasPowerVerb)
        {
            score += 10;
        }

        // Check for curiosity phrases
        var hasCuriosity = CuriosityPhrases.Any(p => hookLower.Contains(p));
        if (hasCuriosity)
        {
            score += 15;
        }
        else
        {
            suggestions.Add("Create a curiosity gap (e.g., 'Here's what happened...')");
        }

        // Check for question (engagement driver)
        if (hook.Contains('?'))
        {
            score += 10;
        }

        // Penalize weak starts
        if (hookLower.StartsWith("i think") || hookLower.StartsWith("in my opinion"))
        {
            score -= 15;
            warnings.Add("Hook starts with weak phrase. Be more direct and confident.");
        }

        // Penalize generic openers
        if (hookLower.StartsWith("today i want to") || hookLower.StartsWith("let me tell you"))
        {
            score -= 10;
            warnings.Add("Hook uses generic opener. Start with impact, not setup.");
        }

        // Check if hook is too short (might lack impact)
        if (hook.Length < 80)
        {
            suggestions.Add("Consider expanding your hook - longer hooks often perform better");
        }

        // Cap score at 100
        return Math.Clamp(score, 0, 100);
    }

    private static int AnalyzeCta(string cta, List<string> warnings, List<string> suggestions)
    {
        var score = 50;
        var ctaLower = cta.ToLowerInvariant();

        // Check for CTA indicators
        var hasCtaIndicator = CtaIndicators.Any(c => ctaLower.Contains(c));
        if (hasCtaIndicator)
        {
            score += 30;
        }
        else
        {
            warnings.Add("Final tweet may lack a clear call-to-action");
            suggestions.Add("End with engagement: 'Follow for more', 'RT if you agree', 'What's your take?'");
        }

        // Check for question (drives comments)
        if (cta.Contains('?'))
        {
            score += 15;
        }

        // Check for action verb
        var actionVerbs = new[] { "follow", "share", "try", "start", "join", "build", "create" };
        if (actionVerbs.Any(v => ctaLower.Contains(v)))
        {
            score += 5;
        }

        return Math.Clamp(score, 0, 100);
    }

    private static void CheckDuplicates(string[] tweets, List<string> warnings)
    {
        // Simple duplicate phrase detection
        var phrases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var tweet in tweets)
        {
            // Extract 4-word phrases
            var words = tweet.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i <= words.Length - 4; i++)
            {
                var phrase = string.Join(" ", words.Skip(i).Take(4));
                if (phrase.Length > 15 && !phrases.Add(phrase))
                {
                    warnings.Add($"Repeated phrase detected: '{phrase}'");
                    return; // Only warn once
                }
            }
        }
    }

    private static void CheckEmojiUsage(string[] tweets, string? tone, List<string> warnings, List<string> suggestions)
    {
        var totalEmojis = tweets.Sum(t => EmojiPattern().Matches(t).Count);

        // Professional tone should have fewer emojis
        if (tone?.Equals("professional", StringComparison.OrdinalIgnoreCase) == true && totalEmojis > 3)
        {
            warnings.Add("Professional tone typically uses fewer emojis");
        }

        // Humorous tone could use more
        if (tone?.Equals("humorous", StringComparison.OrdinalIgnoreCase) == true && totalEmojis == 0)
        {
            suggestions.Add("Consider adding emojis to enhance the humorous tone");
        }
    }

    [GeneratedRegex(@"\$?\d[\d,\.]*[KkMmBb]?|\d+%|\d+x", RegexOptions.Compiled)]
    private static partial Regex NumberPattern();

    [GeneratedRegex(@"[\uD83C-\uDBFF][\uDC00-\uDFFF]", RegexOptions.Compiled)]
    private static partial Regex EmojiPattern();
}
