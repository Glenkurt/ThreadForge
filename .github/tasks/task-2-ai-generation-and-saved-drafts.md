# ThreadForge – User Stories & Execution Plan (Task 2)

## Global Constraints

- No authentication (unless explicitly specified)
- Single user session (no multi-tenant logic)
- English UI
- Desktop-first UX
- Errors must be user-readable
- All generated threads are editable before use
- Anonymous client identification via browser storage
- Rate limit: 20 generations per client per day
- Backend persists drafts automatically on generation

---

## Definitions (Shared Vocabulary)

- **Thread**: A sequence of 5–12 tweets intended to be posted in order on X/Twitter.
- **Tweet**: A single text block with a maximum length of 280 characters.
- **Tweet Card**: A visual UI component that displays a single tweet in a styled container matching the design spec.
- **Draft**: A persisted record containing the user's input (topic, tone, audience, tweet count) and the generated thread output.
- **Client ID**: A unique identifier (UUID v4 format) generated and stored in browser `localStorage` to identify anonymous users across sessions.
- **X-Client-Id Header**: An HTTP header sent with all API requests containing the Client ID value.
- **Preview Panel**: The right column panel that displays generated tweet cards or loading/error states.
- **Forge Panel**: The left column panel containing thread input controls (from Task 1).
- **Recent Drafts Page**: A separate route (`/drafts`) that lists all saved drafts for the current client.
- **Draft Card**: A visual UI component that displays a summary of a saved draft (timestamp, topic preview, first tweet preview).
- **Hook**: The first tweet in a thread, designed to capture attention and encourage reading.
- **CTA (Call To Action)**: The final tweet in a thread, prompting an action or engagement.
- **Rate Limit Error**: HTTP 429 status returned when the client has exceeded 20 generations in a 24-hour period.
- **Loading State**: A visual state shown in the Preview Panel while the AI is generating the thread.
- **Skeleton Loader**: An animated placeholder that mimics the tweet card layout during loading.
- **Thread Connector**: A vertical line connecting tweet cards to visually indicate they are part of a sequence.

---

## User Stories

---

### User Story 1 – Generate Client ID And Persist In Browser Storage

#### Intent

The system identifies anonymous users across sessions for rate limiting and draft persistence without requiring authentication.

#### Preconditions

- Task 1 is complete (Generator Page with Forge Panel inputs).
- Browser supports `localStorage`.

#### User Flow (Step-by-Step)

1. User opens the app for the first time.
2. User refreshes the page.
3. User clears browser data and reopens the app.

#### System Behavior

- **Step 1: First visit**
  - On app initialization (in `main.ts` or root `AppComponent`), check for `localStorage` key `threadforge_client_id`.
  - If key does not exist:
    - Generate a new UUID v4 (using standard UUID generation: `crypto.randomUUID()` or a polyfill).
    - Store the value in `localStorage` with key `threadforge_client_id`.
  - If key exists:
    - Read the existing Client ID from `localStorage`.
- **Step 2: Refresh**
  - The same Client ID is read from `localStorage`.
  - No new ID is generated.
- **Step 3: After clearing data**
  - `localStorage` is empty.
  - A new Client ID is generated and stored.
  - The new Client ID is treated as a different anonymous user by the backend.

#### Inputs

- None (automatic behavior).

#### Outputs

- A Client ID string stored in `localStorage` with key `threadforge_client_id`.
- Client ID format: UUID v4 (e.g., `550e8400-e29b-41d4-a716-446655440000`).

#### Error Cases

- Browser does not support `localStorage` → fallback to in-memory storage for the current session only; warn user in console: `localStorage not available. Client ID will not persist across sessions.`
- UUID generation fails → throw application error and halt initialization.

#### Acceptance Criteria

- [ ] On first visit, a new Client ID is generated and stored in `localStorage`.
- [ ] On subsequent visits, the same Client ID is reused from `localStorage`.
- [ ] Client ID is a valid UUID v4 string.
- [ ] Client ID persists across page refreshes.
- [ ] If `localStorage` is unavailable, the app continues with in-memory fallback and logs a warning.

---

### User Story 2 – Add HTTP Interceptor To Include X-Client-Id Header

#### Intent

All API requests to the backend include the anonymous Client ID for rate limiting and draft association.

#### Preconditions

- User Story 1 is complete (Client ID exists).

#### User Flow (Step-by-Step)

1. User triggers any API request (e.g., clicking Generate).
2. Request is sent to the backend.

#### System Behavior

