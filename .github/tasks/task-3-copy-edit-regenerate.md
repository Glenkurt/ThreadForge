# ThreadForge – Task 3: Copy, Edit & Regenerate

## Global Constraints

- No authentication (single user session)
- English UI only
- Desktop-first UX (responsive stacking on mobile < 960px)
- All errors must show user-readable messages via Material snackbar
- All generated threads are editable before use
- Edits persist locally in component state (no backend persistence in Task 3)
- Design tokens from `.github/Docs/designSpec.md` must be used

---

## Definitions (Shared Vocabulary)

- **Thread**: A sequence of 5–12 tweets intended to be posted in order on X/Twitter
- **Tweet Card**: A visual component displaying a single tweet with index, character count, and action buttons
- **Copy Single**: Copy the content of one tweet to the clipboard
- **Copy All**: Copy the entire thread as a numbered, formatted text block to the clipboard
- **Inline Edit**: Edit the tweet text directly within the tweet card without navigating away
- **Regenerate**: Request a new AI-generated thread using the current form inputs
- **Regenerate with Feedback**: Request a new thread with an additional instruction (e.g., "make it more provocative")
- **Toast**: A temporary notification shown at top-center of the screen for 3 seconds

---

## User Stories

---

### User Story 1 – Copy Single Tweet to Clipboard

#### Intent

Users can copy an individual tweet to paste it elsewhere (e.g., into X/Twitter composer).

#### Preconditions

- A thread has been generated (`generatedThread` is not null)
- At least one tweet card is visible in the Preview Panel

#### User Flow (Step-by-Step)

1. User hovers over a tweet card
2. Action buttons appear on the tweet card (copy icon visible)
3. User clicks the copy icon button
4. System copies the tweet text to clipboard
5. System shows success toast: "Tweet copied to clipboard"
6. Copy icon briefly changes to a checkmark for 1.5 seconds
7. Icon reverts to copy icon after 1.5 seconds

#### System Behavior

**Tweet Card Component Updates:**

- Add a `.tweet-actions` container inside each tweet card
- Actions container is hidden by default (`opacity: 0`)
- Actions container becomes visible on tweet card hover (`opacity: 1`)
- Copy button uses a clipboard/copy SVG icon (16px)
- On click, call `navigator.clipboard.writeText(tweet)`
- On successful copy:
  - Show snackbar with message: `Tweet copied to clipboard`
  - Set local state `copied = true` for 1.5 seconds
  - While `copied === true`, display checkmark icon instead of copy icon
  - After 1.5 seconds, revert to copy icon

**CSS for tweet-actions (add to tweet-card.component.css):**

```css
.tweet-actions {
  position: absolute;
  top: var(--space-3);
  right: var(--space-3);
  display: flex;
  gap: var(--space-2);
  opacity: 0;
  transition: opacity 0.15s ease;
}

.tweet-card:hover .tweet-actions {
  opacity: 1;
}

.action-button {
  width: 32px;
  height: 32px;
  border: none;
  border-radius: var(--radius-sm);
  background: var(--bg-panel);
  color: var(--text-tertiary);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.15s ease;
}

.action-button:hover {
  background: var(--accent-soft);
  color: var(--accent);
}
```

#### Inputs

- `tweet`: string (the tweet text to copy)

#### Outputs

- Tweet text copied to system clipboard
- Visual feedback: checkmark icon for 1.5 seconds
- Toast notification: "Tweet copied to clipboard"

#### Error Cases

- Clipboard API fails → show toast: "Failed to copy. Please try again."

#### Acceptance Criteria

- [ ] Tweet card shows action buttons on hover
- [ ] Copy icon is 16px, uses `var(--text-tertiary)` color
- [ ] Clicking copy button copies exact tweet text to clipboard
- [ ] Success toast appears at top-center for 3 seconds
- [ ] Copy icon changes to checkmark for 1.5 seconds after successful copy
- [ ] Clipboard API errors show user-friendly error message

---

### User Story 2 – Copy All Tweets as Numbered Thread

#### Intent

Users can copy the entire generated thread as a formatted, numbered text block for easy pasting into a thread scheduler or notes app.

#### Preconditions

- A thread has been generated (`generatedThread` is not null)
- Thread contains at least one tweet

#### User Flow (Step-by-Step)

1. User sees the "Copy All" button in the Preview Panel header
2. User clicks "Copy All" button
3. System formats all tweets as numbered text block
4. System copies formatted text to clipboard
5. System shows success toast: "Thread copied to clipboard"
6. Button shows checkmark icon for 1.5 seconds
7. Button reverts to copy icon after 1.5 seconds

#### System Behavior

**Copy All Button Location:**

- Add button to Preview Panel header, right-aligned next to "Preview" title
- Button is only visible when `generatedThread !== null`

**Button Styling:**

