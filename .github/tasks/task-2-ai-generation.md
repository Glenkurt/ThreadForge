# ThreadForge – Task 2: AI Thread Generation & Preview

## Global Constraints

- No authentication (single user session)
- Desktop-first UX (responsive not required for MVP)
- English UI only
- All errors must show user-readable messages
- All HTTP calls use Angular HttpClient
- Backend endpoint: `POST /api/v1/threads/generate`
- Rate limit: 20 requests per day per client (enforced by backend)
- p95 latency target: < 10 seconds

---

## Definitions (Shared Vocabulary)

- **Thread**: A sequence of 5-7 tweets intended to be posted in order on X/Twitter
- **Tweet Card**: A visual component displaying a single tweet with character count
- **Preview Panel**: A vertical list of tweet cards showing the generated thread
- **Empty State**: UI shown before any thread has been generated
- **Loading State**: UI shown while waiting for AI generation (spinner + message)
- **Tone**: Enum of writing styles: `casual`, `professional`, `humorous`, `educational`
- **Audience**: Target reader type: `developers`, `marketers`, `general`, `entrepreneurs`
- **Topic**: The subject matter for the thread (user-provided text)
- **Tweet Count**: Number of tweets to generate (5, 6, or 7)

---

## User Stories

### User Story 1 – Connect Form to Backend API

#### Intent

Enable the user to generate a Twitter thread by submitting the form and calling the backend AI service.

#### Preconditions

- Generator page is loaded and displays the form (task 1 completed)
- Form fields are valid: topic (non-empty), tone (selected), audience (selected), tweet count (selected)
- Backend is running and `/api/v1/threads/generate` endpoint is accessible

#### User Flow (Step-by-Step)

1. User fills in the topic field (e.g., "How to build a SaaS product")
2. User selects a tone from dropdown (e.g., "casual")
3. User selects an audience from dropdown (e.g., "entrepreneurs")
4. User selects tweet count from dropdown (e.g., "6")
5. User clicks "Generate Thread" button
6. System disables the button and shows loading state
7. System sends HTTP POST request to backend
8. System receives response with generated thread
9. System enables the button and displays the thread in preview panel

#### System Behavior

**Request:**
- Method: POST
- URL: `/api/v1/threads/generate`
- Headers: `Content-Type: application/json`
- Body:
```json
{
  "topic": "string (user input)",
  "tone": "string (selected value)",
  "audience": "string (selected value)",
  "tweetCount": number (selected value)
}
```

**Response (Success - 200 OK):**
```json
{
  "tweets": [
    "string (tweet 1, max 280 chars)",
    "string (tweet 2, max 280 chars)",
    ...
  ]
}
```

**Frontend Processing:**
- Remove console.log statement from generator component
- Create Angular service: `ThreadService` with method `generateThread(request: GenerateThreadRequest): Observable<GenerateThreadResponse>`
- Inject `HttpClient` into `ThreadService`
- Inject `ThreadService` into generator component
- On form submit, call `threadService.generateThread()` with form values
- Store response in component property: `generatedThread: string[] | null = null`
- Pass `generatedThread` to preview panel component

#### Inputs

**Form Values:**
- `topic`: string, min 1 char, max 500 chars, required
- `tone`: enum ["casual", "professional", "humorous", "educational"], required
- `audience`: enum ["developers", "marketers", "general", "entrepreneurs"], required
- `tweetCount`: number [5, 6, 7], required

**TypeScript Interface (create in `src/app/models/thread.model.ts`):**
```typescript
export interface GenerateThreadRequest {
  topic: string;
  tone: 'casual' | 'professional' | 'humorous' | 'educational';
  audience: 'developers' | 'marketers' | 'general' | 'entrepreneurs';
  tweetCount: 5 | 6 | 7;
}

export interface GenerateThreadResponse {
  tweets: string[];
}
```

#### Outputs

- HTTP request sent to backend
- Component property `generatedThread` populated with array of tweet strings
- Preview panel receives tweet data

#### Error Cases

Handled in User Story 5. For this story, assume happy path only.

#### Acceptance Criteria

