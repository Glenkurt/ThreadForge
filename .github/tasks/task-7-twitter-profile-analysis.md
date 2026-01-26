# ThreadForge – User Stories & Execution Plan
## Task 7: Twitter Profile Analysis & Brand Description Generation

---

## Global Constraints

- No authentication (unless explicitly specified)
- Single user session (no multi-tenant logic)
- English UI and generated content
- Desktop-first UX
- Errors must be user-readable
- Twitter username validation required before API call
- No Twitter API credentials required (scraping-based approach)
- Generated brand descriptions are editable before use
- Analysis data is NOT persisted to database (MVP constraint)

---

## Definitions (Shared Vocabulary)

- **Twitter Profile**: Public Twitter/X account identified by username (e.g., @threadforge)
- **Brand Description Document**: A structured document containing brand voice, tone, audience, topics, and content strategy insights
- **Profile Analysis**: Process of extracting and analyzing recent tweets, bio, and engagement patterns
- **Twitter Handle**: Username with or without @ symbol (e.g., "threadforge" or "@threadforge")
- **Tweet Corpus**: Collection of recent tweets (up to 100) used for analysis
- **Brand Voice**: Consistent personality and style reflected in written content
- **Content Pillars**: Main topics or themes an account consistently posts about
- **Engagement Patterns**: Types of content that receive high interaction (likes, retweets, replies)

---

## User Stories

---

### User Story 7.1 – Analyze Twitter Profile by Username

#### Intent

Allow users to analyze any public Twitter profile and receive AI-generated insights about brand voice, content strategy, and audience to inform their own thread creation.

#### Preconditions

- Frontend application is running and accessible
- Backend API is running with X.AI access configured
- User has not exceeded rate limits (same limits as thread generation)
- Target Twitter profile is public (not private/protected)
- Target Twitter profile exists and has at least 10 tweets

#### User Flow (Step-by-Step)

1. User navigates to "Analyze Profile" section in the app
2. User enters a Twitter username in input field (with or without @ symbol)
3. User clicks "Analyze Profile" button
4. System displays loading state with message "Analyzing @username profile..."
5. System fetches recent tweets and profile information
6. System generates brand description document using AI
7. System displays the complete brand description document
8. User can copy the full document to clipboard
9. User can download the document as a .txt or .md file

#### System Behavior

**Backend API:**
1. Receives Twitter username via POST request
2. Validates username format (alphanumeric, underscores, 1-15 chars)
3. Strips @ symbol if present
4. Fetches up to 100 recent tweets from the profile using Twitter scraping library
5. Fetches profile bio, follower count, following count
6. Constructs prompt for X.AI with collected data
7. Sends prompt to X.AI (Grok model)
8. Receives brand description from X.AI
9. Returns structured response with brand description and metadata

**Frontend:**
1. Validates username is not empty before submitting
2. Displays loading spinner during analysis (estimated 10-30 seconds)
3. Renders brand description with formatted sections
4. Provides "Copy" button to copy full text to clipboard
5. Provides "Download" button to save as file
6. Shows error message if analysis fails

#### Inputs

**API Request:**
```typescript
POST /api/v1/profiles/analyze
Content-Type: application/json
X-Client-Id: {clientId}

{
  "username": "threadforge"
}
```

Request constraints:
- username: string, required, 1-15 chars, alphanumeric and underscores only
- username: @ symbol is optional and will be stripped

**Frontend Form:**
- Username input field with placeholder "Enter Twitter username (e.g., @threadforge)"
- "Analyze Profile" button (disabled while loading)

#### Outputs

