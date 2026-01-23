# ThreadForge – Task 4: QA & Launch

## Global Constraints

- No authentication (single user session)
- English UI only
- Desktop-first UX (responsive stacking on mobile < 960px)
- All errors must show user-readable messages via Material snackbar
- Rate limit: 20 thread generations per day per client
- p95 latency target: < 10 seconds for generation
- Design tokens from `.github/Docs/designSpec.md` must be used

---

## Definitions (Shared Vocabulary)

- **Rate Limit**: Maximum 20 thread generations per anonymous user per 24-hour period
- **Client ID**: Unique identifier stored in localStorage to track rate limits (fallback: IP address)
- **429 Response**: HTTP status code indicating rate limit exceeded
- **Toast**: A temporary notification shown at top-center of the screen for 3-5 seconds
- **Accessible**: Keyboard navigable, screen reader compatible, proper ARIA labels
- **E2E Test**: End-to-end test that simulates real user interactions

---

## User Stories

---

### User Story 1 – Implement Client ID for Rate Limiting

#### Intent

Enable rate limiting by providing a consistent client identifier with each API request.

#### Preconditions

- Frontend application loads successfully
- LocalStorage is available in the browser

#### User Flow (Step-by-Step)

1. User opens the application for the first time
2. System generates a unique client ID (UUID v4)
3. System stores client ID in localStorage under key `threadforge_client_id`
4. User generates a thread
5. System includes client ID in request header `X-Client-Id`
6. User closes and reopens browser
7. System retrieves existing client ID from localStorage
8. Subsequent requests use the same client ID

#### System Behavior

**Client ID Generation (create `src/app/core/services/client-id.service.ts`):**
```typescript
@Injectable({ providedIn: 'root' })
export class ClientIdService {
  private readonly STORAGE_KEY = 'threadforge_client_id';

  getClientId(): string {
    let clientId = localStorage.getItem(this.STORAGE_KEY);
    if (!clientId) {
      clientId = crypto.randomUUID();
      localStorage.setItem(this.STORAGE_KEY, clientId);
    }
    return clientId;
  }
}
```

**HTTP Interceptor (create `src/app/core/interceptors/client-id.interceptor.ts`):**
```typescript
export const clientIdInterceptor: HttpInterceptorFn = (req, next) => {
  const clientIdService = inject(ClientIdService);
  const clientId = clientIdService.getClientId();
  
  const clonedReq = req.clone({
    setHeaders: { 'X-Client-Id': clientId }
  });
  
  return next(clonedReq);
};
```

**Register interceptor in `app.config.ts`:**
```typescript
provideHttpClient(withInterceptors([clientIdInterceptor]))
```

#### Inputs

- None (automatic generation)

#### Outputs

- UUID v4 stored in localStorage
- `X-Client-Id` header included in all API requests

#### Error Cases

- LocalStorage unavailable → generate client ID per session (memory only)
- Existing client ID is invalid UUID → regenerate new UUID

#### Acceptance Criteria

- [ ] Client ID is generated on first visit and persisted
- [ ] Client ID survives browser refresh and tab close/reopen
- [ ] All API requests include `X-Client-Id` header
- [ ] Client ID is valid UUID v4 format
- [ ] No errors thrown when localStorage is unavailable

---

### User Story 2 – Handle 429 Rate Limit Response

#### Intent

Show a clear, friendly message when the user has exceeded their daily generation limit.

#### Preconditions

- User has made 20 thread generation requests in the current 24-hour period
- Backend returns HTTP 429 status code

#### User Flow (Step-by-Step)

1. User clicks "Generate" button
2. System sends request to backend
3. Backend returns 429 status code
4. System shows toast notification with rate limit message
5. Button returns to enabled state
6. User understands they must wait until tomorrow

#### System Behavior

**429 Response Handling (already partially implemented in generator.component.ts):**

Verify the error handling displays this exact message:
```
Daily limit reached. You can generate 20 threads per day. Try again tomorrow.
```

**Toast Configuration:**
- Duration: 5000ms (5 seconds)
- Position: top-center
- Style: error-toast class (red/danger styling)

