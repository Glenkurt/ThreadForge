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

- **Generator Page**: The single MVP screen where users configure inputs and later generate a thread.
- **Forge Panel**: The left column panel containing thread input controls (topic, tone, audience, length).
- **Preview Panel**: The right column panel that will later show generated tweet cards; in Task 1 it shows an empty-state placeholder.
- **Topic**: The primary subject of the thread; a short phrase describing what the thread is about.
- **Tone**: The writing style requested for the thread; selected from a fixed pill list.
- **Audience**: Optional descriptor of who the thread is for.
- **Tweet Count**: The number of tweets in the generated thread; selected via a slider.
- **Design Tokens**: The CSS variables defined in `.github/Docs/designSpec.md` that control colors, spacing, typography, shadows, and radius.
- **Dot Grid Background**: The subtle app background texture specified in `.github/Docs/designSpec.md`.

---

## User Stories

---

### User Story 1 – Add Generator Route As The Default Landing Page

#### Intent

Users open the app and immediately see the Generator Page.

#### Preconditions

- Frontend app builds and runs.
- Existing routes remain functional.

#### User Flow (Step-by-Step)

1. User navigates to `/`.
2. User is redirected to `/generator`.
3. User refreshes the page on `/generator`.

#### System Behavior

- Step 1: The router redirects from `/` to `/generator` with `pathMatch: 'full'`.
- Step 2: `/generator` loads a standalone Angular component named `GeneratorComponent`.
- Step 3: A refresh on `/generator` renders the same Generator Page without any navigation errors.

#### Inputs

- URL path: `/` or `/generator`.

#### Outputs

- Rendered Generator Page layout (Forge Panel + Preview Panel).

#### Error Cases

- Unknown route → user is redirected to `/generator`.

#### Acceptance Criteria

- [ ] Navigating to `/` lands on `/generator`.
- [ ] Navigating directly to `/generator` loads successfully.
- [ ] Unknown routes do not show a blank page; they redirect to `/generator`.
- [ ] No authentication guard is applied to `/generator`.

---

### User Story 2 – Implement App Shell Visual Foundation (Tokens + Background + Layout)

#### Intent

Users see a dark-mode “power tool” UI foundation that matches the design spec.

#### Preconditions

- User Story 1 is complete.

#### User Flow (Step-by-Step)

1. User opens `/generator`.
2. User observes the page background and two-column layout.
3. User resizes the window from wide desktop to narrow mobile width.

#### System Behavior

- The app defines the exact CSS variables from `.github/Docs/designSpec.md` in `frontend/src/styles.css` under `:root`.
- The `body` uses the font stack: `Inter, system-ui, -apple-system, BlinkMacSystemFont, sans-serif`.
- The page background uses the dot grid background specified in `.github/Docs/designSpec.md`.
- The main Generator Page layout uses a two-column grid:
  - Desktop: `grid-template-columns: 420px 1fr` and `gap: var(--space-8)`.
  - Mobile: the layout stacks vertically with Preview below Forge.
- The desktop breakpoint is exactly `960px`:
  - If viewport width is `>= 960px`, use two columns.
  - If viewport width is `< 960px`, stack into one column.
- The Forge column is sticky on desktop:
  - `position: sticky; top: var(--space-8);`
  - Sticky behavior is disabled on mobile (Forge scrolls normally).
- The Preview column is independently scrollable on desktop:
  - `overflow-y: auto; max-height: calc(100vh - 64px);`
  - On mobile, Preview uses normal document flow (no independent scroll container).

#### Inputs

- Viewport width (responsive behavior): any.

#### Outputs

- Global styles applied:
  - Design tokens available as CSS variables.
  - Dark background with dot grid.
  - Two-column layout on desktop, stacked layout on mobile.

#### Error Cases

- None (pure UI foundation).

#### Acceptance Criteria

- [ ] `frontend/src/styles.css` contains the full token set from `.github/Docs/designSpec.md` (colors, spacing, typography, shadows, radius).
- [ ] The dot grid background matches the spec (radial gradient dots, 24px grid size, very low opacity).
- [ ] Desktop layout uses exactly `420px 1fr` columns and `var(--space-8)` gap.
- [ ] Layout stacks vertically when viewport width is `< 960px`.
- [ ] Forge column is sticky on desktop and not sticky on mobile.

---

### User Story 3 – Build The Forge Panel Inputs (Topic, Tone, Audience, Tweet Count)

#### Intent

Users can enter all generation inputs with clear validation.

#### Preconditions

- User Stories 1–2 are complete.

#### User Flow (Step-by-Step)

1. User sees the Forge Panel in the left column.
2. User types a Topic.
3. User optionally types an Audience.
4. User optionally selects a Tone pill.
5. User sets Tweet Count using a slider.
6. User attempts to click “Generate”.

#### System Behavior

- Forge Panel visual style:
  - Uses `.panel` styling from `.github/Docs/designSpec.md` (background, border, radius, padding, shadow).
  - Contains a visible section title text: `The Forge`.
