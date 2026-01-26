export interface ProfileAnalysisRequest {
  username: string;
}

export interface ProfileAnalysisResponse {
  username: string;
  profileUrl: string;
  analyzedAt: string;
  tweetCount: number;
  brandDescription: BrandDescription;
}

export interface BrandDescription {
  overview: string;
  brandVoice: BrandVoice;
  targetAudience: TargetAudience;
  contentPillars: string[];
  contentPatterns: ContentPatterns;
  engagementInsights: EngagementInsights;
  uniqueDifferentiators: string[];
  recommendedStrategy: RecommendedStrategy;
}

export interface BrandVoice {
  tone: string;
  style: string;
  personality: string;
}

export interface TargetAudience {
  primary: string;
  demographics: string;
  painPoints: string[];
}

export interface ContentPatterns {
  format: string;
  length: string;
  structure: string;
}

export interface EngagementInsights {
  topPerformingContent: string[];
  callToActionStyle: string;
  postingFrequency: string;
}

export interface RecommendedStrategy {
  contentTypes: string[];
  toneGuidance: string;
  topicsToExplore: string[];
}