- An Angular HTTP interceptor is registered in the app providers.
- The interceptor runs for every outgoing HTTP request.
- The interceptor:
  - Reads the Client ID from `localStorage` (key: `threadforge_client_id`).
  - Adds an HTTP header: `X-Client-Id: {clientId}` to the request.
  - If Client ID is not found in `localStorage`, use the in-memory fallback value.
  - If no Client ID exists at all, send header with empty string: `X-Client-Id: ` (backend will fallback to IP).
- The modified request is sent to the backend.

#### Inputs

- Client ID from `localStorage` or in-memory fallback.

#### Outputs

- All HTTP requests include the `X-Client-Id` header.

#### Error Cases

- None (interceptor is passive; empty value is acceptable).

#### Acceptance Criteria

- [ ] All API requests include the `X-Client-Id` header.
- [ ] Header value matches the Client ID from `localStorage`.
- [ ] If Client ID is missing, header is sent with an empty string.
- [ ] Interceptor does not block or fail requests.

---

### User Story 3 – Wire Generate Button To Call POST /api/v1/threads/generate

#### Intent

Users click Generate and the app sends their inputs to the backend for AI thread generation.

#### Preconditions

- User Stories 1–2 are complete (Client ID exists and is sent via header).
- Task 1 is complete (Forge Panel inputs are functional).

#### User Flow (Step-by-Step)

1. User fills in Topic (required) and optionally Tone, Audience, Tweet Count.
2. User clicks the "Generate" button.
3. User waits for the API response.

#### System Behavior

- **Step 1: Form validation**
  - Topic must be non-empty after trimming and ≤ 120 characters.
  - Audience (if provided) must be ≤ 80 characters after trimming.
  - Tweet Count must be between 5 and 12 (validated by slider constraints).
  - Tone is one of: `null`, `"indie_hacker"`, `"educational"`, `"provocative"`, `"direct"`.
- **Step 2: API call**
  - On Generate button click:
    - Disable the Generate button immediately.
    - Change button text to: `Generating...`
    - Send `POST` request to `/api/v1/threads/generate` with JSON body:
      ```json
      {
        "topic": "string (trimmed, 1-120 chars)",
        "tone": "string | null (one of: indie_hacker, educational, provocative, direct)",
        "audience": "string | null (trimmed, 0-80 chars)",
        "tweetCount": 7,
        "keyPoints": null,
        "feedback": null
      }
      ```
    - `keyPoints` is always `null` in Task 2 (no UI for this field yet).
    - `feedback` is always `null` in Task 2 (regenerate feature is out of scope).
    - Include `X-Client-Id` header (from interceptor).
    - Set request timeout to 30 seconds.
- **Step 3: Handle response**
  - On success (HTTP 200):
    - Parse response JSON (see User Story 4 for response handling).
  - On error (4xx/5xx):
    - Parse error JSON (see User Story 5 for error handling).

#### Inputs

- `topic`: string, required, trimmed length 1–120
- `tone`: `null` | `"indie_hacker"` | `"educational"` | `"provocative"` | `"direct"`
- `audience`: string | null, trimmed length 0–80
- `tweetCount`: integer, required, range 5–12

#### Outputs

- HTTP `POST` request to `/api/v1/threads/generate`.
- Request body is valid JSON matching `GenerateThreadRequestDto`.
- `X-Client-Id` header is included.

#### Error Cases

- Topic is empty → Generate button is disabled (validation from Task 1).
- Topic > 120 chars → Generate button is disabled (validation from Task 1).
- Network timeout (>30s) → Show error message: `Request timed out. Please try again.`
- Network failure (no connection) → Show error message: `Network error. Check your connection and try again.`

#### Acceptance Criteria

- [ ] Generate button triggers API call when Topic is valid.
- [ ] Request body matches the exact JSON schema specified.
- [ ] Button is disabled and shows `Generating...` text during request.
- [ ] Request includes `X-Client-Id` header.
- [ ] Request has a 30-second timeout.

---

### User Story 4 – Display Generated Thread In Preview Panel

#### Intent

Users see the generated thread as a vertical stack of tweet cards with visual connectors.

#### Preconditions

- User Story 3 is complete (API call succeeds).

#### User Flow (Step-by-Step)

1. User clicks Generate and waits.
2. API returns a successful response.
3. User views the generated tweet cards in the Preview Panel.

#### System Behavior

- **API Response Schema (HTTP 200)**:
  ```json
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "tweets": [
      "First tweet text (hook)...",
      "Second tweet text...",
      "Third tweet text...",
      "...",
      "Final tweet text (CTA)..."
    ],
    "createdAt": "2026-01-21T10:30:00.000Z",
    "provider": "xai",
    "model": "grok-2-latest"
  }
  ```
