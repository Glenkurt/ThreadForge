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

export interface GenerateThreadResponse {
  tweets: string[];
}