**API Response:**
```typescript
{
  "username": "threadforge",
  "profileUrl": "https://twitter.com/threadforge",
  "analyzedAt": "2026-01-23T10:30:00Z",
  "tweetCount": 87,
  "brandDescription": {
    "overview": "string (2-3 paragraphs summarizing the brand)",
    "brandVoice": {
      "tone": "string (e.g., 'Professional yet approachable, educational')",
      "style": "string (e.g., 'Direct, actionable, data-driven')",
      "personality": "string (e.g., 'Helpful expert, builds trust through transparency')"
    },
    "targetAudience": {
      "primary": "string (e.g., 'Indie hackers and solo founders building SaaS products')",
      "demographics": "string (e.g., 'Age 25-40, technical background, entrepreneurial mindset')",
      "painPoints": "string[] (e.g., ['Growing audience', 'Content consistency', 'Time management'])"
    },
    "contentPillars": "string[] (3-5 main topics, e.g., ['Building in public', 'AI tools', 'Content strategy'])",
    "contentPatterns": {
      "format": "string (e.g., 'Primarily threads (70%), single tweets (20%), polls (10%)')",
      "length": "string (e.g., 'Threads: 5-7 tweets, Single tweets: 180-240 chars')",
      "structure": "string (e.g., 'Hook + numbered steps + actionable insights + CTA')"
    },
    "engagementInsights": {
      "topPerformingContent": "string[] (e.g., ['How-to threads', 'Personal stories', 'Controversial takes'])",
      "callToActionStyle": "string (e.g., 'Questions, engagement prompts, resource offers')",
      "postingFrequency": "string (e.g., 'Daily, primarily 9-11am EST')"
    },
    "uniqueDifferentiators": "string[] (e.g., ['Shares revenue numbers openly', 'Technical depth with simple explanations'])",
    "recommendedStrategy": {
      "contentTypes": "string[] (e.g., ['Educational threads', 'Behind-the-scenes stories'])",
      "toneGuidance": "string (e.g., 'Maintain authenticity, avoid hype, back claims with data')",
      "topicsToExplore": "string[] (e.g., ['AI automation', 'Audience building', 'Product launches'])"
    }
  }
}
```

**Formatted Display in Frontend:**
Render as expandable/collapsible sections with clear headers and formatted lists.

#### Error Cases

**Error: Invalid Username Format**
- Condition: Username contains invalid characters or is empty
- User Message: "Please enter a valid Twitter username (letters, numbers, and underscores only)"
- HTTP Status: 400

**Error: Profile Not Found**
- Condition: Twitter profile does not exist
- User Message: "Twitter profile @{username} not found. Please check the username and try again."
- HTTP Status: 404

**Error: Profile is Private**
- Condition: Twitter profile is protected/private
- User Message: "Cannot analyze private profiles. Please use a public Twitter account."
- HTTP Status: 403

**Error: Insufficient Tweets**
- Condition: Profile has fewer than 10 tweets
- User Message: "Profile @{username} has too few tweets to analyze. At least 10 tweets required."
- HTTP Status: 400

**Error: Rate Limit Exceeded**
- Condition: User has made too many analysis requests
- User Message: "Analysis limit reached. Please try again in 10 minutes."
- HTTP Status: 429

**Error: Twitter Scraping Failed**
- Condition: Unable to fetch tweets (Twitter blocking, network issue)
- User Message: "Unable to access Twitter data. Please try again later."
- HTTP Status: 503

**Error: AI Generation Failed**
- Condition: X.AI API error or timeout
- User Message: "Profile analysis failed. Please try again."
- HTTP Status: 500

#### Acceptance Criteria

**Backend API:**
- [ ] POST /api/v1/profiles/analyze endpoint exists
- [ ] Username validation strips @ symbol automatically
- [ ] Username validation rejects invalid formats (special chars, >15 chars)
- [ ] Profile existence check before analysis attempt
- [ ] Fetches up to 100 recent tweets
- [ ] Fetches profile bio, follower/following counts
- [ ] Generates brand description via X.AI (Grok)
- [ ] Returns structured JSON matching output schema exactly
- [ ] Rate limiting applied (same as thread generation: 10 requests per 10 minutes)
- [ ] All error cases return appropriate HTTP status codes and messages
- [ ] Response time < 30 seconds for successful analysis
- [ ] No data persisted to database (stateless endpoint)

**Frontend:**
- [ ] "Analyze Profile" section accessible from main navigation
- [ ] Username input field with placeholder text
- [ ] "Analyze Profile" button triggers API call
- [ ] Button disabled and shows loading spinner during analysis
- [ ] Loading message displays "@username is being analyzed..."
- [ ] Brand description displays in formatted, readable layout
- [ ] Sections are collapsible/expandable for better UX
- [ ] "Copy to Clipboard" button copies full brand description as plain text
- [ ] "Download" button saves brand description as .md file (filename: `brand-analysis-{username}-{date}.md`)
- [ ] Copy success message displays briefly ("Copied to clipboard!")
- [ ] Download success message displays briefly ("Analysis downloaded!")
- [ ] All error messages display clearly in red text
- [ ] User can perform multiple analyses in sequence
- [ ] Previous analysis results are replaced (not appended)

