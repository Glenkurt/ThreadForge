# ThreadForge UI — Technical Specification

> High‑fidelity UI spec for building the ThreadForge web application.
> Audience: Indie hackers, developers, #BuildInPublic creators.
> Goal: Ship a fast, dark‑mode, power‑tool‑grade UI.

---

## 1. Design Principles

- Dark‑first, distraction‑free
- High contrast, low visual noise
- Single accent color
- Strict spacing scale
- Dense but readable ("developer native")
- Inspired by Linear / Raycast / Vercel dashboards

---

## 2. Design Tokens (CSS Variables)

### 2.1 Color System

```css
:root {
  /* Base */
  --bg-app: #0a0a0a;
  --bg-panel: #0f1115;
  --bg-panel-elevated: #141821;

  --border-subtle: rgba(255, 255, 255, 0.06);
  --border-strong: rgba(255, 255, 255, 0.12);

  /* Text */
  --text-primary: #ffffff;
  --text-secondary: #b5b7c0;
  --text-tertiary: #7a7d86;
  --text-disabled: #4a4d55;

  /* Accent */
  --accent: #3b82f6; /* Electric Blue */
  --accent-hover: #2563eb;
  --accent-soft: rgba(59, 130, 246, 0.15);

  /* Status */
  --success: #22c55e;
  --warning: #f59e0b;
  --danger: #ef4444;
}
```

---

### 2.2 Shadows

```css
:root {
  --shadow-sm: 0 1px 2px rgba(0, 0, 0, 0.4);
  --shadow-md: 0 8px 24px rgba(0, 0, 0, 0.6);
}
```

---

### 2.3 Radius

```css
:root {
  --radius-sm: 8px;
  --radius-md: 12px;
  --radius-lg: 16px;
  --radius-xl: 20px;
}
```

---

## 3. Spacing Scale (8px System)

```css
:root {
  --space-1: 4px;
  --space-2: 8px;
  --space-3: 12px;
  --space-4: 16px;
  --space-5: 20px;
  --space-6: 24px;
  --space-8: 32px;
  --space-10: 40px;
  --space-12: 48px;
  --space-16: 64px;
}
```

All margins, paddings, and gaps must use this scale.

---

## 4. Typography

### 4.1 Font Stack

```css
body {
  font-family: Inter, system-ui, -apple-system, BlinkMacSystemFont, sans-serif;
}
```

### 4.2 Type Scale

```css
:root {
  --text-xs: 12px;
  --text-sm: 14px;
  --text-base: 15px;
  --text-md: 16px;
  --text-lg: 18px;
  --text-xl: 22px;
}
```

Usage:
- Labels: `text-sm`
- Body: `text-base`
- Section headers: `text-md` / `text-lg`
- App title: `text-xl`

---

## 5. Layout Architecture

### 5.1 App Shell

```
<App>
 ├─ Background (dot grid)
 ├─ MainLayout
 │   ├─ Left Column (The Forge – fixed)
 │   └─ Right Column (Preview – scrollable)
```

### 5.2 Two‑Column Grid

```css
.main-layout {
  display: grid;
  grid-template-columns: 420px 1fr;
  gap: var(--space-8);
}
```

Mobile behavior:
- Stack vertically
- Preview below Forge

---

## 6. Background Texture (Dot Grid)

```css
.app-bg {
  background-color: var(--bg-app);
  background-image: radial-gradient(
    rgba(255,255,255,0.04) 1px,
    transparent 1px
  );
  background-size: 24px 24px;
}
```

Opacity must be barely perceptible.

---

## 7. Core UI Components

### 7.1 Panel / Card

```css
.panel {
  background: var(--bg-panel);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-lg);
  padding: var(--space-6);
  box-shadow: var(--shadow-md);
}
```

Used for:
- Forge container
- Preview container
- Tweet cards

---

### 7.2 Text Input (Topic)

```css
.input {
  background: var(--bg-panel-elevated);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-md);
  padding: var(--space-3) var(--space-4);
  color: var(--text-primary);
}

.input:focus {
  outline: none;
  border-color: var(--accent);
}
```

Height: ~44px

