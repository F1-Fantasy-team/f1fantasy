import type { GroupPredictionsData } from "../types/predictions";

/** Group predictions: empty by default. Add data locally for dev. Do not commit real mock data. */
export const MOCK_GROUP_PREDICTIONS: Record<string, GroupPredictionsData> = {};

export function getGroupPredictionsData(_groupId: string): GroupPredictionsData | undefined {
  return undefined;
}

export function getOrCreateGroupPredictionsData(
  groupId: string,
  currentUserId: string,
  currentUserDisplayName: string
): GroupPredictionsData {
  return {
    groupId,
    standings: [
      {
        userId: currentUserId,
        displayName: currentUserDisplayName,
        overallScore: 0,
        rank: 1,
        categoryScores: [],
      },
    ],
    predictions: [
      {
        userId: currentUserId,
        displayName: currentUserDisplayName,
      },
    ],
  };
}
