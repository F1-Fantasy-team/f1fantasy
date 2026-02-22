import { apiGet, apiGetOptional, apiPost, apiPut, getApiBaseUrl } from "./client";
import type {
  PredictionCategoryId,
  DriversChampionshipPrediction,
  ConstructorsChampionshipPrediction,
  DriverDraftPrediction,
  DestructorsPrediction,
  MrSaturdayPrediction,
  ZeroPointersPrediction,
  WildcardPrediction,
  MemberStanding,
  CategoryScore,
  UserPredictions,
} from "../types/predictions";

import { getCurrentSeason } from "../constants/season";

/** Backend category key in categoryScoresJson */
const BACKEND_CATEGORY_TO_FRONT: Record<string, PredictionCategoryId> = {
  DriverChampionship: "driversChampionship",
  ConstructorChampionship: "constructorsChampionship",
  DriverDraft: "driverDraft",
  Destructor: "destructors",
  MrSaturday: "mrSaturday",
  ZeroPointer: "zeroPointers",
  Wildcard: "wildcard",
};

function parseCategoryScoresJson(jsonStr: string): CategoryScore[] {
  try {
    const raw = JSON.parse(jsonStr) as Record<string, number>;
    return Object.entries(raw).map(([key, score]) => ({
      categoryId: BACKEND_CATEGORY_TO_FRONT[key] ?? (key as PredictionCategoryId),
      score,
    }));
  } catch {
    return [];
  }
}

/** Backend standings item */
interface StandingApi {
  id: number;
  userId: string;
  groupId: number;
  totalScore: number;
  rank: number;
  categoryScoresJson: string;
  updatedAt: string;
}

export function mapStandingsApiToMemberStandings(
  list: StandingApi[],
  displayNames?: Record<string, string>
): MemberStanding[] {
  return list.map((s) => ({
    userId: s.userId,
    displayName: displayNames?.[s.userId] ?? s.userId,
    overallScore: s.totalScore,
    rank: s.rank,
    categoryScores: parseCategoryScoresJson(s.categoryScoresJson),
  }));
}

// ----- Driver championship -----
export async function fetchDriverChampionshipFromApi(
  groupId: string
): Promise<DriversChampionshipPrediction | null> {
  if (!getApiBaseUrl()) return null;
  try {
    const res = await apiGetOptional<{ rankedDriverIds?: string[] }>(
      `/api/predictions/groups/${groupId}/driver-championship`
    );
    if (!res?.rankedDriverIds?.length) return null;
    return res.rankedDriverIds.map((driverId, i) => ({
      position: i + 1,
      driverId,
    }));
  } catch {
    return null;
  }
}

export async function postDriverChampionshipFromApi(
  groupId: string,
  prediction: DriversChampionshipPrediction
): Promise<void> {
  const rankedDriverIds = [...prediction]
    .sort((a, b) => a.position - b.position)
    .map((e) => e.driverId);
  await apiPost(`/api/predictions/groups/${groupId}/driver-championship`, rankedDriverIds);
}

// ----- Constructor championship -----
export async function fetchConstructorChampionshipFromApi(
  groupId: string
): Promise<ConstructorsChampionshipPrediction | null> {
  if (!getApiBaseUrl()) return null;
  try {
    const res = await apiGetOptional<{ rankedConstructorIds?: string[] }>(
      `/api/predictions/groups/${groupId}/constructor-championship`
    );
    if (!res?.rankedConstructorIds?.length) return null;
    return res.rankedConstructorIds.map((constructorId, i) => ({
      position: i + 1,
      constructorId,
    }));
  } catch {
    return null;
  }
}

export async function postConstructorChampionshipFromApi(
  groupId: string,
  prediction: ConstructorsChampionshipPrediction
): Promise<void> {
  const rankedConstructorIds = [...prediction]
    .sort((a, b) => a.position - b.position)
    .map((e) => e.constructorId);
  await apiPost(`/api/predictions/groups/${groupId}/constructor-championship`, rankedConstructorIds);
}

// ----- Two-driver categories -----
async function fetchTwoDriverFromApi(
  groupId: string,
  path: string
): Promise<{ driverId1: string; driverId2: string } | null> {
  if (!getApiBaseUrl()) return null;
  try {
    const res = await apiGetOptional<{ driver1Id?: string; driver2Id?: string }>(path);
    if (res?.driver1Id != null && res?.driver2Id != null) {
      return { driverId1: res.driver1Id, driverId2: res.driver2Id };
    }
    return null;
  } catch {
    return null;
  }
}

