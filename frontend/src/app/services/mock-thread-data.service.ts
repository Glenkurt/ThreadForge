import { Injectable } from '@angular/core';
import { Observable, of, delay } from 'rxjs';
import { GenerateThreadRequest, GenerateThreadResponse } from '../models/thread.model';

export interface MockThreadResponse {
  id: string;
  tweets: string[];
  createdAt: string;
  provider: string;
  model: string;
}

@Injectable({
  providedIn: 'root'
})
export class MockThreadDataService {
  private readonly mockVariants: MockThreadResponse[] = [
    // Variant 1: Indie Hacker (topic contains "startup" or "build")
    {
      id: '550e8400-e29b-41d4-a716-446655440001',
      tweets: [
        '🚀 Building in public is the indie hacker\'s secret weapon. Here\'s why transparency beats secrecy in 2026 👇',
        '1/ Traditional startups hide everything until launch. But indie hackers? We share our journey, mistakes, and revenue numbers openly.',
        '2/ This transparency builds trust before you even have a product. People want to support builders they know and believe in.',
        '3/ Plus, public feedback is gold. Your audience will tell you exactly what they need—before you waste months building the wrong thing.',
        '4/ I\'ve gained 10k followers in 6 months just by sharing my honest progress. No marketing budget needed.',
        'Want to start building in public? Drop a 👋 and I\'ll share my exact playbook (it\'s free).'
      ],
      createdAt: '2026-01-23T10:00:00Z',
      provider: 'mock',
      model: 'mock-gpt-4'
    },
    // Variant 2: Educational (topic contains "learn" or "how")
    {
      id: '550e8400-e29b-41d4-a716-446655440002',
      tweets: [
        '📚 Want to learn AI development in 2026? Here\'s your practical roadmap (no fluff) 🧵',
        'Step 1: Master Python basics\n• Variables, functions, classes\n• Work with APIs and JSON\n• 2-3 weeks if you focus',
        'Step 2: Understand how LLMs work\n• Read "Attention Is All You Need" (yes, really)\n• Play with prompts on ChatGPT\n• Build 3-5 simple prompt-based tools',
        'Step 3: Learn an AI framework\n• LangChain for quick prototypes\n• LlamaIndex for RAG apps\n• Pick ONE, build 3 projects',
        'Step 4: Ship publicly\n• Your first AI tool will be bad\n• Ship it anyway\n• Iterate based on real user feedback',
        'That\'s it. No degree needed. Just consistent work.\n\nWhat\'s stopping you? Let me know below 👇'
      ],
      createdAt: '2026-01-23T10:05:00Z',
      provider: 'mock',
      model: 'mock-gpt-4'
    },
    // Variant 3: Provocative (topic contains "truth" or "nobody")
    {
      id: '550e8400-e29b-41d4-a716-446655440003',
      tweets: [
        '🔥 Unpopular truth: Most AI "developers" are just API wrapper builders. And that\'s perfectly fine. Thread 👇',
        'Everyone acts like you need a PhD to work with AI. You don\'t. You need to understand APIs and write good prompts.',
        '"But that\'s not real AI development!" Neither is most web development "real" programming. You\'re using frameworks and libraries built by others.',
        'The real skill isn\'t training models from scratch. It\'s understanding user problems and building solutions that actually work.',
        'I\'ve made $50k this year building "simple" AI wrappers. Meanwhile, ML engineers argue on Twitter about transformers.',
        'Stop gatekeeping. Start building. The market rewards solutions, not complexity.',
        'Agree? Disagree? Hit me with your honest take below 👇'
      ],
      createdAt: '2026-01-23T10:10:00Z',
      provider: 'mock',
      model: 'mock-gpt-4'
    },
    // Variant 4: Storytelling (topic contains "story" or "journey")
    {
      id: '550e8400-e29b-41d4-a716-446655440004',
      tweets: [
        'I launched my SaaS 6 months ago. Made $0 for 4 months. Today I hit $10k MRR. Here\'s the entire story 🧵',
        'Month 1-2: Built the product alone. Nights and weekends. Told nobody. Classic mistake #1.',
        'Month 3: Launched on Product Hunt. Got 200 upvotes. 50 signups. Zero paid customers. I was devastated.',
        'Month 4: Started posting daily on Twitter. Shared my struggles openly. Something shifted. People started actually caring.',
        'Month 5: Got my first paying customer from Twitter. $29/month. I literally cried. That validation changed everything.',
        'Month 6: Doubled down on content. Added features users ASKED for (not what I wanted to build). Hit $10k MRR today.',
        'Key lesson: Build in public from day one. Your future customers are watching.\n\nWhat\'s your biggest launch mistake? 👇'
      ],
      createdAt: '2026-01-23T10:15:00Z',
      provider: 'mock',
      model: 'mock-gpt-4'
    },
    // Variant 5: Analytical (default for all other topics)
    {
      id: '550e8400-e29b-41d4-a716-446655440005',
      tweets: [
        'I analyzed 500 viral tweets in my niche. Here are the 5 patterns that consistently work 📊',
        'Pattern 1: The Hook Formula\n• Start with a number or emoji\n• Promise a clear benefit\n• 92% of viral tweets do this',
        'Pattern 2: White Space Matters\n• Break long sentences into multiple lines\n• Use bullet points\n• Engagement jumps 34%',
        'Pattern 3: The CTA is crucial\n• 78% of high-performing threads end with a question\n• "What do you think?" beats "Follow for more"',
        'Pattern 4: Timing\n• Tuesday-Thursday, 9am-11am EST\n• Avoid weekends unless your niche is active then\n• Data doesn\'t lie',
        'Pattern 5: First tweet wins\n• If tweet 1 doesn\'t hook, thread dies\n• Spend 80% of your time on the opener',
        'Which pattern surprised you most? Reply with the number 👇'
      ],
      createdAt: '2026-01-23T10:20:00Z',
      provider: 'mock',
      model: 'mock-gpt-4'
    }
  ];

  generateThread(request: GenerateThreadRequest): Observable<GenerateThreadResponse> {
    const variant = this.selectVariant(request.topic);
    const simulatedDelay = Math.floor(Math.random() * 1000) + 500; // 500-1500ms

    const response: GenerateThreadResponse = {
      id: variant.id,
      tweets: variant.tweets.slice(0, request.tweetCount || 5),
      createdAt: new Date().toISOString(),
      provider: variant.provider,
      model: variant.model
    };

    return of(response).pipe(delay(simulatedDelay));
  }

  private selectVariant(topic: string): MockThreadResponse {
    const lowerTopic = (topic || '').toLowerCase();

    if (lowerTopic.includes('startup') || lowerTopic.includes('build')) {
      return this.mockVariants[0]; // Indie Hacker
    }
    if (lowerTopic.includes('learn') || lowerTopic.includes('how')) {
      return this.mockVariants[1]; // Educational
    }
    if (lowerTopic.includes('truth') || lowerTopic.includes('nobody')) {
      return this.mockVariants[2]; // Provocative
    }
    if (lowerTopic.includes('story') || lowerTopic.includes('journey')) {
      return this.mockVariants[3]; // Storytelling
    }

    return this.mockVariants[4]; // Analytical (default)
  }
}