- Topic input:
  - Rendered as a single-line text input styled per `.input` in `.github/Docs/designSpec.md`.
  - Placeholder text is exactly: `What do you want to write about?`
  - Validation:
    - Topic is required.
    - Topic is trimmed before validation.
    - Topic length must be `1..120` characters after trimming.
- Audience input:
  - Rendered as a single-line text input styled per `.input`.
  - Label text is exactly: `Audience (optional)`.
  - Placeholder text is exactly: `Indie hackers, founders, developers...`
  - Validation:
    - Optional.
    - If provided, trimmed length must be `1..80` characters.
- Tone selector:
  - Rendered as pills styled per `.tone-pill` spec.
  - Tone options (fixed list, in this exact order):
    1. `Default` (represents `null` tone)
    2. `Indie Hacker` (value: `"indie_hacker"`)
    3. `Educational` (value: `"educational"`)
    4. `Provocative` (value: `"provocative"`)
    5. `Direct` (value: `"direct"`)
  - `Default` is selected on first render.
  - Clicking a pill sets it active and deactivates all others.
- Tweet Count slider:
  - Rendered as an `input[type="range"]`.
  - Uses `accent-color: var(--accent)`.
  - Range constraints:
    - Minimum: `5`
    - Maximum: `12`
    - Step: `1`
    - Default value: `7`
  - The slider label row includes two static labels:
    - Left label text: `Short`
    - Right label text: `Long`
  - The current numeric tweet count is displayed as text: `Tweets: {N}` where `{N}` is the selected value.
- Generate button:
  - Rendered as a full-width primary button styled per `.button-primary`.
  - Button text is exactly: `Generate`.
  - Button is disabled if Topic is invalid.
  - Clicking the enabled button performs no network request in Task 1.
  - Clicking the enabled button triggers a no-op handler that:
    - Prevents page reload.
    - Emits (or logs) the current form state to developer console as a single JSON object.

#### Inputs

- topic: string, required, trimmed length `1..120`
- audience: string, optional, trimmed length `1..80`
- tone: enum [`null`, `indie_hacker`, `educational`, `provocative`, `direct`]
- tweetCount: integer, required, `5..12`

#### Outputs

- A local in-memory form state object with fields:
  - `topic: string`
  - `audience: string | null`
  - `tone: string | null`
  - `tweetCount: number`

#### Error Cases

- Empty Topic → show inline error text: `Please enter a topic.`
- Topic > 120 chars → show inline error text: `Topic must be 120 characters or less.`
- Audience provided but > 80 chars → show inline error text: `Audience must be 80 characters or less.`

#### Acceptance Criteria

- [ ] Forge Panel renders Topic, Audience, Tone pills, Tweet Count slider, and Generate button.
- [ ] Topic is required and validated exactly as specified.
- [ ] Generate button is disabled when Topic is invalid.
- [ ] Tone pills behave as a single-select group with `Default` selected initially.
- [ ] Tweet Count slider supports values 5–12, default 7, and displays `Tweets: {N}`.
- [ ] Clicking Generate does not call any API in Task 1.

---

### User Story 4 – Build The Preview Panel Empty State (No Generation Yet)

#### Intent

Users understand where results will appear and the UI matches the two-panel concept.

#### Preconditions

- User Stories 1–3 are complete.

#### User Flow (Step-by-Step)

1. User opens `/generator`.
2. User looks at the right column.
3. User edits Forge inputs.

#### System Behavior

- Preview Panel visual style:
  - Uses `.panel` styling from `.github/Docs/designSpec.md`.
  - Contains a visible section title text: `Preview`.
- Empty-state content:
  - Shows a single paragraph with text exactly: `Your generated thread will appear here.`
  - Shows a secondary hint line with text exactly: `Fill in a topic, then click Generate.`
- No tweet cards are rendered in Task 1.
- Editing Forge inputs does not change the Preview Panel content in Task 1.

#### Inputs

- None (Preview is static in Task 1).

#### Outputs

- Rendered Preview Panel with empty-state messaging.

#### Error Cases

- None.

#### Acceptance Criteria

- [ ] Preview Panel is visible on desktop next to Forge.
- [ ] Preview Panel appears below Forge on mobile (< 960px).
- [ ] Preview Panel shows the two empty-state lines exactly as specified.
- [ ] No tweet-card UI is present in Task 1.

---

## Non-Goals (Explicitly Out of Scope)

- Calling `POST /api/v1/threads/generate`
- Rendering tweet cards, skeleton loaders, thread connectors, or tweet hover actions
- Copy-to-clipboard actions, toasts, edit-in-place, regenerate flow
- Saved drafts list or any persistence UI
- Authentication UI/flows in the Generator Page

---

## Execution Notes for Dev Agent

- No architectural changes without user approval
- Follow the file exactly as written
- Do not infer missing features
- Do not generalize beyond this MVP
