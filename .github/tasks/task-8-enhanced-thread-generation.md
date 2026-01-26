# ThreadForge – Enhanced Thread Generation (Grok Feedback Implementation)

## Global Constraints

- No authentication changes (existing auth system unchanged)
- Single user session (no multi-tenant logic changes)
- English UI only
- Desktop-first UX
- All errors must be user-readable with specific messages
- All generated threads remain editable before use
- Backward compatibility: existing API requests without new fields MUST continue working with sensible defaults

---

## Definitions (Shared Vocabulary)

- **Thread**: A sequence of tweets intended to be posted in order on X/Twitter
- **Hook**: The first tweet designed to capture attention using a specific style (bold, question, story, stat)
- **CTA (Call To Action)**: Final tweet prompting a user action (soft, direct, or question style)
- **Brand Guidelines**: User-provided text describing their brand voice, vocabulary, dos/don'ts, and favorite phrases
- **Example Threads**: Full thread samples provided by the user for few-shot prompting to match style
- **Style Preferences**: Granular configuration options for thread formatting (emojis, numbering, char limits, hook/CTA types)
- **Tone Expansion**: Server-side mapping of short tone codes to full descriptive prompts
- **Few-Shot Prompting**: Providing examples to the AI to steer output style and format

---

## User Stories

---

### User Story 1 – Add Brand Guidelines Field to API

#### Intent

As a user, I want to provide my brand voice description so that generated threads match my unique writing style and vocabulary.

#### Preconditions

- Backend API is running
- Thread generation endpoint exists at `POST /api/v1/threads/generate`
- User has a brand guidelines document or description ready

#### User Flow (Step-by-Step)

1. User includes `brandGuidelines` field in the thread generation request body
2. User submits the request to `/api/v1/threads/generate`
3. System validates the field length
4. System incorporates brand guidelines into the AI prompt
5. System returns the generated thread

#### System Behavior

1. API accepts `brandGuidelines` as an optional string field in `GenerateThreadRequestDto`
2. API validates `brandGuidelines` does not exceed 1500 characters
3. If `brandGuidelines` is provided and non-empty, the prompt builder appends a dedicated section titled "Brand guidelines (follow strictly):" followed by the user's text
4. If `brandGuidelines` is null or empty, the prompt builder skips this section entirely

#### Inputs

| Field | Type | Required | Constraints | Default |
|-------|------|----------|-------------|---------|
| `brandGuidelines` | string | No | Max 1500 characters, nullable | null |

#### Outputs

- Standard `GenerateThreadResponseDto` with tweets following the brand guidelines

#### Error Cases

| Condition | HTTP Status | Error Message |
|-----------|-------------|---------------|
| `brandGuidelines` exceeds 1500 characters | 400 | "Brand guidelines must not exceed 1500 characters" |

#### Acceptance Criteria

- [ ] `GenerateThreadRequestDto` includes `BrandGuidelines` property of type `string?`
- [ ] Validation rejects requests where `brandGuidelines.Length > 1500`
- [ ] Prompt builder includes brand guidelines section when field is non-empty
- [ ] Prompt builder omits brand guidelines section when field is null or empty
- [ ] Existing requests without `brandGuidelines` continue to work unchanged
- [ ] Swagger documentation shows the new field with description and example

---

### User Story 2 – Add Example Threads Field for Few-Shot Prompting

#### Intent

As a user, I want to provide example threads so that the AI closely matches my preferred writing style and structure.

#### Preconditions

- Backend API is running
- Thread generation endpoint exists at `POST /api/v1/threads/generate`
- User has 1-3 example threads ready (full text, not links)

#### User Flow (Step-by-Step)

1. User includes `exampleThreads` array in the thread generation request body
2. User submits the request to `/api/v1/threads/generate`
3. System validates the array length
4. System incorporates example threads into the AI prompt as few-shot examples
5. System returns the generated thread matching the example style

#### System Behavior

