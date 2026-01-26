# ThreadForge – User Stories & Execution Plan

## Global Constraints

- No authentication (unless explicitly specified)
- Single user session (no multi-tenant logic)
- English UI
- Desktop-first UX
- Errors must be user-readable
- All generated threads are editable before use

---

## Definitions (Shared Vocabulary)

- **Thread**: A sequence of tweets intended to be posted in order on X
- **Tweet**: One item in a thread
- **Tweet Limit**: Maximum characters allowed per tweet text (UI must support 200–280; default 260)
- **Draft**: A saved record of a generated thread in the database (table `ThreadDrafts`)
- **Thread History**: Global list of saved drafts (no user scoping in MVP)
- **Brand Guideline**: A single global text value stored in the database and optionally injected into generation requests
- **Profile Snapshot**: User-provided, copy/pasted real X profile data used for analysis (bio + recent tweets)
- **Honest Analysis (Option A)**: The system never invents profile facts; if required real data is missing, the system returns an “insufficient data” error
- **Edit Mode**: UI state where a tweet’s text can be modified
- **Copy Tweet**: Action that copies only one tweet’s text to the clipboard
- **Copy Full Thread**: Action that copies all tweets to the clipboard in order

---

## User Stories

---

### User Story 1 – Generate Thread (Grounded, Validated)

#### Intent

Let the user generate an editable X thread that matches their topic and constraints.

#### Preconditions

- Frontend is reachable.
- API is reachable.

#### User Flow (Step-by-Step)

1. User enters a Topic.
2. User selects a Tone from a fixed list.
3. User optionally enters Audience.
4. User sets Tweet Count.
5. User optionally sets Key Points (0–20).
6. User clicks “Generate thread”.

#### System Behavior

1. The system validates inputs locally before calling the API.
2. The system calls `POST /api/v1/threads/generate` with a JSON body matching `GenerateThreadRequestDto`.
3. The system disables the Generate button while the request is in progress.
4. On HTTP 200, the system displays the returned tweets in order and shows the returned `Id` and `CreatedAt`.
5. The system enters “Thread Result” view where the user can edit tweets.

#### Inputs

- topic: string, required, 1–1000 characters
- tone: enum string, optional, one of `[indie_hacker, professional, humorous, motivational, educational, provocative, storytelling, clear_practical]`
- audience: string, optional, 1–100 characters
- tweetCount: integer, required, 3–25
- keyPoints: string[], optional, 0–20 items, each item 1–200 characters

#### Outputs

- generate response: `GenerateThreadResponseDto`
  - id: guid
  - tweets: string[], length equals `tweetCount`
  - createdAt: UTC timestamp
  - provider: string
  - model: string

#### Error Cases

- topic missing → “Please enter a topic”
- topic > 1000 chars → “Topic must not exceed 1000 characters”
- tweetCount < 3 or > 25 → “Tweet count must be between 3 and 25”
- API 429 → “Rate limit exceeded. Please wait and try again.”
- API 500 → “Thread generation failed. Try again.”
- Network error → “Unable to reach the server. Check your connection and try again.”

#### Acceptance Criteria

- [ ] The frontend blocks submission when Topic is empty
- [ ] The API response contains exactly `tweetCount` tweets
- [ ] The UI renders each tweet in its own editable card
- [ ] On failure, the UI shows one user-readable message and does not show partial results

---

### User Story 2 – Persist Generated Threads to Database (No Silent Failure)

#### Intent

Ensure every generated thread is saved as a Draft so the user can find it later in history.

#### Preconditions

- Database is reachable by the API.
- EF Core migrations have been applied to the database.

#### User Flow (Step-by-Step)

1. User generates a thread via User Story 1.
2. User navigates to Thread History.
3. User sees the generated thread in the history list.

#### System Behavior

1. On `POST /api/v1/threads/generate`, the API persists a new row in `ThreadDrafts` using the same `Id` returned in `GenerateThreadResponseDto`.
2. The API stores:
	- `PromptJson`: the full request payload sent by the client
	- `OutputJson`: a JSON object containing `tweets` array exactly as returned
	- `CreatedAt`: UTC time
	- `Provider` and `Model`: values used for generation
3. If database persistence fails for any reason, the API returns HTTP 500 with `ErrorResponseDto` and does not return a successful `GenerateThreadResponseDto`.
4. The API logs the persistence failure with exception details.