- [ ] `ThreadService` is created in `src/app/services/thread.service.ts`
- [ ] Service injects `HttpClient` and has `generateThread()` method
- [ ] Generator component injects `ThreadService`
- [ ] Form submit calls `threadService.generateThread()` with correct payload
- [ ] Response is stored in component property `generatedThread`
- [ ] Console.log statement is removed from generator component
- [ ] TypeScript interfaces are defined in `src/app/models/thread.model.ts`

---

### User Story 2 – Display Loading State During Generation

#### Intent

Provide visual feedback to the user that thread generation is in progress and prevent duplicate submissions.

#### Preconditions

- User has filled the form
- User clicks "Generate Thread" button
- HTTP request is sent but response has not yet arrived

#### User Flow (Step-by-Step)

1. User clicks "Generate Thread" button
2. Button text changes to "Generating..."
3. Button becomes disabled (greyed out, not clickable)
4. Spinner icon appears next to button text
5. Loading message appears in preview panel area: "Generating your thread..."
6. (Wait for response)
7. On response arrival, button text reverts to "Generate Thread"
8. Button becomes enabled
9. Spinner disappears
10. Loading message disappears

#### System Behavior

**Component State Management:**
- Add boolean property: `isGenerating: boolean = false`
- On form submit: set `isGenerating = true` BEFORE HTTP call
- On HTTP success: set `isGenerating = false`
- On HTTP error: set `isGenerating = false`

**Button Behavior:**
- Text: `isGenerating ? 'Generating...' : 'Generate Thread'`
- Disabled: `[disabled]="isGenerating || !form.valid"`
- Show spinner icon when `isGenerating === true`

**Preview Panel Behavior:**
- If `isGenerating === true` AND `generatedThread === null`: show loading message
- If `isGenerating === false` AND `generatedThread === null`: show empty state
- If `isGenerating === false` AND `generatedThread !== null`: show tweet cards

#### Inputs

- `isGenerating`: boolean (component state)
- `generatedThread`: string[] | null (component state)

#### Outputs

- Button UI reflects generating state
- Preview panel shows loading message during generation

#### Error Cases

None (error handling covered in User Story 5).

#### Acceptance Criteria

- [ ] Component has `isGenerating` boolean property
- [ ] Property is set to `true` when HTTP call starts
- [ ] Property is set to `false` when HTTP call completes (success or error)
- [ ] Button text changes to "Generating..." when `isGenerating === true`
- [ ] Button is disabled when `isGenerating === true`
- [ ] Spinner icon (Angular Material spinner or CSS spinner) appears when `isGenerating === true`
- [ ] Preview panel shows "Generating your thread..." when `isGenerating === true` and no thread exists
- [ ] Manual test: clicking button multiple times during generation does not send duplicate requests

---

### User Story 3 – Display Empty State Before Generation

#### Intent

Show the user what to expect in the preview panel before they generate their first thread.

#### Preconditions

- Generator page is loaded
- No thread has been generated yet (`generatedThread === null`)
- No generation is in progress (`isGenerating === false`)

#### User Flow (Step-by-Step)

1. User lands on generator page
2. Preview panel displays empty state message
3. (User fills form and generates thread)
4. Empty state is replaced by tweet cards

#### System Behavior

**Empty State UI:**
- Display in preview panel area (right side of screen, 50% width)
- Center content vertically and horizontally
- Show icon: a generic "document" or "thread" icon (use Angular Material icon: `description` or custom SVG)
- Show text: "Your thread will appear here"
- Show subtext: "Fill out the form and click Generate Thread to get started"
- Style: light grey text, subtle icon, centered

**Conditional Rendering:**
```typescript
// In preview panel component or generator template
<div *ngIf="!isGenerating && !generatedThread" class="empty-state">
  <mat-icon>description</mat-icon>
  <h3>Your thread will appear here</h3>
  <p>Fill out the form and click Generate Thread to get started</p>
</div>
```

#### Inputs

- `generatedThread`: null
- `isGenerating`: false

#### Outputs

- Empty state UI displayed in preview panel

#### Error Cases

None.

#### Acceptance Criteria

- [ ] Preview panel shows empty state when no thread exists and not generating
- [ ] Empty state includes icon (Angular Material `description` icon or equivalent)
- [ ] Empty state includes heading: "Your thread will appear here"
- [ ] Empty state includes subtext: "Fill out the form and click Generate Thread to get started"
- [ ] Empty state is centered vertically and horizontally in preview panel
- [ ] Empty state disappears when thread is generated