1. API accepts `exampleThreads` as an optional array of strings in `GenerateThreadRequestDto`
2. API validates `exampleThreads` contains at most 3 items
3. API validates each example thread does not exceed 5000 characters
4. If `exampleThreads` is provided and non-empty, the prompt builder appends a section titled "Learn from these example threads (match style, voice, structure, and formatting closely):" followed by each example prefixed with "Example 1:", "Example 2:", etc.
5. If `exampleThreads` is null or empty, the prompt builder skips this section entirely

#### Inputs

| Field | Type | Required | Constraints | Default |
|-------|------|----------|-------------|---------|
| `exampleThreads` | string[] | No | Max 3 items, each item max 5000 characters, nullable | null |

#### Outputs

- Standard `GenerateThreadResponseDto` with tweets matching the example thread style

#### Error Cases

| Condition | HTTP Status | Error Message |
|-----------|-------------|---------------|
| `exampleThreads` has more than 3 items | 400 | "Example threads must not exceed 3 items" |
| Any item in `exampleThreads` exceeds 5000 characters | 400 | "Each example thread must not exceed 5000 characters" |

#### Acceptance Criteria

- [ ] `GenerateThreadRequestDto` includes `ExampleThreads` property of type `string[]?`
- [ ] Validation rejects requests where `exampleThreads.Length > 3`
- [ ] Validation rejects requests where any example exceeds 5000 characters
- [ ] Prompt builder includes numbered example threads section when array is non-empty
- [ ] Prompt builder omits example threads section when array is null or empty
- [ ] Existing requests without `exampleThreads` continue to work unchanged
- [ ] Swagger documentation shows the new field with description and example

---

### User Story 3 – Add Style Preferences Object to API

#### Intent

As a user, I want granular control over thread formatting options so that I can customize emoji usage, numbering, character limits, hook style, and CTA type without writing prose descriptions.

#### Preconditions

- Backend API is running
- Thread generation endpoint exists at `POST /api/v1/threads/generate`

#### User Flow (Step-by-Step)

1. User includes `stylePreferences` object in the thread generation request body
2. User sets individual preferences (useEmojis, useNumbering, maxCharsPerTweet, hookStrength, ctaType)
3. User submits the request to `/api/v1/threads/generate`
4. System validates each preference value
5. System incorporates style preferences into the AI prompt
6. System applies post-processing based on maxCharsPerTweet
7. System returns the generated thread

#### System Behavior

1. API accepts `stylePreferences` as an optional object in `GenerateThreadRequestDto`
2. API validates each field within `stylePreferences`:
   - `useEmojis`: boolean or null (default: null, meaning AI decides)
   - `useNumbering`: boolean or null (default: true)
   - `maxCharsPerTweet`: integer 200-280 or null (default: 260)
   - `hookStrength`: string enum ["bold", "question", "story", "stat"] or null (default: null, meaning "strong")
   - `ctaType`: string enum ["soft", "direct", "question"] or null (default: null, meaning "clear")
3. If `stylePreferences` is provided, the prompt builder appends a "Style preferences:" section with each non-null preference listed
4. Post-processing uses `maxCharsPerTweet` (or 260 if null) instead of hardcoded 280
5. If `stylePreferences` is null, the prompt builder uses default values and 260 char limit

#### Inputs

| Field | Type | Required | Constraints | Default |
|-------|------|----------|-------------|---------|
| `stylePreferences` | object | No | See sub-fields below | null |
| `stylePreferences.useEmojis` | boolean | No | true/false/null | null (AI decides) |
| `stylePreferences.useNumbering` | boolean | No | true/false/null | true |
| `stylePreferences.maxCharsPerTweet` | integer | No | 200-280, null | 260 |
| `stylePreferences.hookStrength` | string | No | "bold", "question", "story", "stat", null | null |
| `stylePreferences.ctaType` | string | No | "soft", "direct", "question", null | null |

#### Outputs

