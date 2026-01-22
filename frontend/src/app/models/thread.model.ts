export interface GenerateThreadRequest {
  topic: string;
  tone: 'indie_hacker' | 'educational' | 'provocative' | 'direct' | null;
  audience: string | null;
  tweetCount: number; // Backend accepts any valid integer
}

export interface GenerateThreadResponse {
  tweets: string[];
}