---

### User Story 4 – Display Generated Thread as Tweet Cards

#### Intent

Show the generated thread as a series of visually distinct tweet cards that resemble actual Twitter posts.

#### Preconditions

- User has generated a thread
- `generatedThread` contains array of 5-7 tweet strings
- `isGenerating === false`

#### User Flow (Step-by-Step)

1. User generates a thread (User Story 1)
2. Response arrives with tweets array
3. Preview panel displays tweet cards in vertical sequence
4. Each tweet appears in its own card with index number and character count
5. User can scroll if cards exceed viewport height

#### System Behavior

**Tweet Card Component:**
- Create new component: `TweetCardComponent`
- Location: `src/app/components/tweet-card/tweet-card.component.ts`
- Inputs:
  - `@Input() tweet: string` (required)
  - `@Input() index: number` (required, 1-indexed for display)
- Template structure:
```html
<div class="tweet-card">
  <div class="tweet-header">
    <span class="tweet-number">{{ index }}</span>
    <span class="char-count" [class.over-limit]="charCount > 280">
      {{ charCount }}/280
    </span>
  </div>
  <div class="tweet-body">
    {{ tweet }}
  </div>
</div>
```

**Styling (tweet-card.component.css):**
```css
.tweet-card {
  background: #ffffff;
  border: 1px solid #e1e8ed;
  border-radius: 12px;
  padding: 16px;
  margin-bottom: 16px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.tweet-header {
  display: flex;
  justify-content: space-between;
  margin-bottom: 12px;
  font-size: 14px;
}

.tweet-number {
  font-weight: 600;
  color: #1da1f2;
}

.char-count {
  color: #657786;
}

.char-count.over-limit {
  color: #e0245e;
  font-weight: 600;
}

.tweet-body {
  color: #14171a;
  line-height: 1.5;
  white-space: pre-wrap;
  word-wrap: break-word;
}
```

**Preview Panel Component:**
- Create component: `ThreadPreviewComponent`
- Location: `src/app/components/thread-preview/thread-preview.component.ts`
- Inputs:
  - `@Input() tweets: string[] | null`
  - `@Input() isGenerating: boolean`
- Template:
```html
<div class="preview-panel">
  <!-- Empty State -->
  <div *ngIf="!isGenerating && !tweets" class="empty-state">
    <!-- Empty state content from User Story 3 -->
  </div>

  <!-- Loading State -->
  <div *ngIf="isGenerating" class="loading-state">
    <mat-spinner diameter="40"></mat-spinner>
    <p>Generating your thread...</p>
  </div>

  <!-- Tweet Cards -->
  <div *ngIf="!isGenerating && tweets" class="tweet-list">
    <app-tweet-card
      *ngFor="let tweet of tweets; let i = index"
      [tweet]="tweet"
      [index]="i + 1">
    </app-tweet-card>
  </div>
</div>
```

**Character Count Logic (in TweetCardComponent):**
```typescript
get charCount(): number {
  return this.tweet.length;
}
```

#### Inputs

- `tweets`: string[] (array of 5-7 tweets from API response)
- `index`: number (1-indexed position for display)

#### Outputs

- Preview panel displays `TweetCardComponent` for each tweet
- Each card shows tweet number, content, and character count
- Cards with >280 chars show red character count (validation indicator)

#### Error Cases

None (backend enforces 280 char limit, but visual indicator provided if exceeded).

#### Acceptance Criteria

- [ ] `TweetCardComponent` is created with tweet and index inputs
- [ ] Card displays tweet number (1-indexed)
- [ ] Card displays tweet content with proper line breaks
- [ ] Card displays character count in format "X/280"
- [ ] Character count turns red when >280 (class `over-limit` applied)
- [ ] `ThreadPreviewComponent` is created with tweets and isGenerating inputs
- [ ] Preview component renders list of tweet cards when tweets exist
- [ ] Cards appear in vertical sequence with spacing
- [ ] Preview panel is scrollable if content exceeds viewport
- [ ] Manual test: generate thread and verify all tweets display correctly

---