- **Response fields**:
  - `id`: UUID string (draft ID in the backend database).
  - `tweets`: Array of strings, length 5–12, each tweet ≤ 280 characters.
  - `createdAt`: ISO 8601 timestamp string.
  - `provider`: String (e.g., `"xai"`).
  - `model`: String (e.g., `"grok-2-latest"`).
- **On successful response**:
  - Store the response in component state.
  - Re-enable the Generate button.
  - Reset button text to: `Generate`
  - Clear the Preview Panel empty-state content (from Task 1).
  - Render the tweet cards in the Preview Panel.
- **Tweet Card rendering**:
  - For each tweet in the `tweets` array:
    - Render a `.tweet-card` component styled per `.github/Docs/designSpec.md` section 8.1.
    - Tweet card visual structure:
      - A circular avatar placeholder (40px diameter, background: `var(--bg-app)`, border: `1px solid var(--border-subtle)`).
      - A username placeholder text: `@YourHandle` in `--text-secondary`, font size `--text-sm`.
      - The tweet text in `--text-primary`, font size `--text-base`, line height `1.5`.
      - A character count indicator in bottom-right corner: `{charCount}/280` in `--text-tertiary`, font size `--text-xs`.
    - Tweet card styling:
      - Background: `var(--bg-panel-elevated)`
      - Border: `1px solid var(--border-subtle)`
      - Border-radius: `var(--radius-lg)`
      - Padding: `var(--space-4)`
      - Margin-bottom: `var(--space-6)` (for spacing between cards).
  - **Thread Connector** (visual line between tweets):
    - Render a vertical line between each pair of consecutive tweet cards.
    - Connector styling (from `.github/Docs/designSpec.md` section 8.2):
      - Position: `absolute`
      - Left: `28px` (centered on avatar placeholder)
      - Width: `1.5px`
      - Height: `24px` (spans the gap between cards)
      - Background: `#262626`
    - The connector appears below every tweet card except the last one.
- **Tweet numbering**:
  - Display a small numeric label on each tweet card: `1`, `2`, `3`, etc.
  - Label styling:
    - Position: top-right corner of the tweet card.
    - Font size: `--text-xs`
    - Color: `--text-tertiary`
    - Padding: `var(--space-1)`

#### Inputs

- `GenerateThreadResponseDto` from API (JSON object).

#### Outputs

- Preview Panel displays:
  - N tweet cards (where N = length of `tweets` array).
  - Thread connectors between cards.
  - Each tweet card shows tweet text, character count, avatar placeholder, and username placeholder.

#### Error Cases

- API returns malformed JSON → Show error message: `Invalid response from server. Please try again.`
- `tweets` array is empty → Show error message: `No tweets were generated. Please try again.`
- `tweets` array contains tweet > 280 chars → Display the tweet as-is and highlight character count in red if > 280.

#### Acceptance Criteria

- [ ] On successful API response, Preview Panel displays tweet cards.
- [ ] Each tweet card matches the visual design specified in `.github/Docs/designSpec.md`.
- [ ] Thread connectors appear between all consecutive tweet cards except after the last one.
- [ ] Tweet cards are numbered 1, 2, 3, etc.
- [ ] Character count is displayed for each tweet.
- [ ] Generate button is re-enabled after response is rendered.
- [ ] Empty-state content (from Task 1) is hidden when tweets are displayed.

---

### User Story 5 – Show Loading State During Thread Generation

#### Intent

Users understand that the AI is working and the request has not frozen.

#### Preconditions

- User Story 3 is complete (API call is initiated).

#### User Flow (Step-by-Step)

1. User clicks Generate.
2. User sees a loading animation in the Preview Panel.
3. API responds (success or error).
4. Loading animation is replaced with tweet cards or an error message.

#### System Behavior

- **Immediately after Generate button is clicked**:
  - Clear the Preview Panel (remove empty-state or previous tweets).
  - Display a loading state in the Preview Panel.
- **Loading state visual design**:
  - Show exactly 5 skeleton loaders (matching the expected number of tweet cards).
  - Each skeleton loader mimics a tweet card layout:
    - Background: gradient shimmer animation (from `.github/Docs/designSpec.md` section 8.4):
      ```css
      background: linear-gradient(
        90deg,
        #1a1a1a 25%,
        #222 37%,
        #1a1a1a 63%
      );
      background-size: 200% 100%;
      animation: shimmer 1.4s infinite ease-in-out;
      ```
      ```css
      @keyframes shimmer {
        0% { background-position: 200% 0; }
        100% { background-position: -200% 0; }
      }
      ```
    - Border: `1px solid var(--border-subtle)`
    - Border-radius: `var(--radius-lg)`
    - Padding: `var(--space-4)`
    - Margin-bottom: `var(--space-6)`
    - Height: ~120px (approximate height of a tweet card).
  - Show thread connectors between skeleton loaders (same styling as real connectors).
  - No interactive elements are rendered during loading.