#### Inputs

- generate request: `GenerateThreadRequestDto` (same as User Story 1)

#### Outputs

- On success: `GenerateThreadResponseDto` with an `Id` that exists in `ThreadDrafts`
- On persistence failure: HTTP 500 + `ErrorResponseDto` with message “Thread generation failed. Unable to save draft.”

#### Error Cases

- DB unreachable → “Thread generation failed. Unable to save draft.”
- Missing table/migrations → “Thread generation failed. Unable to save draft.”

#### Acceptance Criteria

- [ ] After a successful generate call, `GET /api/v1/threads/history?limit=20&offset=0` includes the generated `Id`
- [ ] If `SaveChangesAsync` fails, the API returns HTTP 500 (not HTTP 200)
- [ ] The API error response message matches exactly: “Thread generation failed. Unable to save draft.”

---

### User Story 3 – Thread History List

#### Intent

Let the user see previously generated drafts so they can reopen and reuse them.

#### Preconditions

- At least one draft exists in the database.

#### User Flow (Step-by-Step)

1. User opens the “History” view.
2. User sees a paginated list of drafts.
3. User clicks a history item.

#### System Behavior

1. The UI calls `GET /api/v1/threads/history?limit={limit}&offset={offset}`.
2. The API returns `ThreadHistoryListItemDto[]` ordered by `CreatedAt` descending.
3. The UI renders, per item:
	- CreatedAt
	- TopicPreview
	- TweetCount
	- FirstTweetPreview
4. When the user clicks an item, the UI navigates to a detail view (User Story 4).

#### Inputs

- limit: integer, optional, default 20, min 1, max 100
- offset: integer, optional, default 0, min 0

#### Outputs

- history list: array of `ThreadHistoryListItemDto`

#### Error Cases

- limit < 1 → “Limit must be at least 1”
- limit > 100 → “Limit must not exceed 100”
- offset < 0 → “Offset must be 0 or greater”

#### Acceptance Criteria

- [ ] The API returns drafts ordered newest-first
- [ ] The UI shows at most `limit` items
- [ ] Clicking an item navigates to its detail view using its `Id`

---

### User Story 4 – Thread History Detail (Reopen a Draft)

#### Intent

Let the user reopen a previously generated draft and edit/copy it.

#### Preconditions

- A draft exists in the database.

#### User Flow (Step-by-Step)

1. User selects a draft from History.
2. The draft detail view loads.
3. User edits tweets locally.
4. User copies tweets.

#### System Behavior

1. The UI calls `GET /api/v1/threads/history/{id}`.
2. On HTTP 200, the UI displays the returned tweets in editable cards.
3. Edits are local only in MVP (no API call is made to persist edits).

#### Inputs

- id: guid, required

#### Outputs

- history detail: `ThreadHistoryDetailDto`

#### Error Cases

- id not found → “Thread not found”

#### Acceptance Criteria

- [ ] The UI loads a draft by id and renders all tweets
- [ ] The UI allows editing without re-calling generation
- [ ] A missing id shows “Thread not found”

---

### User Story 5 – Multi-Line Tweet Editing (Client-Side)

#### Intent

Enable the user to insert line breaks inside a tweet during edit mode for better readability.

#### Preconditions

- A thread with at least one tweet is displayed.

#### User Flow (Step-by-Step)

1. User clicks the edit control for a tweet.
2. User places the cursor inside the tweet editor.
3. User presses Enter.
4. User saves the tweet.

#### System Behavior

1. The system renders a multi-line text area for the tweet in Edit Mode.
2. Pressing Enter inserts a newline character and does not submit the form.
3. The system enforces the configured Tweet Limit by counting newline characters as characters.

#### Inputs

- tweetText: string, required, max = Tweet Limit

#### Outputs

- updatedTweetText: string (in client state)

#### Error Cases

- tweetText exceeds Tweet Limit → “Tweet exceeds character limit”

#### Acceptance Criteria

- [ ] Enter inserts a newline in Edit Mode
- [ ] Saving preserves newline characters
- [ ] The UI blocks saving when length exceeds Tweet Limit

---

### User Story 6 – Copy Tweet and Copy Full Thread

#### Intent