- Standard `GenerateThreadResponseDto` with tweets following the style preferences
- Tweets are enforced to `maxCharsPerTweet` length (default 260)

#### Error Cases

| Condition | HTTP Status | Error Message |
|-----------|-------------|---------------|
| `maxCharsPerTweet` < 200 | 400 | "Max characters per tweet must be at least 200" |
| `maxCharsPerTweet` > 280 | 400 | "Max characters per tweet must not exceed 280" |
| `hookStrength` is not a valid enum value | 400 | "Hook strength must be one of: bold, question, story, stat" |
| `ctaType` is not a valid enum value | 400 | "CTA type must be one of: soft, direct, question" |

#### Acceptance Criteria

- [ ] `StylePreferencesDto` record created with all five properties
- [ ] `GenerateThreadRequestDto` includes `StylePreferences` property of type `StylePreferencesDto?`
- [ ] Validation enforces `maxCharsPerTweet` between 200-280
- [ ] Validation enforces `hookStrength` enum values
- [ ] Validation enforces `ctaType` enum values
- [ ] Prompt builder includes style preferences section with clear instructions for each non-null preference
- [ ] Post-processing `EnforceTweetLength` uses `maxCharsPerTweet` value (default 260, not 280)
- [ ] Existing requests without `stylePreferences` continue to work with default 260 char limit
- [ ] Swagger documentation shows the new nested object with descriptions and examples

---

### User Story 4 – Implement Tone Expansion with Server-Side Mapping

#### Intent

As a user, I want short tone codes to be automatically expanded into rich descriptions so that the AI receives clear, detailed guidance on writing style.

#### Preconditions

- Backend API is running
- Thread generation endpoint exists at `POST /api/v1/threads/generate`

#### User Flow (Step-by-Step)

1. User includes `tone` field with a short code (e.g., "indie_hacker")
2. User submits the request to `/api/v1/threads/generate`
3. System maps the tone code to a full description
4. System incorporates the expanded tone into the AI prompt
5. System returns the generated thread

#### System Behavior

1. API maintains a static dictionary mapping tone codes to full descriptions
2. Tone mappings are as follows:

| Tone Code | Full Description |
|-----------|------------------|
| `indie_hacker` | "Casual, transparent, no-BS voice. Use first-person, share real numbers and failures, motivational but realistic. Favorite phrases: 'here's what actually happened', 'shipped this in a weekend', 'MRR update'." |
| `professional` | "Clear, structured, authoritative but approachable. Use bullet-style lists inside tweets, data-driven examples." |
| `humorous` | "Witty, sarcastic when appropriate, internet-native humor. Use meme references lightly." |
| `motivational` | "Inspiring, energetic, lots of questions to the reader. Encourage action and positivity." |
| `educational` | "Teacher-like, step-by-step explanations, uses analogies and examples to clarify concepts." |
| `provocative` | "Bold, contrarian, challenges conventional wisdom. Uses strong statements to spark engagement." |
| `storytelling` | "Narrative-driven, uses personal anecdotes, builds tension and resolution across tweets." |
| `clear_practical` | "Straightforward, actionable, step-by-step. Focus on practical advice over theory." |

3. If `tone` matches a known code (case-insensitive), the prompt builder uses the full description
4. If `tone` does not match any known code, the prompt builder uses the user-provided tone value as-is (allows custom tones)
5. If `tone` is null or empty, the prompt builder uses `clear_practical` description as default

#### Inputs

| Field | Type | Required | Constraints | Default |
|-------|------|----------|-------------|---------|
| `tone` | string | No | Max 100 characters, nullable | "clear_practical" |

#### Outputs

- Standard `GenerateThreadResponseDto` with tweets matching the expanded tone description

#### Error Cases

| Condition | HTTP Status | Error Message |
|-----------|-------------|---------------|
| `tone` exceeds 100 characters | 400 | "Tone must not exceed 100 characters" |

#### Acceptance Criteria