```css
.copy-all-button {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  padding: var(--space-2) var(--space-3);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-sm);
  background: transparent;
  color: var(--text-secondary);
  font-size: var(--text-sm);
  cursor: pointer;
  transition: all 0.15s ease;
}

.copy-all-button:hover {
  border-color: var(--accent);
  color: var(--accent);
  background: var(--accent-soft);
}
```

**Text Format (exact output):**

```
1/ [Tweet 1 text]

2/ [Tweet 2 text]

3/ [Tweet 3 text]

...
```

- Each tweet is prefixed with `{index}/` followed by a space
- Tweets are separated by exactly two newlines (`\n\n`)
- No trailing newline after last tweet

**Implementation (in thread-preview.component.ts):**

```typescript
copyAllTweets(): void {
  if (!this.tweets) return;

  const formattedThread = this.tweets
    .map((tweet, index) => `${index + 1}/ ${tweet}`)
    .join('\n\n');

  navigator.clipboard.writeText(formattedThread).then(
    () => {
      this.showToast('Thread copied to clipboard');
      this.allCopied = true;
      setTimeout(() => this.allCopied = false, 1500);
    },
    () => {
      this.showToast('Failed to copy. Please try again.');
    }
  );
}
```

#### Inputs

- `tweets`: string[] (array of tweet texts)

#### Outputs

- Formatted numbered thread copied to clipboard
- Toast notification: "Thread copied to clipboard"
- Button shows checkmark for 1.5 seconds

#### Error Cases

- Clipboard API fails → show toast: "Failed to copy. Please try again."
- No tweets to copy → button is hidden (precondition)

#### Acceptance Criteria

- [ ] "Copy All" button appears in Preview Panel header when thread exists
- [ ] Button is hidden when no thread is generated
- [ ] Clicking button copies all tweets in numbered format: `1/ text\n\n2/ text\n\n...`
- [ ] Success toast appears: "Thread copied to clipboard"
- [ ] Button icon changes to checkmark for 1.5 seconds
- [ ] Clipboard API errors show user-friendly error message

---

### User Story 3 – Inline Edit Single Tweet

#### Intent

Users can edit individual tweets directly within the tweet card to refine wording without regenerating the entire thread.

#### Preconditions

- A thread has been generated
- At least one tweet card is visible

#### User Flow (Step-by-Step)

1. User hovers over a tweet card
2. Edit icon button appears in tweet actions
3. User clicks edit icon
4. Tweet text transforms into an editable textarea
5. User modifies the text
6. Character count updates in real-time
7. User clicks outside the textarea OR presses Enter (without Shift)
8. System saves the edited text to local state
9. Textarea transforms back to static text display
10. Character count reflects final edited text

#### System Behavior

**Tweet Card Edit Mode:**

- Add `isEditing: boolean = false` state per tweet card
- When `isEditing === true`:
  - Replace static text with `<textarea>`
  - Textarea auto-focuses on entering edit mode
  - Textarea has same styling as static text (seamless transition)
  - Character counter updates on every keystroke
- When `isEditing === false`:
  - Display static tweet text

**Textarea Styling:**

```css
.tweet-edit-textarea {
  width: 100%;
  min-height: 80px;
  background: transparent;
  border: 1px solid var(--accent);
  border-radius: var(--radius-sm);
  padding: var(--space-3);
  color: var(--text-primary);
  font-size: var(--text-base);
  font-family: inherit;
  line-height: 1.5;
  resize: vertical;
}

.tweet-edit-textarea:focus {
  outline: none;
  box-shadow: 0 0 0 2px var(--accent-soft);
}
```

**Exit Edit Mode Triggers:**

1. Click outside textarea (blur event)
2. Press Enter key (without Shift held)
3. Click save/check button in tweet actions

**Data Flow:**

- TweetCardComponent emits `(tweetEdited)` event with `{ index: number, newText: string }`
- ThreadPreviewComponent receives event and calls parent callback
- GeneratorComponent updates `generatedThread` signal at the specified index
- Signal update triggers re-render with new text

**Component Changes (tweet-card.component.ts):**

```typescript
@Input({ required: true }) tweet!: string;
@Input({ required: true }) index!: number;
@Output() tweetEdited = new EventEmitter<{ index: number; newText: string }>();

isEditing = false;
editedText = '';

startEditing(): void {
  this.editedText = this.tweet;
  this.isEditing = true;
}

saveEdit(): void {
  const trimmed = this.editedText.trim();
  if (trimmed.length > 0 && trimmed !== this.tweet) {
    this.tweetEdited.emit({ index: this.index, newText: trimmed });
  }
  this.isEditing = false;
}

cancelEdit(): void {
  this.isEditing = false;
}

onKeydown(event: KeyboardEvent): void {
  if (event.key === 'Enter' && !event.shiftKey) {
    event.preventDefault();
    this.saveEdit();
  }
  if (event.key === 'Escape') {
    this.cancelEdit();
  }
}
```