**Backend Response Format (expected):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.29",
  "title": "Too Many Requests",
  "status": 429,
  "detail": "Daily limit of 20 thread generations exceeded. Try again tomorrow.",
  "retryAfter": "2026-01-23T00:00:00Z"
}
```

#### Inputs

- HTTP 429 response from backend

#### Outputs

- User-friendly toast message displayed
- Generate button re-enabled
- No console errors

#### Error Cases

- None (this IS the error case handling)

#### Acceptance Criteria

- [ ] 429 response shows toast: "Daily limit reached. You can generate 20 threads per day. Try again tomorrow."
- [ ] Toast uses error styling (red/danger color scheme)
- [ ] Toast duration is 5 seconds
- [ ] Generate button is re-enabled after error
- [ ] No JavaScript errors in console

---

### User Story 3 – Handle Network Errors Gracefully

#### Intent

Provide clear feedback when network connectivity issues prevent thread generation.

#### Preconditions

- User has valid form input
- Network is unavailable or request fails to reach server

#### User Flow (Step-by-Step)

1. User clicks "Generate" button
2. System attempts to send request
3. Network error occurs (status 0 or fetch failure)
4. System shows toast with network error message
5. Button returns to enabled state
6. User can retry when network is restored

#### System Behavior

**Error Message:**
```
Network error. Check your connection and try again.
```

**Detection Logic (in error handler):**
```typescript
if (error.status === 0) {
  message = 'Network error. Check your connection and try again.';
}
```

#### Inputs

- HTTP error with status 0 or network failure

#### Outputs

- User-friendly toast message
- Generate button re-enabled

#### Error Cases

- Offline mode → same message
- DNS failure → same message
- CORS error → same message

#### Acceptance Criteria

- [ ] Network errors show toast: "Network error. Check your connection and try again."
- [ ] Toast appears within 1 second of error
- [ ] Generate button is re-enabled
- [ ] User can retry immediately after error

---

### User Story 4 – Handle Server Errors (500/503)

#### Intent

Provide clear feedback when the backend or AI service encounters an error.

#### Preconditions

- User has valid form input
- Backend returns 500 or 503 status code

#### User Flow (Step-by-Step)

1. User clicks "Generate" button
2. System sends request to backend
3. Backend returns 500 or 503 error
4. System shows toast with server error message
5. Button returns to enabled state
6. User can retry

#### System Behavior

**Error Message:**
```
Thread generation failed. Please try again.
```

**Detection Logic:**
```typescript
if (error.status === 500 || error.status === 503) {
  message = 'Thread generation failed. Please try again.';
}
```

#### Inputs

- HTTP 500 or 503 response

#### Outputs

- User-friendly toast message
- Generate button re-enabled

#### Error Cases

- AI service timeout → same message
- Database connection failure → same message

#### Acceptance Criteria

- [ ] 500/503 errors show toast: "Thread generation failed. Please try again."
- [ ] Toast uses error styling
- [ ] Generate button is re-enabled after error
- [ ] No stack traces or technical details shown to user

---

### User Story 5 – Handle Request Timeout

#### Intent

Provide clear feedback when thread generation takes too long.

#### Preconditions

- User has valid form input
- Request exceeds 15 second timeout

#### User Flow (Step-by-Step)

1. User clicks "Generate" button
2. System sends request with 15 second timeout
3. Request exceeds timeout
4. System cancels request and shows timeout message
5. Button returns to enabled state

#### System Behavior

**Error Message:**
```
Request took too long. Please try again.
```

**Timeout Configuration (in thread.service.ts):**
```typescript
private readonly timeout_ms = 15000; // 15 seconds
```

**Detection Logic:**
```typescript
if (error instanceof TimeoutError) {
  message = 'Request took too long. Please try again.';
}
```

#### Inputs

- RxJS TimeoutError

#### Outputs

- User-friendly toast message
- Generate button re-enabled
- Request cancelled

#### Error Cases

- Slow network → timeout triggered
- AI service slow response → timeout triggered

#### Acceptance Criteria

- [ ] Requests timeout after 15 seconds
- [ ] Timeout shows toast: "Request took too long. Please try again."
- [ ] Generate button is re-enabled after timeout
- [ ] No pending requests left after timeout

---

### User Story 6 – Keyboard Accessibility for Form

#### Intent

Enable users to complete the entire generation flow using only keyboard navigation.

#### Preconditions

- Generator page is loaded
- User is using keyboard-only navigation

#### User Flow (Step-by-Step)

1. User presses Tab to focus Topic input
2. User types topic and presses Tab
3. User focuses Audience input, types (optional), presses Tab
4. User navigates through Tone pills using Tab/Arrow keys
5. User presses Enter/Space to select a tone
6. User tabs to slider, uses Arrow keys to adjust
7. User tabs to Generate button
8. User presses Enter/Space to generate

#### System Behavior

**Focus States (add to styles.css if missing):**
```css
:focus-visible {
  outline: 2px solid var(--accent);
  outline-offset: 2px;
}