- [ ] Static dictionary `ToneDescriptions` exists in `ThreadGenerationService` with all 8 tone mappings
- [ ] Tone lookup is case-insensitive (e.g., "Indie_Hacker" matches "indie_hacker")
- [ ] Unknown tone values pass through unchanged to allow custom tones
- [ ] Null or empty tone defaults to `clear_practical` description
- [ ] Prompt includes full tone description, not the short code
- [ ] Validation enforces max 100 character limit on tone field
- [ ] Swagger documentation lists available tone codes with descriptions

---

### User Story 5 – Enhance System Prompt for Higher Quality Output

#### Intent

As a system, I want an improved system prompt with explicit quality rules so that generated threads have consistently better hooks, CTAs, and flow.

#### Preconditions

- Backend API is running
- Thread generation endpoint exists at `POST /api/v1/threads/generate`

#### User Flow (Step-by-Step)

1. User submits a thread generation request
2. System uses the enhanced system prompt
3. AI generates higher quality thread following explicit rules
4. System returns the generated thread

#### System Behavior

1. Replace the current system prompt with the following enhanced version:

```
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
```

2. The system prompt is static and does not change based on user input
3. User-specific instructions are always in the user message, not the system prompt

#### Inputs

- None (system prompt is internal)

#### Outputs

- Higher quality threads with better hooks, CTAs, and flow

#### Error Cases

- None (internal change)

#### Acceptance Criteria

- [ ] System prompt in `ThreadGenerationService.GenerateAsync` is replaced with the enhanced version
- [ ] System prompt mentions default 260 character limit
- [ ] System prompt explicitly requires strong hook in tweet 1
- [ ] System prompt explicitly requires CTA in last tweet
- [ ] System prompt explicitly requires following brand guidelines, style preferences, and examples
- [ ] System prompt explicitly requires natural line breaks
- [ ] All existing tests continue to pass

---

### User Story 6 – Implement Rich User Message Construction

#### Intent

As a system, I want the user prompt to be assembled from all available fields in a structured, instructive format so that the AI receives maximum context for optimal output.

#### Preconditions

- Backend API is running
- All new fields (brandGuidelines, exampleThreads, stylePreferences) are implemented
- Tone expansion is implemented

#### User Flow (Step-by-Step)

1. User submits a thread generation request with any combination of fields
2. System builds a rich user prompt by assembling all provided fields
3. AI generates a thread using the comprehensive prompt
4. System returns the generated thread

#### System Behavior

1. The `BuildUserPrompt` method constructs the user message using this exact template (sections are included only if the field is provided and non-empty):

```
Write an engaging X/Twitter thread.

Topic: {topic}

Audience: {audience or "builders and makers"}

Tone: {expanded tone description from tone mapping}

Tweet count: {tweetCount}

Key points to cover (distribute naturally across the thread):
- {keyPoint1}
- {keyPoint2}
...

Brand guidelines (follow strictly):
{brandGuidelines}

Learn from these example threads (match style, voice, structure, and formatting closely):
Example 1:
{exampleThread1}

Example 2:
{exampleThread2}

Style preferences:
- Thread numbering: {if useNumbering: "Include X/{tweetCount} at the end of each tweet" else: "Do not number tweets"}
- Emojis: {if useEmojis true: "Use relevant emojis sparingly to enhance engagement" elif useEmojis false: "No emojis" else: "Use emojis at your discretion"}
- Max chars per tweet: {maxCharsPerTweet or 260}
- First tweet hook: {hookStrength capitalized + " style hook" or "Strong hook"}
- Final CTA: {ctaType capitalized + " call to action" or "Clear CTA"}

Previous feedback / regeneration instructions: {feedback}

Universal rules:
- Keep each tweet under {maxCharsPerTweet or 260} characters and highly readable.
- Use short paragraphs and line breaks within tweets.
- Make it conversational, authentic, and valuable.
- End the last tweet with the CTA.
```

2. Omit entire sections (including headers) if the corresponding field is null or empty
3. Style preferences section is always included with defaults if `stylePreferences` is null