- **After API responds**:
  - Remove all skeleton loaders.
  - Render tweet cards (User Story 4) or error message (User Story 6).

#### Inputs

- None (automatic behavior on API call).

#### Outputs

- Preview Panel displays 5 skeleton loaders with shimmer animation.
- Thread connectors between skeleton loaders.

#### Error Cases

- None (loading state is purely visual).

#### Acceptance Criteria

- [ ] Loading state appears immediately when Generate is clicked.
- [ ] Skeleton loaders match the visual design specified.
- [ ] Shimmer animation runs continuously until API responds.
- [ ] Skeleton loaders are replaced with tweet cards or error message after API response.
- [ ] Thread connectors appear between skeleton loaders.

---

### User Story 6 – Handle API Errors With User-Friendly Messages

#### Intent

Users understand what went wrong and how to resolve it when thread generation fails.

#### Preconditions

- User Story 3 is complete (API call is initiated).

#### User Flow (Step-by-Step)

1. User clicks Generate.
2. API returns an error response (4xx or 5xx).
3. User sees an error message in the Preview Panel.
4. User takes corrective action (if applicable).

#### System Behavior

- **On API error response**:
  - Stop loading animation (remove skeleton loaders).
  - Re-enable the Generate button.
  - Reset button text to: `Generate`
  - Display an error message in the Preview Panel.
- **Error message visual design**:
  - Centered vertically and horizontally in the Preview Panel.
  - Background: `var(--bg-panel-elevated)`
  - Border: `1px solid var(--danger)` (red border for error state)
  - Border-radius: `var(--radius-lg)`
  - Padding: `var(--space-6)`
  - Max-width: `480px`
  - Icon: Red circle with exclamation mark (or Unicode `⚠️`).
  - Error title: Bold text in `--text-primary`, font size `--text-md`.
  - Error message: Regular text in `--text-secondary`, font size `--text-base`.
- **Error response schema** (from backend):
  ```json
  {
    "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
    "title": "One or more validation errors occurred.",
    "status": 400,
    "errors": {
      "Topic": ["Topic is required"],
      "TweetCount": ["TweetCount must be between 3 and 25"]
    }
  }
  ```
  or for rate limit:
  ```json
  {
    "type": "https://httpstatuses.com/429",
    "title": "Too Many Requests",
    "status": 429,
    "detail": "Rate limit exceeded. Try again later."
  }
  ```
- **Error mapping** (backend status → user message):
  - **400 Bad Request**:
    - Extract first error message from `errors` object.
    - Display title: `Invalid Request`
    - Display message: `{firstErrorMessage}` (e.g., "Topic is required").
  - **429 Too Many Requests**:
    - Display title: `Daily Limit Reached`
    - Display message: `You've used all 20 thread generations for today. Come back tomorrow!`
  - **500 Internal Server Error**:
    - Display title: `Generation Failed`
    - Display message: `Something went wrong on our end. Please try again in a moment.`
  - **503 Service Unavailable**:
    - Display title: `Service Unavailable`
    - Display message: `The AI service is temporarily unavailable. Please try again later.`
  - **Network timeout** (client-side, >30s):
    - Display title: `Request Timed Out`
    - Display message: `The request took too long. Please try again.`
  - **Unknown error** (any other status or no response):
    - Display title: `Unexpected Error`
    - Display message: `An unexpected error occurred. Please try again.`
- **User action after error**:
  - User can immediately click Generate again (button is re-enabled).
  - For 429 errors, Generate button remains enabled (user can try again, but will receive 429 again until rate limit window resets).

#### Inputs

- HTTP error response from API (4xx/5xx status code).

#### Outputs

- Error message displayed in Preview Panel.
- Generate button is re-enabled.

#### Error Cases

- Malformed error JSON → Display "Unexpected Error" fallback message.

#### Acceptance Criteria

- [ ] 400 errors display the first validation message from the backend.
- [ ] 429 errors display: `You've used all 20 thread generations for today. Come back tomorrow!`
- [ ] 500 errors display: `Something went wrong on our end. Please try again in a moment.`
- [ ] 503 errors display: `The AI service is temporarily unavailable. Please try again later.`
- [ ] Network timeout displays: `The request took too long. Please try again.`
- [ ] Unknown errors display: `An unexpected error occurred. Please try again.`
- [ ] Error message has red border and warning icon.
- [ ] Generate button is re-enabled after error is displayed.

