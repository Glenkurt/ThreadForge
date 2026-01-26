# ThreadForge – User Stories & Execution Plan

## Global Constraints

- No authentication changes (existing auth system unchanged)
- Single user session (no multi-tenant logic)
- English UI
- Desktop-first UX
- Errors must be user-readable
- All generated threads are editable before use

---

## Definitions (Shared Vocabulary)

- **Topic**: The main free-text input the user writes to describe what the thread should be about
- **Generated Thread**: The tweets returned by the AI after a successful generation call
- **Thread History**: A global (single-user MVP) list of all generated threads stored in the database and accessible via the UI
- **History Card**: A UI card summarizing one saved generated thread (topic preview, date/time, tweet count)
- **Thread Detail**: A UI view showing a single saved thread’s full tweets with editing and copy actions
- **Overlap Bug**: A UI issue where the tweet character counter and tweet edit action buttons visually overlap each other

---

## User Stories

---

### User Story 1 – Increase Topic Input Character Limit to 1000

#### Intent

As a user, I want to paste a much longer topic description so that I can provide enough context without being blocked by a low character limit.

#### Preconditions

- Frontend is running
- Thread generation page is accessible
- Backend thread generation endpoint exists at `POST /api/v1/threads/generate`

#### User Flow (Step-by-Step)

1. User navigates to the thread generation page
2. User types or pastes a long topic description (up to 1000 characters)
3. User generates a thread

#### System Behavior

1. Frontend topic input accepts up to 1000 characters
2. Frontend shows a live character counter in the format `{current}/{max}` with max set to `1000`
3. If topic exceeds 1000 characters, the Generate action is disabled and an inline error is shown
4. Backend accepts `topic` up to 1000 characters
5. If `topic` exceeds 1000 characters, backend returns a 400 error with the exact message defined below

#### Inputs

- `topic`: string, required, min 1 character, max 1000 characters

#### Outputs

- Standard thread generation response (unchanged)

#### Error Cases

- Topic empty → `"Please enter a topic"`
- Topic exceeds 1000 characters → `"Topic must not exceed 1000 characters"`

#### Acceptance Criteria

- [ ] Frontend topic input `maxlength` is 1000
- [ ] Frontend validation blocks submit if topic length is 0 or >1000
- [ ] Backend validation rejects `topic.Length > 1000` with HTTP 400 and message `"Topic must not exceed 1000 characters"`
- [ ] Swagger/OpenAPI documentation (if present for topic) reflects max 1000
- [ ] Existing clients sending topic ≤ 500 continue to work unchanged

---

### User Story 2 – Fix Tweet Edit UI Overlap (Char Counter vs Edit Buttons)

#### Intent

As a user, I want the tweet editor UI to be visually clear so that controls never overlap and I can confidently edit multiple tweets.

#### Preconditions

- A thread is generated and displayed in the editor
- Tweet cards are visible and editable

#### User Flow (Step-by-Step)

1. User views a generated thread
2. User hovers or focuses a tweet card to reveal edit controls
3. User edits tweet text
4. User verifies the character counter remains readable and not covered

#### System Behavior

1. Tweet-level character counter and tweet-level action buttons never overlap visually at desktop widths ≥ 1024px
2. Tweet-level character counter and tweet-level action buttons never overlap visually at tablet widths 768px–1023px
3. On small widths (<768px), tweet-level action buttons move to a new row under the header area, preserving readability
4. Hover/focus states do not cause layout jumps that hide text or counters

#### Inputs

- Edited tweet text (existing behavior)

#### Outputs

- Updated tweet text rendered in the editor (existing behavior)

#### Error Cases

- None (UI layout behavior only)

#### Acceptance Criteria

- [ ] Character counter remains fully visible while edit controls are visible
- [ ] No action button overlaps the tweet number, header metadata, or character counter
- [ ] UI remains usable with 20+ tweets displayed (no overlapping due to height constraints)
- [ ] Visual regression check: overlapping does not occur in Chrome and Safari

---

### User Story 3 – Persist Every Successfully Generated Thread in the Database

#### Intent

As a user, I want every AI-generated thread to be saved automatically so that I can access it later from History.

#### Preconditions

- Backend API is running
- Database is reachable
- Thread generation endpoint exists at `POST /api/v1/threads/generate`

#### User Flow (Step-by-Step)

1. User generates a thread
2. System saves the generation result to the database after the AI returns a successful response
3. User later opens History to view it

#### System Behavior

1. On every successful generation response, the backend persists a new record representing the generation
2. The persisted record contains at minimum:
   - `createdAt` timestamp
   - `request` payload (topic, tweetCount, audience, tone, keyPoints, feedback, and any advanced options)
   - `response` payload (tweets)
3. If persistence fails after the AI returns successfully, the API still returns the generated thread to the caller and logs the persistence failure

#### Inputs

- Thread generation request body (existing)

#### Outputs

- Standard thread generation response (unchanged)

#### Error Cases

- Database write failure after AI success → user still receives thread; no user-facing error is shown for persistence

#### Acceptance Criteria

- [ ] A successful generation creates exactly one new database record
- [ ] Stored record includes request and response payloads
- [ ] Persistence failures do not block generation success responses
- [ ] History list endpoints (User Story 4) can retrieve the saved record

---

### User Story 4 – Add Global Thread History API (List + Detail)

#### Intent

As a user, I want a History page backed by an API so that I can browse and open any previously generated thread.

#### Preconditions

- Backend API is running
- Database contains at least one saved generated thread

#### User Flow (Step-by-Step)