#### Inputs

- All fields from `GenerateThreadRequestDto`

#### Outputs

- Properly formatted user prompt string passed to AI

#### Error Cases

- None (internal construction)

#### Acceptance Criteria

- [ ] `BuildUserPrompt` method is refactored to include all new fields
- [ ] Each section is conditionally included based on field presence
- [ ] Tone is expanded using the tone mapping dictionary
- [ ] Style preferences section uses explicit instructions for each preference
- [ ] Universal rules section is always included at the end
- [ ] Character limit in universal rules uses `maxCharsPerTweet` value
- [ ] All existing tests continue to pass
- [ ] Integration tests verify prompt structure with various field combinations

---

### User Story 7 – Enhance Post-Processing with Thread Continuity Markers

#### Intent

As a user, I want split tweets to have continuity markers so that the thread maintains coherence when tweets are broken up.

#### Preconditions

- Backend API is running
- `EnforceTweetLength` method exists in `ThreadGenerationService`

#### User Flow (Step-by-Step)

1. User submits a thread generation request
2. AI generates tweets, some exceeding the character limit
3. System splits long tweets into multiple tweets
4. System adds "🧵" marker to split tweets (except the last part)
5. System returns the processed thread

#### System Behavior

1. When `SplitToMaxLength` splits a tweet into multiple parts:
   - All parts except the last one receive a "🧵" suffix (with space before)
   - The marker is included in the character count (so max length becomes `maxCharsPerTweet - 2`)
2. The "🧵" marker is only added to split tweets, not to naturally short tweets
3. The marker is added before the length validation, not after

#### Inputs

- Tweets array from AI response
- `maxCharsPerTweet` value (default 260)

#### Outputs

- Processed tweets array with continuity markers on split tweets

#### Error Cases

- None (internal processing)

#### Acceptance Criteria

- [ ] Split tweets (except last part) end with " 🧵"
- [ ] Character limit accounts for marker length (2 characters: space + emoji)
- [ ] Naturally short tweets do not receive the marker
- [ ] Last part of a split tweet does not receive the marker
- [ ] Marker is visible and consistent across all split scenarios
- [ ] Unit tests verify marker placement logic

---

### User Story 8 – Update Feedback Field Character Limit

#### Intent

As a user, I want a larger feedback field so that I can provide more detailed regeneration instructions.

#### Preconditions

- Backend API is running
- Thread generation endpoint exists at `POST /api/v1/threads/generate`

#### User Flow (Step-by-Step)

1. User includes detailed `feedback` in the thread generation request
2. User submits the request to `/api/v1/threads/generate`
3. System validates the feedback length against new limit
4. System incorporates feedback into the AI prompt
5. System returns the regenerated thread

#### System Behavior

1. Update validation to allow `feedback` up to 1000 characters (previously 500)
2. Swagger documentation reflects the new limit

#### Inputs

| Field | Type | Required | Constraints | Default |
|-------|------|----------|-------------|---------|
| `feedback` | string | No | Max 1000 characters (updated from 500), nullable | null |

#### Outputs

- Standard `GenerateThreadResponseDto`

#### Error Cases

| Condition | HTTP Status | Error Message |
|-----------|-------------|---------------|
| `feedback` exceeds 1000 characters | 400 | "Feedback must not exceed 1000 characters" |

#### Acceptance Criteria

- [ ] Validation accepts `feedback` up to 1000 characters
- [ ] Validation rejects `feedback` exceeding 1000 characters with clear error message
- [ ] Swagger documentation shows max 1000 character limit
- [ ] Existing requests with ≤500 character feedback continue to work

---

### User Story 9 – Add Token Usage Logging

#### Intent

As a system operator, I want token usage logged for each generation request so that I can monitor API costs and optimize prompts.

#### Preconditions

- Backend API is running
- xAI API returns token usage in response
- Logging infrastructure exists

#### User Flow (Step-by-Step)