---

### User Story 7 – Add Recent Drafts Route And Navigation

#### Intent

Users can navigate to a separate page to view all their previously generated threads.

#### Preconditions

- User Story 1 is complete (Client ID exists).

#### User Flow (Step-by-Step)

1. User is on the Generator Page (`/generator`).
2. User clicks a "Recent Drafts" link in the app navigation.
3. User lands on the Recent Drafts page (`/drafts`).
4. User clicks "Back to Generator" link.
5. User returns to the Generator Page.

#### System Behavior

- **Add `/drafts` route**:
  - Create a new standalone Angular component: `DraftsComponent`.
  - Register route: `{ path: 'drafts', component: DraftsComponent }`.
  - Route is publicly accessible (no auth guard).
- **Add navigation UI**:
  - In the Generator Page layout, add a top navigation bar (or a fixed link in the Forge Panel header).
  - Navigation bar styling:
    - Background: `var(--bg-panel)`
    - Border-bottom: `1px solid var(--border-subtle)`
    - Height: `64px`
    - Padding: `var(--space-4) var(--space-8)`
    - Flexbox layout: space-between.
  - Navigation bar content:
    - Left side: App title text: `ThreadForge` in `--text-primary`, font size `--text-xl`, bold.
    - Right side: Link to `/drafts` with text: `Recent Drafts` in `--text-secondary`, font size `--text-base`, hover color `--accent`.
  - On the Drafts Page:
    - Display the same navigation bar.
    - Right side link changes to: `← Back to Generator` (links to `/generator`).
- **Routing behavior**:
  - Clicking "Recent Drafts" navigates to `/drafts` without page reload (Angular Router).
  - Clicking "Back to Generator" navigates to `/generator` without page reload.

#### Inputs

- None (navigation links).

#### Outputs

- `/drafts` route is accessible.
- Navigation bar is present on both `/generator` and `/drafts`.

#### Error Cases

- None.

#### Acceptance Criteria

- [ ] `/drafts` route loads the `DraftsComponent`.
- [ ] Navigation bar displays on both Generator Page and Drafts Page.
- [ ] "Recent Drafts" link navigates to `/drafts`.
- [ ] "Back to Generator" link navigates to `/generator`.
- [ ] Navigation is client-side (no full page reload).

---

### User Story 8 – Fetch And Display List Of Recent Drafts

#### Intent

Users see all their previously generated threads in reverse chronological order.

#### Preconditions

- User Story 7 is complete (`/drafts` route exists).
- User has generated at least one thread (so at least one draft exists in the backend).

#### User Flow (Step-by-Step)

1. User navigates to `/drafts`.
2. User sees a loading state.
3. API returns the list of drafts.
4. User sees a list of draft cards.

#### System Behavior

- **On `/drafts` page load**:
  - Send `GET` request to `/api/v1/threads/drafts`.
  - Include `X-Client-Id` header (from interceptor).
  - Set request timeout to 10 seconds.
- **API Request**:
  - Method: `GET`
  - URL: `/api/v1/threads/drafts`
  - Headers: `X-Client-Id: {clientId}`
  - Query params: None (backend filters by `X-Client-Id` automatically).
