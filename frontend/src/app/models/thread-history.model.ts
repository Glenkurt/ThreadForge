export type ThreadHistoryListItem = {
  id: string;
  createdAt: string;
  topicPreview: string;
  tweetCount: number;
  firstTweetPreview: string;
  provider: string;
  model: string;
};

export type ThreadHistoryDetail = {
  id: string;
  createdAt: string;
  request: Record<string, unknown>;
  tweets: string[];
  provider: string;
  model: string;
};
