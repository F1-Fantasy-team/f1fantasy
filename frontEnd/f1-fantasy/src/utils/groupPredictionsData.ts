import type { GroupPredictionsData } from "../types/predictions";

/**
 * Create initial group predictions data (current user only, zero scores).
 * Used as the default shape before API data is loaded.
 */
export function createInitialGroupPredictionsData(
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