1. User opens History page in the frontend
2. Frontend calls the History list API
3. User clicks a history card
4. Frontend calls the History detail API
5. Thread detail is displayed

#### System Behavior

1. Backend exposes a list endpoint:
   - `GET /api/v1/threads/history?limit={limit}&offset={offset}`
2. List endpoint returns records newest-first
3. `limit` defaults to 20
4. `limit` is clamped to a maximum of 100
5. Backend exposes a detail endpoint:
   - `GET /api/v1/threads/history/{id}`
6. If `{id}` does not exist, return 404 with a user-readable error message

#### Inputs

- `limit`: integer, optional, default 20, min 1, max 100
- `offset`: integer, optional, default 0, min 0
- `id`: GUID/identifier, required

#### Outputs

- List response returns an array of items with:
  - `id`
  - `createdAt`
  - `topicPreview` (first 120 characters of topic, computed server-side from stored request)
  - `tweetCount`
  - `firstTweetPreview` (first 120 characters of tweet 1)
- Detail response returns:
  - `id`
  - `createdAt`
  - full stored request payload
  - full stored tweets array

#### Error Cases

- Invalid `limit` (<1) → 400 `"Limit must be at least 1"`
- Invalid `limit` (>100) → 400 `"Limit must not exceed 100"`
- Invalid `offset` (<0) → 400 `"Offset must be 0 or greater"`
- Unknown `id` → 404 `"Thread not found"`

#### Acceptance Criteria

- [ ] `GET /api/v1/threads/history` returns newest-first records
- [ ] Pagination works via `limit` and `offset`
- [ ] `GET /api/v1/threads/history/{id}` returns the full thread
- [ ] Error messages match exactly
- [ ] History is global for this MVP (no filtering by user/client)

---

### User Story 5 – Add History Navigation Link in the Main Menu

#### Intent

As a user, I want a clear entry point to History so that I can access saved threads from anywhere.

#### Preconditions

- Frontend is running
- Main navigation/header is visible

#### User Flow (Step-by-Step)

1. User looks at the main menu
2. User clicks `History`
3. User is routed to the History page

#### System Behavior

1. A new `History` link is added to the main menu/navigation
2. Clicking `History` routes to `/history`
3. The active navigation state highlights `History` when the user is on `/history` or `/history/{id}`

#### Inputs

- None

#### Outputs

- Route change to the History view

#### Error Cases

- None

#### Acceptance Criteria

- [ ] `History` link is visible in the primary nav
- [ ] Route `/history` exists
- [ ] Active nav state works for list and detail pages

---

### User Story 6 – History List UI with Card-Based Layout

#### Intent

As a user, I want to scan my past threads quickly so I can reopen the right one without reading everything.

#### Preconditions

- Frontend is running
- History API is available

#### User Flow (Step-by-Step)

1. User opens `/history`
2. System loads the first page of results
3. User scrolls and loads more results
4. User clicks a card to open the thread

#### System Behavior

1. History list displays items as cards in a responsive grid:
   - Desktop: 2 columns
   - Tablet: 1 column
2. Each card displays:
   - Created timestamp in local time
   - Topic preview (max 120 characters, ellipsis)
   - Tweet count
   - First tweet preview (max 120 characters, ellipsis)
3. Clicking a card navigates to `/history/{id}`
4. Loading states:
   - While loading: show a skeleton or placeholder rows (at least 3 cards)
   - On empty list: show `"No threads yet"`

#### Inputs

- None (UI triggers API calls)

#### Outputs

- Rendered list of cards

#### Error Cases

- API unavailable or 5xx → show `"Unable to load history. Please try again."`
- API returns 401/403 → show `"You are not authorized to view history."` (even though MVP is global, handle generically)

#### Acceptance Criteria

- [ ] Cards render with the required fields
- [ ] Empty state message matches exactly
- [ ] Error state message matches exactly
- [ ] Clicking a card opens the correct detail page

---

### User Story 7 – History Detail UI with Editable Tweets and Copy Actions

#### Intent

As a user, I want to reopen a past thread, edit it, and copy it so that History is directly usable.

#### Preconditions

- Frontend is running
- At least one history item exists

#### User Flow (Step-by-Step)

1. User opens `/history/{id}`
2. System loads the full thread
3. User edits one or more tweets
4. User copies the full thread

#### System Behavior

1. Detail view displays:
   - Created timestamp
   - Topic (full)
   - Full tweet list
2. Tweets are editable in the same UI pattern as the generator tweet editor
3. Copy actions:
   - `Copy full thread` copies tweets separated by two newlines
   - Each tweet has a `Copy` action that copies only that tweet
4. If the thread cannot be loaded (404), show `"Thread not found"`

#### Inputs

- Edited tweets (client-side)

#### Outputs

- Clipboard content

#### Error Cases

- 404 on detail fetch → `"Thread not found"`
- Clipboard API failure → `"Copy failed. Please try again."`

#### Acceptance Criteria

- [ ] Thread loads and renders for a valid id
- [ ] 404 shows exact message `"Thread not found"`
- [ ] Copy full thread uses the required separator
- [ ] Per-tweet copy works
- [ ] Clipboard failure shows exact message

---

## Non-Goals (Explicitly Out of Scope)

- User accounts, login-based history, or per-user filtering
- Deleting history items
- Searching history
- Tagging or favoriting threads
- Scheduling or posting to X/Twitter
- Analytics dashboards

---

## Execution Notes for Dev Agent

- No architectural changes without user approval
- Follow the file exactly as written
- Do not infer missing features
- Do not generalize beyond this MVP
