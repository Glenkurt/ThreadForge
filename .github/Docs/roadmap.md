# ThreadForge Roadmap

Last updated: 2026-01-20

This roadmap is ordered for fastest path to a lovable MVP, aligned to the PRD in [.github/Docs/prd.md](.github/Docs/prd.md).

## MVP decisions (locked)

- Auth: keep existing auth stack in repo but **unused** for MVP.
- Model/provider: **Grok via xAI** first (no fallback chain in MVP).
- Rate limit: **20 thread generations per anonymous user per day**.
- Privacy: **do not log prompts**; **do persist prompt + output** in DB to enable saved drafts.

## Current state (baseline)

- Backend template exists (health + auth scaffolding) and Docker/Postgres.
- MVP UI for thread generation is not shipped yet.

## Next agent focus (highest leverage)

1. Build the Generator UI end-to-end (form → generate → tweet cards → copy-all → edit → regenerate).
2. Add “saved drafts” view (anonymous, per-client) so users can come back.
3. Polish output quality (hooks, CTAs, consistent tone) and error UX.

## Milestones (Feb 2026)

| Week | Milestone               | User-visible outcome                       | Exit criteria                                |
| ---- | ----------------------- | ------------------------------------------ | -------------------------------------------- |
| 1    | Generator UI foundation | Users can enter topic/tone/audience/length | Form validates; mobile layout acceptable     |
| 2    | AI generation + preview | Generate in <10s and see tweet cards       | p95 <10s; tweets ≤280 chars                  |
| 3    | Copy/edit/regenerate    | Users iterate and copy thread easily       | Copy-all works; inline edits persist locally |
| 4    | QA + launch             | Stable MVP shipped                         | Rate limit enforced; error messages readable |

## Phase 0 — MVP core loop (Must-have)

Goal: ship “generate → preview → copy” with minimal complexity.

### Epic A — Thread Generator (Frontend)

Order of delivery:

1. Generator page route
2. Input form fields
3. Call generation API
4. Tweet-card preview
5. Copy-all (numbered) + copy single tweet
6. Inline edit per tweet
7. Regenerate with feedback (“make it more controversial”)

Acceptance criteria:

- All required fields validate with clear messages
- Loading state + retry
- 429 shows a friendly “daily limit reached” message

### Epic B — Thread Generation (Backend)

Order of delivery:

1. `POST /api/v1/threads/generate` request/response contract
2. Grok prompt template and JSON-only output parsing
3. Enforce tweet length (≤280) by splitting when needed
4. Persist draft (prompt JSON + output JSON) tied to anonymous client
5. Rate limit policy: 20/day keyed by `X-Client-Id` (fallback: IP)

Acceptance criteria:

- 21st request/day returns 429
- Prompt is never written to logs
- Draft can be retrieved later (see Phase 1)

## Phase 1 — MVP polish (Should-have)

Goal: make outputs consistently “good enough to post” and the UX resilient.

### Output quality

- Stronger hook patterns (first tweet)
- Better CTA patterns (final tweet)
- Optional emojis/hashtags tuned per tone (avoid spam)
- Reduce hallucination risk: add “no fabricated metrics/claims” rule

### Reliability + UX

- Timeouts + cancellation
- Clear errors for: AI failure, 429, network
- Basic observability: request id surfaced in problem details

## Phase 2 — V1 retention (Could-have)

Goal: bring users back without requiring accounts.

- “Recent drafts” (anonymous) list with timestamps
- “Examples gallery” (curated threads) for inspiration
- “Surprise me” topic generator

## Phase 3 — V2 accounts + growth (Later)

Goal: persistence across devices + monetization.

- Optional accounts (magic link)
- Cross-device draft history
- Usage-based billing / quotas

## Risks & mitigations

- Cost: daily limits + monitor usage
- Quality variance: tighten prompt + regenerate feedback loop
- Privacy: store prompts for drafts but never log; provide delete option in V1