- **API Response Schema (HTTP 200)**:
  ```json
  {
    "drafts": [
      {
        "id": "550e8400-e29b-41d4-a716-446655440000",
        "topic": "How to build in public as a solo founder",
        "createdAt": "2026-01-21T10:30:00.000Z",
        "firstTweet": "Building in public is scary. Here's why you should do it anyway...",
        "tweetCount": 7
      },
      {
        "id": "6f3e5c20-a19b-41d4-b716-556655440001",
        "topic": "5 lessons from launching my SaaS",
        "createdAt": "2026-01-20T14:15:00.000Z",
        "firstTweet": "I launched my SaaS 6 months ago. Here's what I learned...",
        "tweetCount": 10
      }
    ]
  }
  ```
  - `drafts`: Array of draft summary objects, sorted by `createdAt` descending (newest first).
  - Each draft summary contains:
    - `id`: UUID string (draft ID).
    - `topic`: String (user's original topic input).
    - `createdAt`: ISO 8601 timestamp string.
    - `firstTweet`: String (first tweet text, for preview).
    - `tweetCount`: Integer (number of tweets in the thread).
- **Rendering draft list**:
  - If `drafts` array is empty:
    - Display empty state message: `You haven't generated any threads yet. Go to the Generator to create your first thread!`
    - Show a button: `Go to Generator` that navigates to `/generator`.
  - If `drafts` array is not empty:
    - Render a vertical list of draft cards.
    - Each draft card displays:
      - Date/time label: Format `createdAt` as relative time (e.g., "2 hours ago", "Yesterday", "Jan 20") using a library like `date-fns` or `Intl.RelativeTimeFormat`.
      - Topic: Display `topic` text in bold, `--text-primary`, font size `--text-md`, truncate if > 60 chars with ellipsis.
      - First tweet preview: Display `firstTweet` text in `--text-secondary`, font size `--text-sm`, line clamp 2 lines with ellipsis.
      - Tweet count badge: Small badge in bottom-right corner showing `{tweetCount} tweets` in `--text-tertiary`, font size `--text-xs`.
    - Draft card styling:
      - Background: `var(--bg-panel-elevated)`
      - Border: `1px solid var(--border-subtle)`
      - Border-radius: `var(--radius-lg)`
      - Padding: `var(--space-4)`
      - Margin-bottom: `var(--space-4)`
      - Cursor: `pointer`
      - Hover state: Border color changes to `var(--accent)`, slight shadow: `var(--shadow-sm)`.
    - Clicking a draft card navigates to `/generator` with the draft ID as a query param: `/generator?draft={draftId}` (see User Story 9).
- **Loading state**:
  - While API request is in progress, show 3 skeleton loaders with shimmer animation (same design as tweet skeleton loaders).
- **Error handling**:
  - If API returns error (4xx/5xx), display error message: `Failed to load drafts. Please refresh the page.`
  - If network timeout (>10s), display error message: `Request timed out. Please refresh the page.`

#### Inputs

- None (automatic on page load).

#### Outputs

- List of draft cards displayed in `/drafts` page.
- Each draft card shows topic, timestamp, first tweet preview, and tweet count.

#### Error Cases

- API returns empty `drafts` array → Show empty state message.
- API returns error → Show error message: `Failed to load drafts. Please refresh the page.`
- Network timeout → Show error message: `Request timed out. Please refresh the page.`

#### Acceptance Criteria

- [ ] On `/drafts` page load, API request is sent to `/api/v1/threads/drafts`.
- [ ] Loading state shows 3 skeleton loaders.
- [ ] Draft cards are displayed in reverse chronological order (newest first).
- [ ] Each draft card shows topic, relative timestamp, first tweet preview, and tweet count.
- [ ] Draft cards are clickable and have hover state.
- [ ] Empty state is shown when no drafts exist.
- [ ] Error state is shown when API call fails.

---

### User Story 9 – Load A Draft Back Into The Generator

#### Intent

Users can restore a previously generated thread into the Generator Page to view, edit, or regenerate it.

#### Preconditions

- User Story 8 is complete (drafts list is functional).

#### User Flow (Step-by-Step)

1. User is on `/drafts` page.
2. User clicks a draft card.
3. User is navigated to `/generator?draft={draftId}`.
4. Generator Page loads the draft data.
5. User sees the Forge Panel inputs pre-filled with the original inputs.
6. User sees the Preview Panel showing the generated tweet cards from the draft.

#### System Behavior

- **On draft card click**:
  - Navigate to `/generator?draft={draftId}` using Angular Router.
- **On Generator Page load with `?draft={draftId}` query param**:
  - Extract `draftId` from route query params.
  - Send `GET` request to `/api/v1/threads/drafts/{draftId}`.
  - Include `X-Client-Id` header (from interceptor).
  - Set request timeout to 10 seconds.
- **API Request**:
  - Method: `GET`
  - URL: `/api/v1/threads/drafts/{draftId}`
  - Headers: `X-Client-Id: {clientId}`
- **API Response Schema (HTTP 200)**:
  ```json
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "topic": "How to build in public as a solo founder",
    "tone": "indie_hacker",
    "audience": "indie hackers",
    "tweetCount": 7,
    "tweets": [
      "Building in public is scary. Here's why you should do it anyway...",
      "Transparency builds trust. People root for real stories, not polished brands.",
      "...",
      "What are you building? Share below 👇"
    ],
    "createdAt": "2026-01-21T10:30:00.000Z",
    "provider": "xai",
    "model": "grok-2-latest"
  }
  ```
  - Fields:
    - `id`: UUID string (draft ID).
    - `topic`: String (original topic input).
    - `tone`: String | null (original tone input).
    - `audience`: String | null (original audience input).
    - `tweetCount`: Integer (original tweet count input).
    - `tweets`: Array of strings (generated tweets).
    - `createdAt`: ISO 8601 timestamp.
    - `provider`: String.
    - `model`: String.
- **On successful draft load**:
  - Pre-fill Forge Panel inputs:
    - Set Topic input value to `topic`.
    - Set Tone selector to `tone` (or "Default" if `tone` is `null`).
    - Set Audience input value to `audience` (or empty string if `null`).
    - Set Tweet Count slider value to `tweetCount`.
  - Display tweet cards in Preview Panel:
    - Render tweet cards exactly as in User Story 4.
    - Use the `tweets` array from the draft response.
  - Remove the `?draft={draftId}` query param from the URL (replace state) to avoid re-loading the draft on refresh.
- **Loading state**:
  - While API request is in progress, show loading skeleton loaders in Preview Panel.
  - Forge Panel inputs remain enabled but empty during loading.
- **Error handling**:
  - If API returns 404 (draft not found or wrong client):
    - Display error message in Preview Panel: `Draft not found. It may have been deleted or you don't have access.`
  - If API returns 4xx/5xx error:
    - Display error message in Preview Panel: `Failed to load draft. Please try again.`
  - If network timeout (>10s):
    - Display error message in Preview Panel: `Request timed out. Please try again.`

#### Inputs

- Draft ID from query param: `?draft={draftId}`.

#### Outputs

- Forge Panel inputs are pre-filled with original values.
- Preview Panel displays tweet cards from the draft.
- URL is updated to remove `?draft={draftId}` query param after loading.

#### Error Cases

- Draft ID is invalid UUID → API returns 404 → Show error message.
- Draft does not belong to current Client ID → API returns 404 → Show error message.
- API error → Show error message: `Failed to load draft. Please try again.`

#### Acceptance Criteria

- [ ] Clicking a draft card navigates to `/generator?draft={draftId}`.
- [ ] Generator Page loads draft data via API call to `/api/v1/threads/drafts/{draftId}`.
- [ ] Forge Panel inputs are pre-filled with draft's original inputs.
- [ ] Preview Panel displays tweet cards from draft.
- [ ] Query param `?draft={draftId}` is removed from URL after draft is loaded.
- [ ] 404 errors display: `Draft not found. It may have been deleted or you don't have access.`
- [ ] Other errors display: `Failed to load draft. Please try again.`

---

### User Story 10 – Add Backend Endpoint GET /api/v1/threads/drafts

#### Intent

Backend provides a list of all drafts belonging to the requesting client.

#### Preconditions

- ThreadDraft entity exists in the database (already implemented in backend).
- User Story 2 is complete (X-Client-Id header is sent from frontend).

#### User Flow (Step-by-Step)

N/A (backend implementation).

#### System Behavior

- **Create new endpoint in `ThreadsController.cs`**:
  - Method: `GET`
  - Route: `/api/v1/threads/drafts`
  - No authentication required.
- **Implementation**:
  - Extract `clientId` from `X-Client-Id` header.
  - If header is missing or invalid, fallback to IP address.
  - Query database: `SELECT * FROM ThreadDrafts WHERE ClientId = @clientId ORDER BY CreatedAt DESC LIMIT 50`.
  - Limit results to 50 most recent drafts to avoid unbounded queries.
  - For each draft:
    - Deserialize `PromptJson` to extract `topic`, `tone`, `audience`, `tweetCount`.
    - Deserialize `OutputJson` to extract `tweets` array.
    - Extract first tweet from `tweets` array for preview.
    - Build a `DraftSummaryDto` object.
  - Return response as JSON array.
- **Response DTO** (create new DTO: `DraftSummaryDto`):
  ```csharp
  public sealed record DraftSummaryDto(
      Guid Id,
      string Topic,
      DateTime CreatedAt,
      string FirstTweet,
      int TweetCount);
  ```
- **Response format**:
  ```json
  {
    "drafts": [
      {
        "id": "550e8400-e29b-41d4-a716-446655440000",
        "topic": "How to build in public",
        "createdAt": "2026-01-21T10:30:00.000Z",
        "firstTweet": "Building in public is scary...",
        "tweetCount": 7
      }
    ]
  }
  ```

#### Inputs

- `X-Client-Id` header: string (UUID or fallback to IP).

#### Outputs

- HTTP 200 response with JSON array of draft summaries.

#### Error Cases

- Database query fails → HTTP 500 with error message: `Failed to retrieve drafts.`
- `PromptJson` or `OutputJson` is malformed → Skip the draft (do not fail entire request).

#### Acceptance Criteria

- [ ] `GET /api/v1/threads/drafts` returns list of drafts for the requesting client.
- [ ] Drafts are sorted by `CreatedAt` descending (newest first).
- [ ] Response includes `id`, `topic`, `createdAt`, `firstTweet`, `tweetCount` for each draft.
- [ ] Maximum 50 drafts are returned.
- [ ] Malformed draft records are skipped without failing the entire request.

---

### User Story 11 – Add Backend Endpoint GET /api/v1/threads/drafts/{id}

#### Intent

Backend provides the full details of a specific draft for loading into the Generator.

#### Preconditions

- ThreadDraft entity exists in the database.
- User Story 2 is complete (X-Client-Id header is sent from frontend).

#### User Flow (Step-by-Step)

N/A (backend implementation).

#### System Behavior

- **Create new endpoint in `ThreadsController.cs`**:
  - Method: `GET`
  - Route: `/api/v1/threads/drafts/{id}`
  - Route parameter: `id` (Guid).
  - No authentication required.
- **Implementation**:
  - Extract `clientId` from `X-Client-Id` header.
  - If header is missing or invalid, fallback to IP address.
  - Query database: `SELECT * FROM ThreadDrafts WHERE Id = @id AND ClientId = @clientId`.
  - If no draft is found → return HTTP 404.
  - If draft is found:
    - Deserialize `PromptJson` to extract `topic`, `tone`, `audience`, `tweetCount`.
    - Deserialize `OutputJson` to extract `tweets` array.
    - Build a `DraftDetailDto` object.
  - Return response as JSON.
- **Response DTO** (create new DTO: `DraftDetailDto`):
  ```csharp
  public sealed record DraftDetailDto(
      Guid Id,
      string Topic,
      string? Tone,
      string? Audience,
      int TweetCount,
      string[] Tweets,
      DateTime CreatedAt,
      string Provider,
      string Model);
  ```
- **Response format**:
  ```json
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "topic": "How to build in public",
    "tone": "indie_hacker",
    "audience": "indie hackers",
    "tweetCount": 7,
    "tweets": ["tweet 1", "tweet 2", "..."],
    "createdAt": "2026-01-21T10:30:00.000Z",
    "provider": "xai",
    "model": "grok-2-latest"
  }
  ```

#### Inputs

- Route parameter: `id` (Guid).
- `X-Client-Id` header: string (UUID or fallback to IP).

#### Outputs

- HTTP 200 response with full draft details.
- HTTP 404 if draft is not found or does not belong to the client.

#### Error Cases

- Draft ID is not found → HTTP 404 with message: `Draft not found.`
- Draft belongs to different client → HTTP 404 with message: `Draft not found.`
- `PromptJson` or `OutputJson` is malformed → HTTP 500 with message: `Failed to load draft.`

#### Acceptance Criteria

- [ ] `GET /api/v1/threads/drafts/{id}` returns full draft details.
- [ ] Response includes all original inputs (`topic`, `tone`, `audience`, `tweetCount`) and generated `tweets`.
- [ ] HTTP 404 is returned if draft is not found or belongs to a different client.
- [ ] HTTP 500 is returned if draft data is corrupted.

---

## Non-Goals (Explicitly Out of Scope)

- Editing individual tweets inline (deferred to Task 3).
- Copy-to-clipboard functionality for tweets or threads (deferred to Task 3).
- Regenerate feature with feedback input (deferred to Task 3).
- Deleting drafts from the UI (deferred to later).
- Searching or filtering drafts (deferred to later).
- Pagination for drafts list (50 drafts max is sufficient for MVP).
- Exporting drafts as JSON or CSV.
- Sharing drafts via public links.
- Draft versioning or history.
- User accounts or authentication.
- Cross-device draft sync (requires authentication).
- Analytics on draft usage.

---

## Execution Notes for Dev Agent

- No architectural changes without user approval.
- Follow the file exactly as written.
- Do not infer missing features.
- Do not generalize beyond this MVP.
- All API contracts are specified exactly as the backend expects (see `ThreadsController.cs` and `ThreadDtos.cs`).
- The backend already persists drafts in `ThreadGenerationService.GenerateAsync` (no changes needed to that logic).
- You MUST implement two new backend endpoints: `GET /api/v1/threads/drafts` and `GET /api/v1/threads/drafts/{id}`.
- You MUST implement two new DTOs: `DraftSummaryDto` and `DraftDetailDto`.
- All frontend HTTP requests MUST use Angular `HttpClient`.
- All error messages MUST match the exact text specified in each user story.
- All visual styling MUST reference design tokens from `.github/Docs/designSpec.md`.
- Do not add toast notifications or snackbars in Task 2 (deferred to Task 3).
- Do not add "Copy All" or "Copy Tweet" buttons in Task 2 (deferred to Task 3).
