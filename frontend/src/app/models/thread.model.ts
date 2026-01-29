export type ToneValue = 'indie_hacker' | 'professional' | 'humorous' | 'motivational' | 'educational' | 'provocative' | 'storytelling' | 'clear_practical' | null;
export type HookStrength = 'bold' | 'question' | 'story' | 'stat' | null;
export type CtaType = 'soft' | 'direct' | 'question' | null;

export interface StylePreferences {
  useEmojis?: boolean | null;
  useNumbering?: boolean | null;
  maxCharsPerTweet?: number | null;
  hookStrength?: HookStrength;
  ctaType?: CtaType;
}

export interface GenerateThreadRequest {
  topic: string;
  tone: ToneValue;
  audience: string | null;
  tweetCount: number;
  keyPoints?: string[] | null;
  feedback?: string | null;
  brandGuidelines?: string | null;
  exampleThreads?: string[] | null;
  stylePreferences?: StylePreferences | null;
}

export interface ThreadQuality {
  hookScore: number;
  ctaScore: number;
  overallScore: number;
  warnings: string[];
  suggestions: string[];
}

export interface GenerateThreadResponse {
  id: string;
  tweets: string[];
  createdAt: string;
  provider: string;
  model: string;
  hashtags?: string[];
  quality?: ThreadQuality;
}

// Predefined feedback suggestions for quick selection
export const FEEDBACK_SUGGESTIONS = [
  'Make it more controversial',
  'Add specific numbers/statistics',
  'Shorter sentences',
  'Stronger hook',
  'Less marketing-y',
  'More actionable advice',
  'Add a question hook',
  'Make it more personal/story-driven'
] as const;

// Single tweet regeneration
export interface RegenerateTweetRequest {
  feedback?: string;
}

export interface RegenerateTweetResponse {
  tweet: string;
  index: number;
}