Let the user quickly copy content for posting on X.

#### Preconditions

- A thread is displayed.

#### User Flow (Step-by-Step)

1. User clicks “Copy” on a single tweet.
2. User clicks “Copy full thread”.

#### System Behavior

1. Copy Tweet copies exactly the tweet text (including newline characters) to clipboard.
2. Copy Full Thread copies all tweets in order, separated by two newline characters (`\n\n`).
3. After copying, the UI shows “Copied” confirmation for 2 seconds.

#### Inputs

- tweetIndex: integer, required

#### Outputs

- clipboard content: string

#### Error Cases

- clipboard permission denied → “Unable to copy to clipboard. Please copy manually.”

#### Acceptance Criteria

- [ ] Copy Tweet copies only the selected tweet
- [ ] Copy Full Thread preserves tweet order and separation

---

### User Story 7 – Global Brand Guidelines (Save + Load + Inject)

#### Intent

Allow the user to define one global brand guideline text that influences generation.

#### Preconditions

- API is reachable.

#### User Flow (Step-by-Step)

1. User opens “Brand Guidelines”.
2. User enters guideline text.
3. User clicks “Save”.
4. User generates a thread.

#### System Behavior

1. The UI loads existing guidelines via `GET /api/v1/brand-guidelines`.
2. The UI saves guidelines via `PUT /api/v1/brand-guidelines` with body `{ "text": "..." }`.
3. On successful save, the UI displays the saved text.
4. When generating a thread, the UI injects the saved guideline text into `GenerateThreadRequestDto.brandGuidelines`.
5. If the saved guideline text is empty, the UI sends `brandGuidelines: null`.

#### Inputs

- text: string, optional, trimmed, max 1500 characters

#### Outputs

- brand guideline: `BrandGuidelineDto`

#### Error Cases

- text > 1500 chars → “Brand guideline must not exceed 1500 characters”

#### Acceptance Criteria

- [ ] Saved guidelines re-load after refresh
- [ ] Generated thread requests include brandGuidelines when non-empty

---

### User Story 8 – Honest Profile Brand Analysis (Option A: Real Data Required)

#### Intent

Generate a brand description that is grounded in real, user-provided profile content and never invents facts.

#### Preconditions

- User has access to the target X profile page to copy/paste the bio and recent tweets.

#### User Flow (Step-by-Step)

1. User enters a username (with or without `@`).
2. User pastes the profile bio.
3. User pastes 5–30 recent tweets (one per line).
4. User clicks “Analyze brand”.
5. User reviews the produced brand description.

#### System Behavior

1. The UI validates that the username is 1–15 characters after removing leading `@`.
2. The UI validates that profile bio is non-empty.
3. The UI validates that at least 5 recent tweets are provided.
4. The API endpoint `POST /api/v1/profiles/analyze` is updated to accept a request body with:
	- `username`: string
	- `profileBio`: string
	- `recentTweets`: string[]
5. The API performs analysis only using `profileBio` and `recentTweets` content.
6. The API sets `tweetCount` in the response equal to `recentTweets.length`.
7. The API response uses the existing `ProfileAnalysisResponseDto` shape.

#### Inputs

- username: string, required, 1–15 characters after trimming and stripping leading `@`
- profileBio: string, required, 1–400 characters
- recentTweets: string[], required, 5–30 items, each 1–500 characters

#### Outputs

- profile analysis: `ProfileAnalysisResponseDto`
  - username: normalized without `@`
  - profileUrl: `https://x.com/{username}`
  - analyzedAt: UTC timestamp
  - tweetCount: integer
  - brandDescription: `BrandDescriptionDto`

#### Error Cases

- invalid username → “Invalid username format”
- missing bio → “Please paste the profile bio”
- fewer than 5 tweets → “Please paste at least 5 recent tweets”
- more than 30 tweets → “Please paste no more than 30 tweets”
- AI returns invalid JSON → “Brand analysis failed. Try again.”

#### Acceptance Criteria

- [ ] The API rejects requests missing bio or with < 5 tweets
- [ ] The analysis response sets tweetCount equal to number of provided tweets
- [ ] The analysis text does not mention fabricated stats (no made-up follower counts, posting frequency, or engagement numbers)

---

### User Story 9 – Apply Profile Analysis to Generation

#### Intent