async function postTwoDriverFromApi(
  path: string,
  body: { driverId1: string; driverId2: string }
): Promise<void> {
  await apiPost(path, {
    driver1Id: body.driverId1,
    driver2Id: body.driver2Id,
  });
}

export function fetchDriverDraftFromApi(
  groupId: string
): Promise<DriverDraftPrediction | null> {
  return fetchTwoDriverFromApi(
    groupId,
    `/api/predictions/groups/${groupId}/driver-draft`
  );
}

export function postDriverDraftFromApi(
  groupId: string,
  prediction: DriverDraftPrediction
): Promise<void> {
  return postTwoDriverFromApi(
    `/api/predictions/groups/${groupId}/driver-draft`,
    prediction
  );
}

export function fetchDestructorFromApi(
  groupId: string
): Promise<DestructorsPrediction | null> {
  return fetchTwoDriverFromApi(
    groupId,
    `/api/predictions/groups/${groupId}/destructor`
  );
}

export function postDestructorFromApi(
  groupId: string,
  prediction: DestructorsPrediction
): Promise<void> {
  return postTwoDriverFromApi(
    `/api/predictions/groups/${groupId}/destructor`,
    prediction
  );
}

export function fetchMrSaturdayFromApi(
  groupId: string
): Promise<MrSaturdayPrediction | null> {
  return fetchTwoDriverFromApi(
    groupId,
    `/api/predictions/groups/${groupId}/mr-saturday`
  );
}

export function postMrSaturdayFromApi(
  groupId: string,
  prediction: MrSaturdayPrediction
): Promise<void> {
  return postTwoDriverFromApi(
    `/api/predictions/groups/${groupId}/mr-saturday`,
    prediction
  );
}

// ----- Zero pointer (backend: DriverIds list) -----
export async function fetchZeroPointerFromApi(
  groupId: string
): Promise<ZeroPointersPrediction | null> {
  if (!getApiBaseUrl()) return null;
  try {
    const res = await apiGetOptional<{ driverIds?: string[] }>(
      `/api/predictions/groups/${groupId}/zero-pointer`
    );
    if (res?.driverIds != null && res.driverIds.length > 0) {
      return { driverIds: res.driverIds };
    }
    return null;
  } catch {
    return null;
  }
}

export async function postZeroPointerFromApi(
  groupId: string,
  prediction: ZeroPointersPrediction
): Promise<void> {
  await apiPost(`/api/predictions/groups/${groupId}/zero-pointer`, {
    driverIds: prediction.driverIds ?? [],
  });
}

// ----- Wildcard -----
export async function fetchWildcardFromApi(
  groupId: string
): Promise<WildcardPrediction | null> {
  if (!getApiBaseUrl()) return null;
  try {
    const res = await apiGetOptional<{
      statement?: string;
      pointsPotential?: number;
      fulfilled?: boolean;
      fullfilled?: boolean; /* backend typo */
    }>(`/api/predictions/groups/${groupId}/wildcard`);
    if (res?.statement != null) {
      return {
        statement: res.statement,
        pointsPotential: res.pointsPotential,
        fulfilled: res.fulfilled ?? res.fullfilled,
      };
    }
    return null;
  } catch {
    return null;
  }
}

export async function postWildcardFromApi(
  groupId: string,
  prediction: WildcardPrediction
): Promise<void> {
  await apiPost(`/api/predictions/groups/${groupId}/wildcard`, {
    statement: prediction.statement,
  });
}

// ----- Admin wildcard (points 100-200, fulfill) -----
/** Backend wildcard item from GET admin/groups/{groupId}/wildcards */
export interface AdminWildcardApi {
  id: number;
  userId: string;
  statement: string;
  pointsPotential?: number;
  fulfilled?: boolean;
}

export async function fetchAdminWildcardsFromApi(
  groupId: string
): Promise<AdminWildcardApi[] | null> {
  if (!getApiBaseUrl()) return null;
  try {
    const list = await apiGet<AdminWildcardApi[]>(
      `/api/admin/groups/${groupId}/wildcards`
    );
    return Array.isArray(list) ? list : null;
  } catch (err) {
    if (import.meta.env.DEV) console.warn("Fetch admin wildcards failed:", err);
    return null;
  }
}

/** Set wildcard points potential (100-200). Admin only. */
export async function putAdminWildcardPointsFromApi(
  groupId: string,
  userId: string,
  points: number
): Promise<void> {
  await apiPut(
    `/api/admin/groups/${groupId}/wildcard/${encodeURIComponent(userId)}/points`,
    { pointsPotential: points }
  );
}