.tone-pill:focus-visible {
  outline: 2px solid var(--accent);
  outline-offset: 2px;
}

.button-primary:focus-visible {
  outline: 2px solid white;
  outline-offset: 2px;
}
```

**Tone Pills Keyboard Support:**
- Tab focuses first pill in group
- Arrow Left/Right moves between pills
- Enter/Space selects focused pill

**ARIA Attributes:**
- Tone selector: `role="radiogroup"`, `aria-labelledby="tone-label"`
- Each tone pill: `role="radio"`, `aria-checked="true/false"`
- Generate button: `aria-busy="true"` when generating

#### Inputs

- Keyboard events (Tab, Enter, Space, Arrow keys)

#### Outputs

- Visible focus indicators on all interactive elements
- Logical tab order through form
- Proper ARIA states announced by screen readers

#### Error Cases

- None

#### Acceptance Criteria

- [ ] All form elements are reachable via Tab key
- [ ] Tab order follows visual layout (top to bottom, left to right)
- [ ] Focus is visible on all interactive elements
- [ ] Tone pills can be selected with keyboard
- [ ] Slider can be adjusted with Arrow keys
- [ ] Generate button can be activated with Enter/Space
- [ ] No keyboard traps exist

---

### User Story 7 – Mobile Responsive Layout Verification

#### Intent

Ensure the application is usable on mobile devices with viewport width < 960px.

#### Preconditions

- Application loaded on mobile device or narrow viewport

#### User Flow (Step-by-Step)

1. User opens application on mobile device
2. User sees single-column layout
3. Forge Panel appears first (top)
4. Preview Panel appears below Forge
5. User can scroll to see entire form
6. User generates thread
7. User scrolls down to see generated tweets
8. User can copy and edit tweets

#### System Behavior

**Breakpoint: 960px**

**Mobile Layout (< 960px):**
- Single column layout
- Forge Panel: full width, not sticky
- Preview Panel: full width, below Forge
- All touch targets minimum 44x44px

**Already Implemented in generator.component.css:**
```css
@media (max-width: 959px) {
  .generator-layout {
    grid-template-columns: 1fr;
    gap: var(--space-6);
  }
  
  .forge-column {
    position: static;
  }
  
  .preview-column {
    overflow-y: visible;
    max-height: none;
  }
}
```

#### Inputs

- Viewport width < 960px

#### Outputs

- Single-column stacked layout
- All content accessible via scrolling
- Touch-friendly button sizes

#### Error Cases

- None

#### Acceptance Criteria

- [ ] Layout stacks vertically at viewport width < 960px
- [ ] Forge Panel appears above Preview Panel on mobile
- [ ] All form elements are usable on touch devices
- [ ] Touch targets are minimum 44x44px
- [ ] No horizontal scrolling required
- [ ] Generate button is easily tappable

---

### User Story 8 – Verify Tweet Character Limit Enforcement

#### Intent

Ensure all generated tweets respect the 280 character limit and display warnings when exceeded.

#### Preconditions

- User has generated a thread
- Thread contains tweets displayed in Preview Panel

#### User Flow (Step-by-Step)

1. User generates a thread
2. Each tweet card displays character count (e.g., "145/280")
3. If any tweet exceeds 280 characters, counter turns red
4. User can edit tweets to reduce character count
5. Character count updates in real-time during editing

#### System Behavior

**Character Count Display:**
- Format: `{count}/280`
- Color: `var(--text-tertiary)` when ≤ 280
- Color: `var(--danger)` when > 280
- Font weight: 600 when over limit

**Already Implemented in tweet-card.component.ts:**
```typescript
get isOverLimit(): boolean {
  return this.charCount > 280;
}
```

#### Inputs

- Tweet text string

#### Outputs

- Character count displayed on each tweet card
- Visual warning when over 280 characters

#### Error Cases

- None (over-limit is a warning, not an error)

#### Acceptance Criteria

- [ ] Each tweet card shows character count in format "X/280"
- [ ] Character count updates in real-time when editing
- [ ] Counter turns red when count > 280
- [ ] Counter uses bold font weight when over limit
- [ ] Backend should enforce 280 limit (out of scope for frontend)

---

### User Story 9 – End-to-End Flow Smoke Test

#### Intent

Verify the complete user journey works without errors.

#### Preconditions

- Frontend application running
- Backend API running and connected

#### User Flow (Step-by-Step)

1. User navigates to `/` → redirects to `/generator`
2. User enters topic: "How to build a SaaS in 2026"
3. User enters audience: "Indie hackers"
4. User selects tone: "Indie Hacker"
5. User adjusts tweet count to 6
6. User clicks "Generate" → loading state appears
7. Loading completes → 6 tweet cards appear
8. User hovers tweet 1 → action buttons appear
9. User clicks copy on tweet 1 → toast "Tweet copied to clipboard"
10. User clicks "Copy All" → toast "Thread copied to clipboard"
11. User clicks edit on tweet 2 → textarea appears
12. User modifies text and presses Enter → edit saved
13. User clicks "Regenerate" → new thread generated
14. User expands feedback, types "Make it more provocative"
15. User clicks "Regenerate" → new thread with feedback applied

#### System Behavior

All previously implemented functionality working together.

#### Inputs

- Valid form data
- User interactions (clicks, keyboard input)

#### Outputs

- Complete flow works end-to-end without errors

#### Error Cases

- Any step failing indicates a bug to fix

#### Acceptance Criteria

- [ ] Navigation to `/` redirects to `/generator`
- [ ] Form validation works correctly
- [ ] Thread generation completes successfully
- [ ] Tweet cards display with correct styling
- [ ] Copy single tweet works
- [ ] Copy all tweets works
- [ ] Inline edit works
- [ ] Regenerate works
- [ ] Regenerate with feedback works
- [ ] No console errors during entire flow
- [ ] No visual glitches or layout issues

---

### User Story 10 – Error Toast Styling

#### Intent

Ensure error toasts are visually distinct and match the design system.

#### Preconditions

- Material Snackbar is configured
- Error toast class is applied

#### User Flow (Step-by-Step)

1. User triggers an error (network, timeout, 429)
2. Error toast appears at top-center
3. Toast has red/danger styling
4. Toast has close button
5. Toast auto-dismisses after 5 seconds

#### System Behavior

**Add error toast styles to `styles.css`:**
```css
.error-toast {
  --mdc-snackbar-container-color: var(--danger);
  --mdc-snackbar-supporting-text-color: white;
  --mat-snack-bar-button-color: white;
}