Let the user turn the analyzed brand description into generation guidance.

#### Preconditions

- A successful profile analysis exists in the UI.

#### User Flow (Step-by-Step)

1. User runs “Analyze brand”.
2. User clicks “Use this voice for generation”.
3. User generates a thread.

#### System Behavior

1. The UI converts the analysis into a single text block (max 1500 characters) and sets it as the global Brand Guideline (User Story 7).
2. If the generated guideline text would exceed 1500 characters, the UI truncates to 1497 characters and appends `...`.
3. Generation requests include this guideline in `brandGuidelines`.

#### Inputs

- brandDescription: `BrandDescriptionDto`

#### Outputs

- saved global brand guideline text

#### Error Cases

- analysis missing → “Run brand analysis first”

#### Acceptance Criteria

- [ ] Clicking “Use this voice for generation” updates the Brand Guidelines text area
- [ ] Subsequent generation calls include `brandGuidelines` populated

---

### User Story 10 – Deterministic Output Validation for Thread Generation

#### Intent

Prevent weak outputs by enforcing hard constraints (count, length, numbering) and failing fast when the model violates them.

#### Preconditions

- Thread generation endpoint is available.

#### User Flow (Step-by-Step)

1. User sets Tweet Count and Style Preferences.
2. User generates a thread.

#### System Behavior

1. The API validates the model output before returning it:
	- `tweets.length` equals requested `tweetCount`
	- each tweet length is `<= stylePreferences.maxCharsPerTweet` when provided; otherwise `<= 260`
2. If `stylePreferences.useNumbering == true`, each tweet ends with `" {i}/{n}"` where i is 1-based.
3. If validation fails, the API returns HTTP 500 with `ErrorResponseDto` message “Thread generation failed. Try again.”
4. The API logs the validation failure details.

#### Inputs

- stylePreferences.useNumbering: boolean, optional
- stylePreferences.maxCharsPerTweet: integer, optional, 200–280

#### Outputs

- validated `GenerateThreadResponseDto`

#### Error Cases

- invalid output count/length/numbering → “Thread generation failed. Try again.”

#### Acceptance Criteria

- [ ] The API never returns a response with wrong tweet count
- [ ] The API never returns a tweet longer than the configured max

---

### User Story 11 – Automatic Database Migration on API Startup

#### Intent

Eliminate “generated but not saved” behavior caused by missing migrations.

#### Preconditions

- API is started.
- Database connection string is configured.

#### User Flow (Step-by-Step)

1. Operator starts the API.
2. API becomes ready.

#### System Behavior

1. On startup, the API applies EF Core migrations to the configured database.
2. If migrations fail, the API process exits and logs an error.
3. The health endpoint reports not-ready until migrations complete.

#### Inputs

- connection string: `DefaultConnection`

#### Outputs

- database schema includes tables required by the app (`ThreadDrafts`, `BrandGuidelines`, auth tables if present)

#### Error Cases

- migration failure → API does not start serving traffic; logs contain migration exception

#### Acceptance Criteria

- [ ] Starting the API on an empty database results in required tables created
- [ ] If the DB is unreachable, the API does not claim readiness

---

## Non-Goals (Explicitly Out of Scope)

- Scheduling tweets
- Posting directly to X
- Analytics (impressions, likes, follower counts)
- Multi-user accounts and authentication flows in the UI
- Persisting edited drafts back to the database (edits are local only in MVP)
- Automatic scraping of X profile/tweets (analysis uses user-provided pasted text in MVP)

---

## Execution Notes for Dev Agent

- No architectural changes without user approval
- Follow the file exactly as written
- Do not infer missing features
- Do not generalize beyond this MVP

The file must be register in .github/tasks

#### Outputs

- updatedTweetText: string with one or more newline characters

#### Error Cases

- tweetText length exceeds 280 → “Tweet exceeds 280 characters. Shorten it before saving.”

#### Acceptance Criteria

- [ ] Enter inserts a newline within the tweet in Edit Mode.
- [ ] Enter does not exit Edit Mode.
- [ ] Saved tweet preserves newline characters.
- [ ] Character limit validation includes newline characters.

---

### User Story 2 – Multi-Line Tweet Display Formatting

#### Intent

Present tweet text with breathing space using line breaks for easy reading.

#### Preconditions

