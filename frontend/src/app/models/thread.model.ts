export interface GenerateThreadRequest {
  topic: string;
  tone: 'indie_hacker' | 'educational' | 'provocative' | 'direct' | null;
  audience: string | null;
  tweetCount: number;
  feedback?: string | null; // Optional feedback for regeneration
}

export interface GenerateThreadResponse {
  tweets: string[];
}
