# ThreadForge – User Stories & Execution Plan
## Task 5: Frontend Mock Data for Development

---

## Global Constraints

- No authentication (unless explicitly specified)
- Single user session (no multi-tenant logic)
- English UI
- Desktop-first UX
- Errors must be user-readable
- All generated threads are editable before use
- Mock data must match production API response structure exactly

---

## Definitions (Shared Vocabulary)

- **Mock Data**: Hardcoded sample data that simulates API responses for frontend development
- **Thread**: A sequence of 5-7 tweets intended to be posted in order on X
- **Hook**: The first tweet designed to capture attention
- **CTA (Call To Action)**: Final tweet prompting an action
- **Topic**: The main subject provided by the user for thread generation
- **Tone**: The style of writing (indie_hacker, educational, provocative, storytelling, analytical)
- **Development Mode**: Frontend running with environment flag that enables mock data instead of real API calls

---

## User Stories

---

### User Story 5.1 – Mock Thread Generation Response

#### Intent

Allow frontend developers to build and test the thread UI without needing a running backend or consuming API quota.

#### Preconditions

- Frontend application is running in development mode
- Environment variable `USE_MOCK_DATA=true` is set
- No backend API is required to be running

#### User Flow (Step-by-Step)

1. Frontend developer sets `USE_MOCK_DATA=true` in environment
2. User enters a topic in the thread generation form
3. User optionally selects tone, audience, tweet count, key points
4. User clicks "Generate Thread" button
5. System immediately returns mock data (no HTTP call)
6. UI displays the mock thread as if it came from the real API

#### System Behavior

When `USE_MOCK_DATA=true`:
- Thread service does NOT make HTTP request to backend
- Thread service returns one of 5 predefined mock responses
- Mock response is selected based on topic string hash (deterministic)
- Response is returned within 500-1500ms simulated delay
- Response structure matches `GenerateThreadResponseDto` exactly

When `USE_MOCK_DATA=false`:
- System behaves normally, calling real API

#### Inputs

- topic: string, 1-120 chars, required
- tone: enum [indie_hacker, educational, provocative, storytelling, analytical], optional
- audience: string, max 100 chars, optional
- tweetCount: number, 5-7, default 5
- keyPoints: array of strings, max 5 items, max 100 chars each, optional
- feedback: string, max 500 chars, optional

#### Outputs

Mock response matches this exact structure:

```typescript
{
  id: string (UUID format),
  tweets: string[] (array of 5-7 tweets, each ≤280 chars),
  createdAt: string (ISO 8601 datetime),
  provider: "mock",
  model: "mock-gpt-4"
}
```

#### Mock Data Variants (5 Required)

**Variant 1: Indie Hacker (topic contains "startup" or "build")**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440001",
  "tweets": [
    "🚀 Building in public is the indie hacker's secret weapon. Here's why transparency beats secrecy in 2026 👇",
    "1/ Traditional startups hide everything until launch. But indie hackers? We share our journey, mistakes, and revenue numbers openly.",
    "2/ This transparency builds trust before you even have a product. People want to support builders they know and believe in.",
    "3/ Plus, public feedback is gold. Your audience will tell you exactly what they need—before you waste months building the wrong thing.",
    "4/ I've gained 10k followers in 6 months just by sharing my honest progress. No marketing budget needed.",
    "Want to start building in public? Drop a 👋 and I'll share my exact playbook (it's free)."
  ],
  "createdAt": "2026-01-23T10:00:00Z",
  "provider": "mock",
  "model": "mock-gpt-4"
}
```

**Variant 2: Educational (topic contains "learn" or "how")**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440002",
  "tweets": [
    "📚 Want to learn AI development in 2026? Here's your practical roadmap (no fluff) 🧵",
    "Step 1: Master Python basics\n• Variables, functions, classes\n• Work with APIs and JSON\n• 2-3 weeks if you focus",
    "Step 2: Understand how LLMs work\n• Read \"Attention Is All You Need\" (yes, really)\n• Play with prompts on ChatGPT\n• Build 3-5 simple prompt-based tools",
    "Step 3: Learn an AI framework\n• LangChain for quick prototypes\n• LlamaIndex for RAG apps\n• Pick ONE, build 3 projects",
    "Step 4: Ship publicly\n• Your first AI tool will be bad\n• Ship it anyway\n• Iterate based on real user feedback",
    "That's it. No degree needed. Just consistent work.\n\nWhat's stopping you? Let me know below 👇"
  ],
  "createdAt": "2026-01-23T10:05:00Z",
  "provider": "mock",
  "model": "mock-gpt-4"
}
```