- A thread with at least one tweet is displayed.
- At least one tweet contains newline characters.

#### User Flow (Step-by-Step)

1. User views the thread output list.
2. User reads a tweet that contains line breaks.

#### System Behavior

1. The system renders each tweet as a separate visual block with a visible tweet number.
2. The system preserves newline characters in the tweet text and renders them as line breaks.
3. The system maintains consistent spacing between lines within a tweet (one blank line is not inserted automatically).

#### Inputs

- tweetText: string, may include newline characters

#### Outputs

- renderedTweet: text with visible line breaks inside the tweet block

#### Error Cases

- None

#### Acceptance Criteria

- [ ] Tweets render with visible line breaks where newline characters exist.
- [ ] Each tweet is displayed as its own numbered block.

---

### User Story 3 – Copy Single Tweet Correctly

#### Intent

Allow the user to copy the text of a specific tweet using its own copy button.

#### Preconditions

- A thread with at least two tweets is displayed.
- Each tweet has its own “Copy” button.

#### User Flow (Step-by-Step)

1. User clicks “Copy” on tweet N.
2. User pastes into another app.

#### System Behavior

1. The system copies only tweet N’s text to the clipboard.
2. The system includes newline characters exactly as shown in the tweet.
3. The system does not copy any other tweet.

#### Inputs

- tweetIndex: integer, required
- tweetText: string, required

#### Outputs

- clipboardText: string equal to the selected tweet’s text

#### Error Cases

- Clipboard write fails → “Copy failed. Please try again.”

#### Acceptance Criteria

- [ ] Clicking “Copy” on tweet N copies only tweet N’s text.
- [ ] Copy preserves line breaks.
- [ ] Copy does not default to the first tweet.

---

### User Story 4 – Persist Brand Guideline (Single Global Record)

#### Intent

Let the user save and reuse a single global Brand Guideline for thread generation.

#### Preconditions

- The Advanced Options area is visible.
- The Brand Guideline field is visible and editable.

#### User Flow (Step-by-Step)

1. User enters text into the Brand Guideline field.
2. User triggers the existing “Generate thread” action.
3. User leaves and returns to the app.
4. User opens Advanced Options.

#### System Behavior

1. On generate, the system saves the Brand Guideline text to the database as a single global record.
2. On app load, the system reads the global Brand Guideline and populates the field.
3. If no record exists, the field is empty.
4. The system uses the stored Brand Guideline in generation requests when the field is not empty.

#### Inputs

- brandGuideline: string, optional, max 2000 characters

#### Outputs

- storedBrandGuideline: string

#### Error Cases

- brandGuideline exceeds 2000 characters → “Brand guideline is too long (max 2000 characters).”
- Database save fails → “Could not save brand guideline. Try again.”
- Database read fails → “Could not load brand guideline. Try again.”

#### Acceptance Criteria

- [ ] Brand Guideline persists across page refresh.
- [ ] Brand Guideline is prefilled from the database on app load.
- [ ] Generation requests include the Brand Guideline when present.

---

### User Story 5 – Standalone Brand Guideline Component

#### Intent

Provide a reusable Brand Guideline UI component that can be moved into a settings screen later.

#### Preconditions

- Advanced Options area is rendered in the current generation screen.

#### User Flow (Step-by-Step)

1. User opens Advanced Options.
2. User sees the Brand Guideline field rendered by a standalone component.
3. User edits the Brand Guideline text.

#### System Behavior

1. The system renders the Brand Guideline input via a dedicated component, not inline markup.
2. The component exposes an input value and change event for the parent to use.
3. The component displays the current stored value.

#### Inputs

- brandGuideline: string, optional

#### Outputs

- brandGuidelineChanged: string emitted to parent on edit

#### Error Cases

- None

#### Acceptance Criteria

- [ ] Brand Guideline field is implemented as a standalone component.
- [ ] Parent screen receives updates from the component on every edit.

---

## Non-Goals (Explicitly Out of Scope)

- Scheduling tweets
- Analytics
- Multiple languages
- User accounts
- Multi-tenant brand guideline storage
- Mobile-first or responsive redesign

---

## Execution Notes for Dev Agent

- No architectural changes without user approval
- Follow the file exactly as written
- Do not infer missing features
- Do not generalize beyond this MVP