---

### 7.3 Tone Selector (Pills)

```css
.tone-pill {
  padding: var(--space-2) var(--space-4);
  border-radius: 999px;
  border: 1px solid var(--border-subtle);
  background: transparent;
  color: var(--text-secondary);
  cursor: pointer;
}

.tone-pill.active {
  background: var(--accent-soft);
  border-color: var(--accent);
  color: var(--accent);
}
```

---

### 7.4 Length Slider

```css
input[type="range"] {
  accent-color: var(--accent);
}
```

Labels: “Short” / “Long” in `--text-tertiary`

---

### 7.5 Primary Button (Generate)

```css
.button-primary {
  background: var(--accent);
  color: white;
  border-radius: var(--radius-md);
  padding: var(--space-4);
  font-weight: 600;
  transition: all 0.15s ease;
}

.button-primary:hover {
  background: var(--accent-hover);
  box-shadow: 0 0 0 4px var(--accent-soft);
}
```

Height: 48–52px

---

## 8. The Preview Stack (The Output)

### 8.1 Tweet Card

```css
.tweet {
  position: relative;
  background: var(--bg-panel-elevated);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-lg);
  padding: var(--space-4);
}
```

---

### 8.2 Thread Connector

- 1.5px solid line
- Color: `#262626`
- Connects profile pictures vertically

```css
.thread-connector {
  position: absolute;
  left: 28px;
  top: 100%;
  width: 1.5px;
  height: 24px;
  background: #262626;
}
```

---

### 8.3 Action Overlays (Copy / Edit)

- Hidden by default
- Appear on tweet hover

```css
.tweet-actions {
  opacity: 0;
  transition: opacity 0.15s ease;
}

.tweet:hover .tweet-actions {
  opacity: 1;
}
```

Icons: 16–18px, muted until hover

---

### 8.4 Draft State (Skeleton Loader)

- Shimmer or pulse animation
- Mimics tweet card layout

```css
.skeleton {
  background: linear-gradient(
    90deg,
    #1a1a1a 25%,
    #222 37%,
    #1a1a1a 63%
  );
  animation: shimmer 1.4s infinite;
}
```

Used while AI API is fetching thread content.

---

## 9. Advanced Layout Logic

### Locked‑Right Scroll Layout

#### Left Column (Fixed)
- Position: `sticky`
- Always visible
- Generate button always accessible

```css
.forge-column {
  position: sticky;
  top: var(--space-8);
}
```

#### Right Column (Scrollable)
- Independent scroll container
- Grows as thread grows

```css
.preview-column {
  overflow-y: auto;
  max-height: calc(100vh - 64px);
}
```

---

### The Bridge (Panel Transition)

- Visual bridge between Forge and Preview
- Uses CSS `mask-image`

```css
.bridge {
  mask-image: linear-gradient(
    to right,
    transparent,
    black 30%,
    black 70%,
    transparent
  );
}
```

Creates a subtle fade between panels ("particle bridge").

---

## 10. Interaction & Feedback

### Copy to Clipboard

- On click:
  - Replace copy icon with checkmark for ~1s
  - Trigger toast notification

### Toast Notification

- Position: bottom-right
- Duration: 2–3s
- Subtle slide + fade

```css
.toast {
  background: var(--bg-panel);
  border: 1px solid var(--border-subtle);
  padding: var(--space-3) var(--space-4);
  border-radius: var(--radius-md);
}
```

---

## 11. Component Tree (Reference)

```
App
 ├─ Background
 ├─ MainLayout
 │   ├─ ForgePanel
 │   │   ├─ TopicInput
 │   │   ├─ ToneSelector
 │   │   ├─ LengthSlider
 │   │   └─ GenerateButton
 │   └─ PreviewPanel
 │       ├─ TweetSkeleton
 │       ├─ TweetCard
 │       ├─ ThreadConnector
 │       └─ TweetActions
```

---

## 12. Non‑Goals (v1)

- No theming system
- No animations beyond micro‑interactions
- No gradients beyond subtle skeletons

---

This spec is intentionally strict. Deviations should be deliberate and minimal to preserve the "power tool" feel.