.mat-mdc-snack-bar-container.error-toast {
  background-color: var(--danger);
}

.error-toast .mdc-snackbar__surface {
  background-color: var(--danger) !important;
}

.error-toast .mdc-snackbar__label {
  color: white !important;
}
```

**Toast Configuration:**
```typescript
this.snackBar.open(message, 'Close', {
  duration: 5000,
  horizontalPosition: 'center',
  verticalPosition: 'top',
  panelClass: ['error-toast']
});
```

#### Inputs

- Error message string

#### Outputs

- Styled error toast displayed

#### Error Cases

- None

#### Acceptance Criteria

- [ ] Error toasts have red/danger background color
- [ ] Error toasts have white text
- [ ] Error toasts appear at top-center of screen
- [ ] Error toasts have visible close button
- [ ] Error toasts auto-dismiss after 5 seconds
- [ ] Success toasts (copy) use default/neutral styling

---

## Non-Goals (Explicitly Out of Scope)

- Automated E2E tests (Cypress, Playwright) — manual QA only
- Performance benchmarking — qualitative "feels fast" check
- Cross-browser testing beyond Chrome — deferred to post-launch
- Accessibility audit (WCAG compliance) — basic keyboard nav only
- Security audit — deferred to post-launch
- Load testing — not required for MVP
- Saved drafts persistence — Phase 2 feature
- Analytics/telemetry integration — deferred

---

## Pre-Launch Checklist

Before marking MVP as "shipped":

- [ ] All 10 user stories pass acceptance criteria
- [ ] Build succeeds with no errors: `npm run build`
- [ ] No TypeScript errors: `npm run lint`
- [ ] Backend rate limiting works (429 on 21st request)
- [ ] Backend returns valid JSON responses
- [ ] CORS configured correctly for production domain
- [ ] Environment variables set for production
- [ ] Error messages don't leak stack traces
- [ ] Console is clean (no errors, minimal warnings)
- [ ] Favicon and meta tags set
- [ ] README updated with deployment instructions

---

## Execution Notes for Dev Agent

- Focus on verification, not new features
- Test in Chrome primarily
- Use browser DevTools to simulate slow network and offline mode
- Check console for errors after each action
- Verify mobile layout using Chrome DevTools device emulation
- All styling must use CSS variables from `:root` in `styles.css`
- No architectural changes without user approval
- Follow the file exactly as written
- Do not infer missing features
- Do not generalize beyond this MVP