1. User submits a thread generation request
2. System calls xAI API
3. xAI API returns response with token usage
4. System logs token usage at Info level
5. System returns the generated thread

#### System Behavior

1. `IXaiChatClient.CreateChatCompletionAsync` returns a result object containing:
   - `Content`: the generated text (existing)
   - `PromptTokens`: integer or null
   - `CompletionTokens`: integer or null
   - `TotalTokens`: integer or null
2. If token usage is available, log at Info level: `"Thread generation completed. Tokens: prompt={PromptTokens}, completion={CompletionTokens}, total={TotalTokens}"`
3. If token usage is not available (null), log at Debug level: `"Thread generation completed. Token usage not available."`
4. Token usage is NOT returned in the API response (internal logging only)

#### Inputs

- None (internal logging)

#### Outputs

- Log entries with token usage

#### Error Cases

- None (logging is best-effort)

#### Acceptance Criteria

- [ ] `XaiChatClient` parses and returns token usage from xAI API response
- [ ] Create `XaiChatCompletionResult` record with `Content`, `PromptTokens`, `CompletionTokens`, `TotalTokens` properties
- [ ] `IXaiChatClient` interface updated to return `XaiChatCompletionResult` instead of string
- [ ] `ThreadGenerationService` logs token usage at Info level
- [ ] Missing token usage is logged at Debug level, not Error
- [ ] Existing functionality is not affected

---

### User Story 10 – Update Frontend Thread Generation Form

#### Intent

As a user, I want the frontend thread generation form to support all new API fields so that I can use brand guidelines, example threads, and style preferences from the UI.

#### Preconditions

- Backend API has all new fields implemented
- Frontend thread generation component exists

#### User Flow (Step-by-Step)

1. User navigates to the thread generation page
2. User fills in topic, tweet count, and basic options (existing)
3. User expands "Advanced Options" section
4. User optionally adds brand guidelines in a textarea
5. User optionally adds up to 3 example threads
6. User optionally configures style preferences
7. User clicks "Generate Thread"
8. Frontend sends request with all fields to backend
9. Generated thread is displayed

#### System Behavior

1. Thread generation form adds collapsible "Advanced Options" section
2. Advanced Options section contains:
   - **Brand Guidelines**: textarea, placeholder "Describe your brand voice, vocabulary, dos/don'ts...", max 1500 chars, character counter displayed
   - **Example Threads**: dynamic list allowing 0-3 entries, each entry is a textarea with placeholder "Paste a full example thread...", max 5000 chars each, "Add Example" button disabled when 3 entries exist
   - **Style Preferences** subsection:
     - Emojis: 3-way toggle (Default / Yes / No)
     - Numbering: 3-way toggle (Default / Yes / No), default selected
     - Max Chars Per Tweet: number input, min 200, max 280, default 260
     - Hook Style: dropdown with options [Default, Bold, Question, Story, Stat]
     - CTA Type: dropdown with options [Default, Soft, Direct, Question]
3. Form state is preserved when navigating away and returning
4. "Clear Advanced Options" button resets all advanced fields to defaults

#### Inputs

- All new fields added to the form model
- Form validation enforces all constraints

#### Outputs

- Request body includes all fields (null for unused fields)

#### Error Cases

| Condition | UI Behavior |
|-----------|-------------|
| Brand guidelines exceeds 1500 chars | Character counter turns red, submit disabled |
| Example thread exceeds 5000 chars | Character counter turns red, submit disabled |
| Max chars per tweet outside 200-280 | Input border turns red, submit disabled |

#### Acceptance Criteria

- [ ] "Advanced Options" section is collapsible, collapsed by default
- [ ] Brand guidelines textarea with 1500 char limit and counter
- [ ] Example threads list with add/remove functionality, max 3 items
- [ ] Style preferences controls with correct input types and defaults
- [ ] Form validation prevents submission with invalid values
- [ ] Request DTO includes all new fields
- [ ] Null values sent for unused optional fields
- [ ] UI matches existing design system (colors, spacing, fonts)
- [ ] Mobile-responsive (stack vertically on small screens)