#### Inputs

- `tweet`: string (original tweet text)
- `index`: number (1-indexed position in thread)

#### Outputs

- `tweetEdited` event emitted with new text
- Updated tweet displayed in card
- Updated character count

#### Error Cases

- Empty text after trim → revert to original text, do not emit
- Text unchanged → do not emit, just close edit mode

#### Acceptance Criteria

- [ ] Edit icon appears on tweet card hover
- [ ] Clicking edit icon transforms tweet text into editable textarea
- [ ] Textarea auto-focuses when edit mode starts
- [ ] Character count updates in real-time during editing
- [ ] Pressing Enter (without Shift) saves and exits edit mode
- [ ] Pressing Escape cancels and reverts to original text
- [ ] Clicking outside textarea saves and exits edit mode
- [ ] Empty edits are rejected (original text preserved)
- [ ] Edited text persists in local component state
- [ ] Character count shows warning styling when > 280

---

### User Story 4 – Regenerate Thread (Same Inputs)

#### Intent

Users can request a new AI-generated thread using the same form inputs if they're not satisfied with the current result.

#### Preconditions

- A thread has been previously generated
- Form inputs are still valid

#### User Flow (Step-by-Step)

1. User views current generated thread
2. User clicks "Regenerate" button in Forge Panel
3. System shows loading state (same as initial generation)
4. System calls `/api/v1/threads/generate` with current form values
5. System replaces current thread with new response
6. Loading state ends, new tweets displayed

#### System Behavior

**Regenerate Button:**

- Add secondary button below "Generate" button in Forge Panel
- Button text: `Regenerate`
- Button is only visible when `generatedThread !== null`
- Button is disabled when `isGenerating === true`

**Button Styling:**

```css
.regenerate-button {
  width: 100%;
  padding: var(--space-3) var(--space-4);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-md);
  background: transparent;
  color: var(--text-secondary);
  font-size: var(--text-base);
  font-weight: 500;
  cursor: pointer;
  transition: all 0.15s ease;
  margin-top: var(--space-3);
}

.regenerate-button:hover:not(:disabled) {
  border-color: var(--accent);
  color: var(--accent);
  background: var(--accent-soft);
}

.regenerate-button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
```

**Behavior:**

- On click, call the same `onGenerate()` method
- Current thread is replaced entirely (no merge/append)
- All local edits to previous thread are discarded
- Loading spinner appears in Preview Panel
- On error, show appropriate toast message (same error handling as Task 2)

#### Inputs

- Current form values (topic, audience, tone, tweetCount)

#### Outputs

- New thread replaces previous thread
- Loading state during generation
- Error toast if API fails

#### Error Cases

- All error cases from Task 2 apply (429, 500, timeout, network error)

#### Acceptance Criteria

- [ ] "Regenerate" button appears below "Generate" button when thread exists
- [ ] Button is hidden when no thread has been generated
- [ ] Clicking button triggers new API call with current form values
- [ ] Loading state displays during regeneration
- [ ] New thread completely replaces previous thread
- [ ] Any local edits to previous thread are discarded
- [ ] Button is disabled during generation

---

### User Story 5 – Regenerate with Feedback Prompt

#### Intent

Users can request a refined thread by providing additional instruction (e.g., "make it more controversial", "add more statistics").

#### Preconditions

- A thread has been previously generated
- Form inputs are still valid

#### User Flow (Step-by-Step)

1. User views current generated thread
2. User clicks "Regenerate with feedback" link/button
3. A text input field appears below the Regenerate button
4. User types feedback (e.g., "Make the hook more attention-grabbing")
5. User clicks "Regenerate" or presses Enter
6. System shows loading state
7. System calls API with form values + feedback string
8. System replaces current thread with new response
9. Feedback input clears and collapses

#### System Behavior

**UI Structure:**

- Add collapsible feedback section below Regenerate button
- Toggle link text: `+ Add feedback` (collapsed) / `− Hide feedback` (expanded)
- When expanded, show text input with placeholder

**Feedback Input:**

```css
.feedback-section {
  margin-top: var(--space-3);
}

.feedback-toggle {
  background: none;
  border: none;
  color: var(--text-tertiary);
  font-size: var(--text-sm);
  cursor: pointer;
  padding: 0;
}

.feedback-toggle:hover {
  color: var(--accent);
}

.feedback-input {
  width: 100%;
  margin-top: var(--space-2);
  padding: var(--space-3);
  background: var(--bg-panel-elevated);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-sm);
  color: var(--text-primary);
  font-size: var(--text-sm);
}

.feedback-input::placeholder {
  color: var(--text-tertiary);
}

.feedback-input:focus {
  outline: none;
  border-color: var(--accent);
}
```

