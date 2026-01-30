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

export interface RegenerateTweetRequest {
  feedback?: string;
}

export interface RegenerateTweetResponse {
  tweet: string;
  index: number;
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

export interface RegenerateTweetRequest {
  feedback?: string;
}

export interface RegenerateTweetResponse {
  tweet: string;
  index: number;
}

// Tweet Improver types
export type ImprovementType = 'more_engaging' | 'more_concise' | 'more_clear' | 'more_viral' | 'more_professional' | 'more_casual';

export interface ImproveTweetRequest {
  draft: string;
  improvementType?: ImprovementType | null;
  tone?: ToneValue;
  preserveElements?: string | null;
  additionalInstructions?: string | null;
}

export interface ImproveTweetResponse {
  original: string;
  improved: string;
  alternatives: string[];
  explanation: string;
  characterCount: number;
  isWithinLimit: boolean;
  model: string;
}

export const IMPROVEMENT_TYPES: { value: ImprovementType; label: string; description: string }[] = [
  { value: 'more_engaging', label: 'More Engaging', description: 'Add hooks, questions, or bold statements' },
  { value: 'more_concise', label: 'More Concise', description: 'Shorter and punchier' },
  { value: 'more_clear', label: 'More Clear', description: 'Clearer and easier to understand' },
  { value: 'more_viral', label: 'More Viral', description: 'Optimize for shares and engagement' },
  { value: 'more_professional', label: 'More Professional', description: 'Polished and authoritative' },
  { value: 'more_casual', label: 'More Casual', description: 'Conversational and friendly' }
];