---

### User Story 11 – Update API Documentation

#### Intent

As a developer, I want complete Swagger/OpenAPI documentation for all new fields so that I can integrate with the enhanced API.

#### Preconditions

- All new API fields are implemented
- Swagger is configured and accessible

#### User Flow (Step-by-Step)

1. Developer opens Swagger UI at `/swagger`
2. Developer views the `/api/v1/threads/generate` endpoint
3. Developer sees complete documentation for all request fields
4. Developer uses the example values to test the API

#### System Behavior

1. All new fields in `GenerateThreadRequestDto` have XML documentation comments
2. XML comments include:
   - `<summary>` with clear description
   - `<example>` with realistic example value
3. `StylePreferencesDto` is fully documented with all properties
4. Enum values are documented with descriptions
5. Swagger shows nested object structure correctly

#### Inputs

- None (documentation only)

#### Outputs

- Complete Swagger documentation

#### Error Cases

- None (documentation only)

#### Acceptance Criteria

- [ ] `GenerateThreadRequestDto.BrandGuidelines` has XML docs with example
- [ ] `GenerateThreadRequestDto.ExampleThreads` has XML docs with example array
- [ ] `GenerateThreadRequestDto.StylePreferences` has XML docs
- [ ] `StylePreferencesDto` and all its properties have XML docs
- [ ] Swagger UI renders all fields correctly with descriptions
- [ ] Swagger UI shows example values for all fields
- [ ] Swagger "Try it out" works with new fields

---

## Non-Goals (Explicitly Out of Scope)

- Saving brand guidelines to user profiles (no user accounts in MVP)
- Auto-detecting tone from example threads
- Importing example threads from URLs (only full text supported)
- Multiple language support
- Scheduling tweets
- Analytics or token usage dashboard (logging only)
- A/B testing different prompts
- Caching or templating brand guidelines
- Real-time character counting via AI

---

## Execution Notes for Dev Agent

1. **Implementation Order**: Implement in this order to manage dependencies:
   - User Stories 1, 2, 3 (new DTO fields) - can be parallel
   - User Story 8 (feedback limit update) - independent
   - User Story 4 (tone expansion) - independent
   - User Story 5 (system prompt) - independent
   - User Story 6 (user message construction) - depends on 1-4
   - User Story 7 (post-processing markers) - independent
   - User Story 9 (token logging) - independent
   - User Story 10 (frontend) - depends on 1-3
   - User Story 11 (documentation) - depends on 1-3

2. **Testing Strategy**:
   - Add unit tests for validation logic
   - Add unit tests for prompt construction with various field combinations
   - Add unit tests for tone expansion mapping
   - Add unit tests for post-processing with markers
   - Update integration tests to cover new fields
   - Verify backward compatibility with existing test requests

3. **File Changes Expected**:
   - `Api/Models/DTOs/ThreadDtos.cs` - new DTOs and properties
   - `Api/Services/ThreadGenerationService.cs` - prompt building, validation, post-processing
   - `Api/Services/IXaiChatClient.cs` - return type update
   - `Api/Services/XaiChatClient.cs` - parse token usage
   - `frontend/src/app/features/generator/` - form updates
   - `frontend/src/app/models/` - TypeScript interfaces

4. **Do NOT**:
   - Change authentication or user management
   - Add database schema changes for these features
   - Create new API endpoints (all changes are to existing endpoint)
   - Add external package dependencies unless absolutely necessary

5. **Character Encoding**:
   - The "🧵" emoji is 1 character in display but may be 2+ bytes in encoding
   - Use `.Length` for character count (C# string length), not byte count
   - Frontend JavaScript also uses `.length` for character count

6. **Backward Compatibility**:
   - All new fields are optional
   - Default behavior matches current behavior (except 260 char limit instead of 280)
   - Existing clients without new fields receive identical output quality