**Placeholder text:** `e.g., Make it more provocative, add statistics...`

**Max length:** 200 characters

**API Request Update:**

- Extend `GenerateThreadRequest` interface:

```typescript
export interface GenerateThreadRequest {
  topic: string;
  tone: "indie_hacker" | "educational" | "provocative" | "direct" | null;
  audience: string | null;
  tweetCount: number;
  feedback?: string | null; // NEW: optional feedback for regeneration
}
```

- Backend must accept optional `feedback` field (no backend changes in scope for Task 3, assume backend supports it)

**Behavior:**

- Feedback is included only when non-empty after trim
- After successful regeneration, feedback input is cleared
- Feedback section remains expanded (user might want to tweak again)

#### Inputs

- `feedback`: string, optional, max 200 characters, trimmed

#### Outputs

- New thread generated with feedback incorporated
- Feedback input cleared after success
- Loading state during generation

#### Error Cases

- Feedback > 200 chars → show inline error: "Feedback must be 200 characters or less"
- API errors → same handling as Task 2

#### Acceptance Criteria

- [ ] "+ Add feedback" toggle appears when thread exists
- [ ] Clicking toggle reveals feedback text input
- [ ] Feedback input has placeholder: "e.g., Make it more provocative, add statistics..."
- [ ] Feedback max length is 200 characters
- [ ] Regenerate button uses feedback value when present
- [ ] Feedback is cleared after successful regeneration
- [ ] Feedback validation error shown if > 200 chars
- [ ] Pressing Enter in feedback input triggers regeneration

---

### User Story 6 – Update Tweet Card Styling to Match Design Spec

#### Intent

Ensure tweet cards use the dark theme design tokens for visual consistency.

#### Preconditions

- Tweet cards exist and render correctly

#### User Flow (Step-by-Step)

1. User generates a thread
2. User observes tweet cards in Preview Panel
3. Cards match the dark theme styling from design spec

#### System Behavior

**Update tweet-card.component.css to use design tokens:**

```css
.tweet-card {
  position: relative;
  background: var(--bg-panel-elevated);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-lg);
  padding: var(--space-4);
  margin-bottom: var(--space-4);
  transition: border-color 0.15s ease;
}

.tweet-card:hover {
  border-color: var(--border-strong);
}

.tweet-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--space-3);
}

.tweet-number {
  font-size: var(--text-sm);
  font-weight: 600;
  color: var(--accent);
}

.char-count {
  font-size: var(--text-xs);
  color: var(--text-tertiary);
}

.char-count.over-limit {
  color: var(--danger);
  font-weight: 600;
}

.tweet-body {
  color: var(--text-primary);
  font-size: var(--text-base);
  line-height: 1.5;
  white-space: pre-wrap;
  word-wrap: break-word;
}
```

**Thread Connector (between cards):**

- Add visual connector line between tweet cards
- Connector is a vertical line from bottom of one card to top of next

```css
.tweet-card:not(:last-child)::after {
  content: "";
  position: absolute;
  left: 28px;
  bottom: -17px;
  width: 2px;
  height: 16px;
  background: var(--border-subtle);
}
```

#### Inputs

- None (pure CSS update)

#### Outputs

- Tweet cards styled with dark theme tokens
- Thread connector visible between cards

#### Error Cases

- None (pure styling)

#### Acceptance Criteria

- [ ] Tweet card background uses `var(--bg-panel-elevated)`
- [ ] Tweet card border uses `var(--border-subtle)`
- [ ] Tweet number uses `var(--accent)` color
- [ ] Character count uses `var(--text-tertiary)` color
- [ ] Over-limit character count uses `var(--danger)` color
- [ ] Tweet body uses `var(--text-primary)` color
- [ ] Hover state increases border visibility
- [ ] Thread connector line appears between cards

---

## Non-Goals (Explicitly Out of Scope)

- Persisting edits to backend/database
- Saved drafts list or history view
- Undo/redo for edits
- Drag-and-drop reordering of tweets
- Delete individual tweets from thread
- Add new tweets to existing thread
- Authentication or user accounts
- Keyboard shortcuts beyond Enter/Escape in edit mode
- Mobile-specific interaction patterns

---

## Execution Notes for Dev Agent

- All styling must use CSS variables from `:root` in `styles.css`
- Use Angular signals for all component state
- Use Angular Material `MatSnackBar` for toast notifications
- Do not modify backend API (assume `feedback` field is already supported)
- Test clipboard API in both secure (HTTPS) and localhost contexts
- Ensure all new buttons are keyboard accessible
- Follow existing code patterns in `generator.component.ts`
- No architectural changes without user approval
- Follow the file exactly as written
- Do not infer missing features
- Do not generalize beyond this MVP