#### Implementation Details

**Backend Structure:**

**Create: api/Api/Controllers/ProfilesController.cs**
```csharp
using Api.Models.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/profiles")]
public sealed class ProfilesController : ControllerBase
{
    private readonly IProfileAnalysisService _profileAnalysis;

    public ProfilesController(IProfileAnalysisService profileAnalysis)
    {
        _profileAnalysis = profileAnalysis;
    }

    [HttpPost("analyze")]
    [EnableRateLimiting("threadgen")]
    [ProducesResponseType(typeof(ProfileAnalysisResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ProfileAnalysisResponseDto>> Analyze(
        [FromBody] ProfileAnalysisRequestDto request,
        CancellationToken cancellationToken)
    {
        var clientId = Request.Headers["X-Client-Id"].ToString();
        if (string.IsNullOrWhiteSpace(clientId))
        {
            clientId = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        var result = await _profileAnalysis.AnalyzeAsync(request, clientId, cancellationToken);
        return Ok(result);
    }
}
```

**Create: api/Api/Models/DTOs/ProfileDtos.cs**
```csharp
namespace Api.Models.DTOs;

public sealed record ProfileAnalysisRequestDto(
    string Username
);

public sealed record ProfileAnalysisResponseDto(
    string Username,
    string ProfileUrl,
    DateTime AnalyzedAt,
    int TweetCount,
    BrandDescriptionDto BrandDescription
);

public sealed record BrandDescriptionDto(
    string Overview,
    BrandVoiceDto BrandVoice,
    TargetAudienceDto TargetAudience,
    string[] ContentPillars,
    ContentPatternsDto ContentPatterns,
    EngagementInsightsDto EngagementInsights,
    string[] UniqueDifferentiators,
    RecommendedStrategyDto RecommendedStrategy
);

public sealed record BrandVoiceDto(
    string Tone,
    string Style,
    string Personality
);

public sealed record TargetAudienceDto(
    string Primary,
    string Demographics,
    string[] PainPoints
);

public sealed record ContentPatternsDto(
    string Format,
    string Length,
    string Structure
);

public sealed record EngagementInsightsDto(
    string[] TopPerformingContent,
    string CallToActionStyle,
    string PostingFrequency
);

public sealed record RecommendedStrategyDto(
    string[] ContentTypes,
    string ToneGuidance,
    string[] TopicsToExplore
);
```

**Create: api/Api/Services/IProfileAnalysisService.cs**
```csharp
using Api.Models.DTOs;

namespace Api.Services;

public interface IProfileAnalysisService
{
    Task<ProfileAnalysisResponseDto> AnalyzeAsync(
        ProfileAnalysisRequestDto request,
        string clientId,
        CancellationToken cancellationToken);
}
```

**Create: api/Api/Services/ProfileAnalysisService.cs**
- Implement username validation (strip @, validate format)
- Use a Twitter scraping library (e.g., TweetInvi, or custom scraper using HTTP client)
- Fetch tweets, bio, follower counts
- Construct detailed prompt for X.AI
- Call X.AI via IXaiChatClient
- Parse X.AI response into BrandDescriptionDto structure
- Handle all error cases with appropriate exceptions

**Required NuGet Package:**
```xml
<PackageReference Include="TweetinviAPI" Version="5.0.4" />
```
OR implement custom HTTP scraper if TweetinviAPI doesn't work due to Twitter API limitations.

**X.AI Prompt Template:**