**Variant 3: Provocative (topic contains "truth" or "nobody")**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440003",
  "tweets": [
    "🔥 Unpopular truth: Most AI \"developers\" are just API wrapper builders. And that's perfectly fine. Thread 👇",
    "Everyone acts like you need a PhD to work with AI. You don't. You need to understand APIs and write good prompts.",
    "\"But that's not real AI development!\" Neither is most web development \"real\" programming. You're using frameworks and libraries built by others.",
    "The real skill isn't training models from scratch. It's understanding user problems and building solutions that actually work.",
    "I've made $50k this year building \"simple\" AI wrappers. Meanwhile, ML engineers argue on Twitter about transformers.",
    "Stop gatekeeping. Start building. The market rewards solutions, not complexity.",
    "Agree? Disagree? Hit me with your honest take below 👇"
  ],
  "createdAt": "2026-01-23T10:10:00Z",
  "provider": "mock",
  "model": "mock-gpt-4"
}
```

**Variant 4: Storytelling (topic contains "story" or "journey")**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440004",
  "tweets": [
    "I launched my SaaS 6 months ago. Made $0 for 4 months. Today I hit $10k MRR. Here's the entire story 🧵",
    "Month 1-2: Built the product alone. Nights and weekends. Told nobody. Classic mistake #1.",
    "Month 3: Launched on Product Hunt. Got 200 upvotes. 50 signups. Zero paid customers. I was devastated.",
    "Month 4: Started posting daily on Twitter. Shared my struggles openly. Something shifted. People started actually caring.",
    "Month 5: Got my first paying customer from Twitter. $29/month. I literally cried. That validation changed everything.",
    "Month 6: Doubled down on content. Added features users ASKED for (not what I wanted to build). Hit $10k MRR today.",
    "Key lesson: Build in public from day one. Your future customers are watching.\n\nWhat's your biggest launch mistake? 👇"
  ],
  "createdAt": "2026-01-23T10:15:00Z",
  "provider": "mock",
  "model": "mock-gpt-4"
}
```

**Variant 5: Analytical (default for all other topics)**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440005",
  "tweets": [
    "I analyzed 500 viral tweets in my niche. Here are the 5 patterns that consistently work 📊",
    "Pattern 1: The Hook Formula\n• Start with a number or emoji\n• Promise a clear benefit\n• 92% of viral tweets do this",
    "Pattern 2: White Space Matters\n• Break long sentences into multiple lines\n• Use bullet points\n• Engagement jumps 34%",
    "Pattern 3: The CTA is crucial\n• 78% of high-performing threads end with a question\n• \"What do you think?\" beats \"Follow for more\"",
    "Pattern 4: Timing\n• Tuesday-Thursday, 9am-11am EST\n• Avoid weekends unless your niche is active then\n• Data doesn't lie",
    "Pattern 5: First tweet wins\n• If tweet 1 doesn't hook, thread dies\n• Spend 80% of your time on the opener",
    "Which pattern surprised you most? Reply with the number 👇"
  ],
  "createdAt": "2026-01-23T10:20:00Z",
  "provider": "mock",
  "model": "mock-gpt-4"
}
```

#### Error Cases

No real errors in mock mode, but must handle these scenarios:

- Empty topic → Return Variant 5 (Analytical)
- Topic too long (>120 chars) → Truncate to 120 chars, then select variant
- Invalid tone/audience → Ignore, select variant based on topic only
- Simulated network delay must be between 500-1500ms (random)

#### Acceptance Criteria

- [ ] Mock data returns within 500-1500ms (simulated delay)
- [ ] Response structure exactly matches `GenerateThreadResponseDto`
- [ ] All 5 variants are implemented with distinct content
- [ ] Topic keyword matching is case-insensitive
- [ ] Each tweet in mock data is ≤280 characters
- [ ] Each variant has 5-7 tweets
- [ ] UUIDs are valid format (can be hardcoded as shown)
- [ ] Timestamps are ISO 8601 format
- [ ] Provider field is "mock" when mock data is active
- [ ] Frontend can toggle between mock and real API via environment variable
- [ ] No console errors when using mock data
- [ ] Mock data works without any backend running

#### Implementation Location

**Frontend (Angular):**

Create new file: `src/app/services/mock-thread-data.service.ts`
- Implement mock data selection logic
- Return mock responses based on topic keyword matching

Modify: `src/app/services/thread.service.ts` (or equivalent)
- Check `environment.useMockData` flag
- If true, use MockThreadDataService instead of HTTP client
- If false, use real API as normal

Modify: `src/environments/environment.development.ts`
- Add `useMockData: true`

Modify: `src/environments/environment.ts`
- Add `useMockData: false`

---

## Non-Goals (Explicitly Out of Scope)

- Mock data for authentication endpoints
- Mock data for other non-thread endpoints
- Configurable mock data via UI
- Saving mock threads to backend
- Mock data in production builds
- Advanced mock scenarios (loading states, specific errors)
- Mock data for future endpoints not yet built

---

## Execution Notes for Dev Agent

- Implement exactly the 5 variants shown above
- Match topic keywords case-insensitively using `.toLowerCase().includes()`
- Use Math.random() * 1000 + 500 for simulated delay
- Do NOT make this configurable—hardcode the 5 variants
- Do NOT generalize beyond these 5 scenarios
- Do NOT add additional mock data without user approval
- Ensure mock data can be toggled via environment variable only
- Test that frontend works completely offline when mock mode is enabled