/** Mark wildcard as fulfilled. Admin only. */
export async function putAdminWildcardFulfilledFromApi(
  groupId: string,
  userId: string,
  fulfilled: boolean
): Promise<void> {
  await apiPut(
    `/api/admin/groups/${groupId}/wildcard/${encodeURIComponent(userId)}/fulfilled`,
    { fullfilled: fulfilled }
  );
}

// ----- Standings -----
export async function fetchStandingsFromApi(
  groupId: string,
  season: string = getCurrentSeason()
): Promise<MemberStanding[] | null> {
  if (!getApiBaseUrl()) return null;
  try {
    const list = await apiGet<StandingApi[]>(
      `/api/standings/groups/${groupId}?season=${season}`
    );
    if (!Array.isArray(list)) return null;
    return mapStandingsApiToMemberStandings(list);
  } catch (err) {
    if (import.meta.env.DEV) console.warn("Fetch standings failed:", err);
    return null;
  }
}

/** Fetch only standings and empty predictions (no category GETs). Use on group open to avoid 404s for new groups. */
export async function fetchGroupStandingsOnlyFromApi(
  groupId: string,
  currentUserId: string,
  currentUserDisplayName: string
): Promise<{ standings: MemberStanding[]; predictions: UserPredictions } | null> {
  if (!getApiBaseUrl()) return null;
  try {
    const standings = await fetchStandingsFromApi(groupId);
    const predictions: UserPredictions = {
      userId: currentUserId,
      displayName: currentUserDisplayName,
    };
    return {
      standings: standings ?? [],
      predictions,
    };
  } catch (err) {
    if (import.meta.env.DEV) console.warn("Fetch group standings failed:", err);
    return null;
  }
}

/** Fetch a single category's prediction (used when user opens that category). Returns null on 404. */
export async function fetchCategoryPredictionFromApi(
  groupId: string,
  categoryId: PredictionCategoryId
): Promise<unknown> {
  switch (categoryId) {
    case "driversChampionship":
      return fetchDriverChampionshipFromApi(groupId);
    case "constructorsChampionship":
      return fetchConstructorChampionshipFromApi(groupId);
    case "driverDraft":
      return fetchDriverDraftFromApi(groupId);
    case "destructors":
      return fetchDestructorFromApi(groupId);
    case "mrSaturday":
      return fetchMrSaturdayFromApi(groupId);
    case "zeroPointers":
      return fetchZeroPointerFromApi(groupId);
    case "wildcard":
      return fetchWildcardFromApi(groupId);
    default:
      return null;
  }
}

/** Fetch all my predictions and standings for a group in parallel. (Use when you need full data; causes 404s for empty categories.) */
export async function fetchGroupPredictionsFromApi(
  groupId: string,
  currentUserId: string,
  currentUserDisplayName: string
): Promise<{ standings: MemberStanding[]; predictions: UserPredictions } | null> {
  if (!getApiBaseUrl()) return null;
  try {
    const [
      standings,
      driversChampionship,
      constructorsChampionship,
      driverDraft,
      destructor,
      mrSaturday,
      zeroPointer,
      wildcard,
    ] = await Promise.all([
      fetchStandingsFromApi(groupId),
      fetchDriverChampionshipFromApi(groupId),
      fetchConstructorChampionshipFromApi(groupId),
      fetchDriverDraftFromApi(groupId),
      fetchDestructorFromApi(groupId),
      fetchMrSaturdayFromApi(groupId),
      fetchZeroPointerFromApi(groupId),
      fetchWildcardFromApi(groupId),
    ]);

    const predictions: UserPredictions = {
      userId: currentUserId,
      displayName: currentUserDisplayName,
    };
    if (driversChampionship?.length) predictions.driversChampionship = driversChampionship;
    if (constructorsChampionship?.length) predictions.constructorsChampionship = constructorsChampionship;
    if (driverDraft) predictions.driverDraft = driverDraft;
    if (destructor) predictions.destructors = destructor;
    if (mrSaturday) predictions.mrSaturday = mrSaturday;
    if (zeroPointer) predictions.zeroPointers = zeroPointer;
    if (wildcard) predictions.wildcard = wildcard;

    return {
      standings: standings ?? [],
      predictions,
    };
  } catch (err) {
    if (import.meta.env.DEV) console.warn("Fetch group predictions failed:", err);
    return null;
  }
}