### User Story 5 – Handle Errors Gracefully

#### Intent

Inform the user of failures during thread generation with clear, actionable error messages.

#### Preconditions

- User submits form to generate thread
- HTTP request is sent to backend
- Backend returns error or request fails

#### User Flow (Step-by-Step)

**Scenario A: Rate Limit Exceeded (429)**
1. User clicks "Generate Thread"
2. Backend responds with 429 status
3. Error toast appears: "Daily limit reached. You can generate 20 threads per day. Try again tomorrow."
4. Button re-enables
5. Previous thread (if any) remains visible

**Scenario B: AI Generation Failure (500)**
1. User clicks "Generate Thread"
2. Backend responds with 500 status
3. Error toast appears: "Thread generation failed. Please try again."
4. Button re-enables
5. Previous thread (if any) remains visible

**Scenario C: Network Error (no response)**
1. User clicks "Generate Thread"
2. Network request times out or fails
3. Error toast appears: "Network error. Check your connection and try again."
4. Button re-enables
5. Previous thread (if any) remains visible

**Scenario D: Validation Error (400)**
1. User submits invalid data (should be prevented by form validation, but handle gracefully)
2. Backend responds with 400 status
3. Error toast appears: "Invalid request. Please check your inputs."
4. Button re-enables

#### System Behavior

**Error Handling in ThreadService:**
```typescript
generateThread(request: GenerateThreadRequest): Observable<GenerateThreadResponse> {
  return this.http.post<GenerateThreadResponse>('/api/v1/threads/generate', request)
    .pipe(
      catchError((error: HttpErrorResponse) => {
        // Error handling logic
        throw error; // Re-throw for component to handle
      })
    );
}
```

**Error Handling in Component:**
```typescript
generateThread(): void {
  if (this.form.invalid) return;

  this.isGenerating = true;

  this.threadService.generateThread(this.form.value)
    .subscribe({
      next: (response) => {
        this.generatedThread = response.tweets;
        this.isGenerating = false;
      },
      error: (error: HttpErrorResponse) => {
        this.isGenerating = false;
        this.handleError(error);
      }
    });
}

private handleError(error: HttpErrorResponse): void {
  let message: string;

  if (error.status === 429) {
    message = 'Daily limit reached. You can generate 20 threads per day. Try again tomorrow.';
  } else if (error.status === 500 || error.status === 503) {
    message = 'Thread generation failed. Please try again.';
  } else if (error.status === 400) {
    message = 'Invalid request. Please check your inputs.';
  } else if (error.status === 0) {
    // Network error
    message = 'Network error. Check your connection and try again.';
  } else {
    message = 'An unexpected error occurred. Please try again.';
  }

  this.showErrorToast(message);
}

private showErrorToast(message: string): void {
  // Use Angular Material Snackbar
  this.snackBar.open(message, 'Close', {
    duration: 5000,
    horizontalPosition: 'center',
    verticalPosition: 'top',
    panelClass: ['error-toast']
  });
}
```

**Toast Styling (styles.css or component styles):**
```css
.error-toast {
  background-color: #e0245e;
  color: #ffffff;
}

::ng-deep .error-toast .mat-simple-snackbar-action {
  color: #ffffff;
}
```

#### Inputs

- HTTP error responses with status codes: 400, 429, 500, 503, 0 (network error)

#### Outputs

- Error toast displayed at top center of screen
- Toast auto-dismisses after 5 seconds
- Toast has close button
- Button re-enables for retry
- Previous thread (if any) remains visible

#### Error Cases

All error cases are handled explicitly above.

#### Acceptance Criteria