```
You are a brand strategist analyzing a Twitter profile.

Twitter Profile: @{username}
Bio: {bio}
Followers: {followerCount}
Following: {followingCount}

Recent Tweets (up to 100):
{tweetList}

Analyze this Twitter profile and generate a comprehensive brand description document in the following JSON structure:

{
  "overview": "2-3 paragraphs summarizing the brand, what they do, and their unique positioning",
  "brandVoice": {
    "tone": "Describe the overall tone (e.g., professional, casual, humorous, authoritative)",
    "style": "Describe the writing style (e.g., concise, detailed, storytelling, data-driven)",
    "personality": "Describe the personality traits (e.g., helpful expert, provocateur, mentor)"
  },
  "targetAudience": {
    "primary": "Who is the primary audience?",
    "demographics": "Age, profession, background, etc.",
    "painPoints": ["List 3-5 pain points the audience has"]
  },
  "contentPillars": ["List 3-5 main topics they post about"],
  "contentPatterns": {
    "format": "What formats do they use? (threads, single tweets, media, polls)",
    "length": "Typical length of content",
    "structure": "Common structure or formula in their content"
  },
  "engagementInsights": {
    "topPerformingContent": ["What types of content get the most engagement?"],
    "callToActionStyle": "How do they end their posts? What CTAs do they use?",
    "postingFrequency": "How often do they post?"
  },
  "uniqueDifferentiators": ["What makes this account unique or stand out?"],
  "recommendedStrategy": {
    "contentTypes": ["Recommend content types based on their brand"],
    "toneGuidance": "Guidance on maintaining their brand voice",
    "topicsToExplore": ["Suggest 3-5 related topics they could explore"]
  }
}

Return ONLY valid JSON matching this exact structure. Be specific and actionable in your analysis.
```

**Frontend Structure:**

**Create: src/app/features/profile-analysis/profile-analysis.component.ts**
- Input field for username
- Submit button
- Loading state management
- Display formatted results
- Copy to clipboard functionality
- Download as .md functionality

**Create: src/app/services/profile-analysis.service.ts**
- HTTP POST to `/api/v1/profiles/analyze`
- Error handling for all error cases

**File Download Logic:**
Generate Markdown content:
```markdown
# Brand Analysis: @{username}
*Analyzed on {date}*

## Overview
{overview}

## Brand Voice
**Tone:** {tone}
**Style:** {style}
**Personality:** {personality}

## Target Audience
**Primary Audience:** {primary}
**Demographics:** {demographics}

**Pain Points:**
{painPoints as bullet list}

## Content Pillars
{contentPillars as bullet list}

## Content Patterns
**Format:** {format}
**Length:** {length}
**Structure:** {structure}

## Engagement Insights
**Top Performing Content:**
{topPerformingContent as bullet list}

**Call-to-Action Style:** {callToActionStyle}
**Posting Frequency:** {postingFrequency}

## Unique Differentiators
{uniqueDifferentiators as bullet list}

## Recommended Strategy

**Content Types:**
{contentTypes as bullet list}

**Tone Guidance:** {toneGuidance}

**Topics to Explore:**
{topicsToExplore as bullet list}

---
*Generated by ThreadForge | https://threadforge.dev*
```

---

## Non-Goals (Explicitly Out of Scope)

- Analyzing private/protected Twitter profiles
- Persisting analysis results to database
- Historical analysis tracking (no "previous analyses" feature)
- Competitive analysis (comparing multiple profiles)
- Sentiment analysis of individual tweets
- Follower analysis or demographics
- Engagement rate calculations
- Tweet scheduling based on analysis
- Integration with Twitter API (use scraping instead)
- Real-time monitoring of Twitter profiles
- Exporting analysis as PDF
- Sharing analysis results via link
- Multi-language support (English only)
- Profile analysis for other social platforms (Instagram, LinkedIn, etc.)

---

## Execution Notes for Dev Agent

- Use Twitter scraping, NOT official Twitter API (no API keys required)
- If TweetinviAPI doesn't work, implement custom HTTP scraper
- Validate username format strictly (alphanumeric and underscores only)
- Strip @ symbol automatically before processing
- Rate limiting uses same limits as thread generation (reuse existing rate limiter)
- X.AI prompt must be detailed and include the full JSON structure in prompt
- Parse X.AI response carefully; handle malformed JSON gracefully
- Do NOT persist analysis data to database
- Frontend download uses browser's native download mechanism (no server-side file generation)
- Error messages must be user-friendly, not technical
- Test with real Twitter profiles (e.g., @naval, @levelsio, @pmarca)
- Ensure CORS is properly configured for API calls
- Add this endpoint to Swagger documentation (Task 6 completion required first)