- [ ] Component handles HTTP errors in subscribe error callback
- [ ] 429 error shows message: "Daily limit reached. You can generate 20 threads per day. Try again tomorrow."
- [ ] 500/503 errors show message: "Thread generation failed. Please try again."
- [ ] 400 error shows message: "Invalid request. Please check your inputs."
- [ ] Network error (status 0) shows message: "Network error. Check your connection and try again."
- [ ] Other errors show message: "An unexpected error occurred. Please try again."
- [ ] Error toast uses Angular Material Snackbar
- [ ] Toast appears at top center
- [ ] Toast auto-dismisses after 5 seconds
- [ ] Toast has "Close" button
- [ ] Toast has red background (#e0245e) and white text
- [ ] Button re-enables after error
- [ ] Previous thread remains visible after error (not cleared)
- [ ] Manual test: simulate each error scenario and verify correct message displays

---

### User Story 6 – Validate Tweet Character Limit

#### Intent

Ensure all generated tweets comply with Twitter's 280 character limit and provide visual feedback if they don't.

#### Preconditions

- Thread has been generated
- Tweets are displayed in preview panel

#### User Flow (Step-by-Step)

1. User generates thread
2. System receives tweets from backend
3. System checks each tweet's length
4. System displays character count for each tweet
5. If any tweet exceeds 280 chars, its character count appears in red
6. User can visually identify non-compliant tweets

#### System Behavior

**Backend Enforcement:**
- Backend already enforces 280 char limit (per context)
- Frontend validation is defensive and for UX feedback only

**Frontend Validation (in TweetCardComponent):**
```typescript
get charCount(): number {
  return this.tweet.length;
}

get isOverLimit(): boolean {
  return this.charCount > 280;
}
```

**Template (already defined in User Story 4):**
```html
<span class="char-count" [class.over-limit]="isOverLimit">
  {{ charCount }}/280
</span>
```

**Additional Check (optional, in ThreadPreviewComponent):**
```typescript
get hasOverLimitTweets(): boolean {
  return this.tweets?.some(tweet => tweet.length > 280) ?? false;
}
```

#### Inputs

- `tweet`: string (individual tweet content)

#### Outputs

- Character count displayed for each tweet
- Over-limit character counts styled in red
- Visual feedback for any non-compliant tweets

#### Error Cases

None (backend prevents over-limit tweets, frontend only provides visual indicator).

#### Acceptance Criteria

- [ ] Each tweet card displays character count in format "X/280"
- [ ] Character count turns red when tweet length > 280
- [ ] Red styling uses CSS class `.over-limit` with color `#e0245e`
- [ ] Manual test: if backend somehow returns >280 char tweet, it's visually identified
- [ ] Manual test: normal tweets (<= 280 chars) show grey character count

---

### User Story 7 – Ensure p95 Latency < 10 Seconds

#### Intent

Provide a responsive user experience by ensuring thread generation completes within 10 seconds for 95% of requests.

#### Preconditions

- Backend AI service is operational
- User submits thread generation request

#### User Flow (Step-by-Step)

1. User clicks "Generate Thread"
2. HTTP request sent to backend
3. Backend processes with Grok AI
4. Response returns to frontend
5. Total time from button click to display < 10 seconds (p95)

#### System Behavior

**HTTP Timeout Configuration:**
- Set HTTP timeout to 15 seconds (allows for p95 < 10s while handling edge cases)
- Configure in `ThreadService`:

```typescript
generateThread(request: GenerateThreadRequest): Observable<GenerateThreadResponse> {
  return this.http.post<GenerateThreadResponse>(
    '/api/v1/threads/generate',
    request,
    { timeout: 15000 } // 15 second timeout
  ).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.name === 'TimeoutError') {
        // Handle timeout specifically
        throw new Error('Request timed out');
      }
      throw error;
    })
  );
}
```

**Timeout Error Handling (update User Story 5 handleError method):**
```typescript
private handleError(error: any): void {
  let message: string;

  if (error.message === 'Request timed out') {
    message = 'Request took too long. Please try again.';
  } else if (error instanceof HttpErrorResponse) {
    // ... existing error handling
  } else {
    message = 'An unexpected error occurred. Please try again.';
  }

  this.showErrorToast(message);
}
```

**Performance Monitoring (optional but recommended):**
```typescript
generateThread(): void {
  if (this.form.invalid) return;

  this.isGenerating = true;
  const startTime = Date.now();

  this.threadService.generateThread(this.form.value)
    .subscribe({
      next: (response) => {
        const duration = Date.now() - startTime;
        console.log(`Thread generation took ${duration}ms`);
        this.generatedThread = response.tweets;
        this.isGenerating = false;
      },
      error: (error) => {
        const duration = Date.now() - startTime;
        console.log(`Thread generation failed after ${duration}ms`);
        this.isGenerating = false;
        this.handleError(error);
      }
    });
}
```

#### Inputs

- HTTP request to backend

#### Outputs

- Response received within 15 seconds (timeout threshold)
- Performance logged to console (for monitoring)
- Timeout error handled gracefully

#### Error Cases

- Request exceeds 15 seconds → timeout error → toast: "Request took too long. Please try again."

#### Acceptance Criteria

- [ ] HTTP timeout is set to 15 seconds in `ThreadService`
- [ ] Timeout errors are caught and handled
- [ ] Timeout error shows message: "Request took too long. Please try again."
- [ ] Performance timing is logged to console (start time, end time, duration)
- [ ] Manual test: normal requests complete within expected time
- [ ] Manual test: if request exceeds 15 seconds, timeout error appears

---

## Non-Goals (Explicitly Out of Scope)

- **Editing tweets**: User cannot edit generated tweets (future task)
- **Copying thread**: No copy-to-clipboard functionality yet (future task)
- **Regenerating individual tweets**: User cannot regenerate single tweets (future task)
- **Saving threads**: No persistence or history (future task)
- **Authentication**: No user accounts or login (global constraint)
- **Mobile responsive design**: Desktop-only for MVP (global constraint)
- **Accessibility**: ARIA labels and screen reader support (future improvement)
- **Analytics**: No tracking of generation success rates or latency metrics (future improvement)
- **Retry logic**: No automatic retries on failure (user must manually retry)
- **Multiple API keys**: Single Grok API key (backend handles)
- **A/B testing tones/audiences**: Fixed options only
- **Thread templates**: No saved templates or presets
- **Exporting**: No export to PDF, image, or other formats

---

## Execution Notes for Dev Agent

### File Structure to Create

```
src/app/
├── models/
│   └── thread.model.ts          (GenerateThreadRequest, GenerateThreadResponse interfaces)
├── services/
│   └── thread.service.ts        (HTTP service for thread generation)
├── components/
│   ├── tweet-card/
│   │   ├── tweet-card.component.ts
│   │   ├── tweet-card.component.html
│   │   └── tweet-card.component.css
│   └── thread-preview/
│       ├── thread-preview.component.ts
│       ├── thread-preview.component.html
│       └── thread-preview.component.css
```

### Implementation Order

1. **User Story 1**: Create models, service, integrate API
2. **User Story 2**: Add loading state management
3. **User Story 3**: Implement empty state UI
4. **User Story 4**: Create tweet card and preview components
5. **User Story 5**: Implement error handling with toast
6. **User Story 7**: Add timeout configuration
7. **User Story 6**: Validate character counts (already covered in User Story 4)

### Dependencies

- Angular Material: `MatSnackbar` for error toasts
- Angular Material: `MatSpinner` for loading state
- Angular Material: `MatIcon` for empty state icon
- HttpClient: already available in Angular

### Testing Checklist

After implementation, verify:

1. Form submission triggers HTTP POST to `/api/v1/threads/generate`
2. Button disables during generation
3. Loading spinner and message appear during generation
4. Empty state shows on initial load
5. Generated thread displays as tweet cards
6. Each tweet shows index and character count
7. Tweets >280 chars show red character count
8. 429 error shows rate limit message
9. 500 error shows generation failure message
10. Network error shows connection message
11. Error toast auto-dismisses after 5 seconds
12. Previous thread remains visible after error
13. Request timeout (>15s) shows timeout message
14. Console logs performance timing

### Known Backend Expectations

- **Endpoint**: `POST /api/v1/threads/generate`
- **Request body**: `{ topic, tone, audience, tweetCount }`
- **Response body**: `{ tweets: string[] }`
- **Rate limit**: 20 requests/day (returns 429 when exceeded)
- **Tweet limit**: Backend enforces 280 chars per tweet
- **Latency**: Backend targets fast responses (p95 < 10s handled by Grok AI)

### No Architectural Changes

- Use existing generator component (from task 1)
- Add service and child components only
- No routing changes required
- No new pages required
- No state management library (use component state only)

### Code Style

- Use TypeScript strict mode
- Use Angular reactive forms (already in use from task 1)
- Use Angular Material components for consistency
- Use CSS for styling (no SCSS unless already configured)
- Follow Angular style guide for file naming and structure
