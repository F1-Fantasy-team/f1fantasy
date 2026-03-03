import { apiGet, apiPost, apiPut, apiDelete, getApiBaseUrl } from "./client";
import type { Group } from "../types/group";
import type { PredictionLockMode } from "../types/group";
import type { UserPredictions } from "../types/predictions";

/** Backend group member (in group response). */
export interface GroupMemberApi {
  id: number;
  groupId: number;
  userId: string;
  /** Optional display name for this user within the group. */
  displayName?: string;
  isAdmin?: boolean;
  joinedAt: string;
}

interface RankedDriversApi {
  rankedDriverIds?: string[];
}

interface RankedConstructorsApi {
  rankedConstructorIds?: string[];
}

interface TwoDriverApi {
  driver1Id?: string | null;
  driver2Id?: string | null;
}

interface ZeroPointerApi {
  driverIds?: string[];
}

interface WildcardApi {
  statement?: string;
  pointsPotential?: number;
  fulfilled?: boolean;
  fullfilled?: boolean;
}

export interface GroupMemberDetailApi extends GroupMemberApi {
  driverChampionship?: RankedDriversApi | null;
  constructorChampionship?: RankedConstructorsApi | null;
  driverDraft?: TwoDriverApi | null;
  destructor?: TwoDriverApi | null;
  mrSaturday?: TwoDriverApi | null;
  zeroPointer?: ZeroPointerApi | null;
  wildcard?: WildcardApi | null;
}

/** Backend group response (id is number). */
export interface GroupApi {
  id: number;
  name: string;
  inviteCode: string;
  lockMode: "admin" | "system" | "hybrid";
  adminUserId: string;
  predictionsLocked: boolean;
  createdAt: string;
  lockedAt?: string | null;
  members?: GroupMemberApi[];
}

export interface GroupDetailApi extends Omit<GroupApi, "members"> {
  members?: GroupMemberDetailApi[];
}

function mapGroupApiToGroup(api: GroupApi): Group {
  return {
    id: String(api.id),
    name: api.name,
    memberCount: Array.isArray(api.members) ? api.members.length : 0,
    createdAt: api.createdAt,
    inviteCode: api.inviteCode || undefined,
    adminUserId: api.adminUserId || undefined,
    predictionLockMode: api.lockMode as PredictionLockMode,
    predictionsLocked: api.predictionsLocked,
    members: api.members?.map((m) => ({
      userId: m.userId,
      displayName: m.displayName,
    })),
  };
}

function mapMemberPredictionApiToUserPrediction(member: GroupMemberDetailApi): UserPredictions {
  const mapped: UserPredictions = {
    userId: member.userId,
    displayName: member.displayName ?? member.userId,
  };

  const rankedDriverIds = member.driverChampionship?.rankedDriverIds;
  if (Array.isArray(rankedDriverIds) && rankedDriverIds.length > 0) {
    mapped.driversChampionship = rankedDriverIds.map((driverId, index) => ({
      position: index + 1,
      driverId,
    }));
  }

  const rankedConstructorIds = member.constructorChampionship?.rankedConstructorIds;
  if (Array.isArray(rankedConstructorIds) && rankedConstructorIds.length > 0) {
    mapped.constructorsChampionship = rankedConstructorIds.map((constructorId, index) => ({
      position: index + 1,
      constructorId,
    }));
  }

  const draft1 = member.driverDraft?.driver1Id ?? undefined;
  const draft2 = member.driverDraft?.driver2Id ?? undefined;
  if (draft1 && draft2) {
    mapped.driverDraft = {
      driverId1: draft1,
      driverId2: draft2,
    };
  }

  const destructor1 = member.destructor?.driver1Id ?? undefined;
  const destructor2 = member.destructor?.driver2Id ?? undefined;
  if (destructor1 && destructor2) {
    mapped.destructors = {
      driverId1: destructor1,
      driverId2: destructor2,
    };
  }

  const mrSaturday1 = member.mrSaturday?.driver1Id ?? undefined;
  const mrSaturday2 = member.mrSaturday?.driver2Id ?? undefined;
  if (mrSaturday1 && mrSaturday2) {
    mapped.mrSaturday = {
      driverId1: mrSaturday1,
      driverId2: mrSaturday2,
    };
  }

  if (member.zeroPointer && Array.isArray(member.zeroPointer.driverIds)) {
    mapped.zeroPointers = { driverIds: member.zeroPointer.driverIds };
  }

  if (member.wildcard?.statement) {
    mapped.wildcard = {
      statement: member.wildcard.statement,
      pointsPotential: member.wildcard.pointsPotential,
      fulfilled: member.wildcard.fulfilled ?? member.wildcard.fullfilled,
    };
  }

  return mapped;
}

export function mapGroupDetailApiToUserPredictions(api: GroupDetailApi): UserPredictions[] {
  if (!Array.isArray(api.members) || api.members.length === 0) return [];
  return api.members.map(mapMemberPredictionApiToUserPrediction);
}

/**
 * Create a group. Requires auth.
 * Returns null if API is not configured or request fails.
 */
export async function createGroupFromApi(
  name: string,
  lockMode: PredictionLockMode
): Promise<Group | null> {
  if (!getApiBaseUrl()) return null;
  try {
    const res = await apiPost<GroupApi>("/api/groups", { name, lockMode });
    return mapGroupApiToGroup(res);
  } catch (err) {
    if (import.meta.env.DEV) console.warn("Create group API failed:", err);
    return null;
  }
}

/**
 * Fetch all groups the current user is a member of.
 * Returns null if API is not configured or request fails.
 */
export async function fetchMyGroupsFromApi(): Promise<Group[] | null> {
  if (!getApiBaseUrl()) return null;
  try {
    const list = await apiGet<GroupApi[]>("/api/groups");
    if (!Array.isArray(list)) return null;
    return list.map(mapGroupApiToGroup);
  } catch (err) {
    if (import.meta.env.DEV) console.warn("Fetch groups API failed:", err);
    return null;
  }
}

/**
 * Get a group by ID. Returns null if not found or API error.
 */
export async function fetchGroupFromApi(groupId: string): Promise<Group | null> {
  if (!getApiBaseUrl()) return null;
  try {
    const res = await apiGet<GroupApi>(`/api/groups/${groupId}`);
    return mapGroupApiToGroup(res);
  } catch (err) {
    if (import.meta.env.DEV) console.warn("Fetch group API failed:", err);
    return null;
  }
}

/**
 * Get full group details by ID (includes member predictions).
 * Returns null if not found or API error.
 */
export async function fetchGroupDetailFromApi(groupId: string): Promise<GroupDetailApi | null> {
  if (!getApiBaseUrl()) return null;
  try {
    const res = await apiGet<GroupDetailApi>(`/api/groups/${groupId}`);
    return res;
  } catch (err) {
    if (import.meta.env.DEV) console.warn("Fetch group detail API failed:", err);
    return null;
  }
}

/**
 * Get a group by invite code (for join flow).
 * Returns null if not found or API error.
 */
export async function fetchGroupByInviteCodeFromApi(inviteCode: string): Promise<Group | null> {
  if (!getApiBaseUrl()) return null;
  const code = encodeURIComponent(inviteCode.trim());
  try {
    const res = await apiGet<GroupApi>(`/api/groups/invite/${code}`);
    return mapGroupApiToGroup(res);
  } catch (err) {
    if (import.meta.env.DEV) console.warn("Fetch group by invite API failed:", err);
    return null;
  }
}

/**
 * Join a group. Throws on API error (e.g. 409 already member).
 */
export async function joinGroupFromApi(groupId: string): Promise<void> {
  await apiPost(`/api/groups/${groupId}/join`);
}

/**
 * Leave a group. Throws on API error.
 */
export async function leaveGroupFromApi(groupId: string): Promise<void> {
  await apiPost(`/api/groups/${groupId}/leave`);
}

/**
 * Rename a group (admin only). Throws on API error.
 */
export async function renameGroupFromApi(groupId: string, name: string): Promise<void> {
  await apiPut(`/api/groups/${groupId}`, { name });
}

/**
 * Delete a group (admin only). Throws on API error.
 */
export async function deleteGroupFromApi(groupId: string): Promise<void> {
  await apiDelete(`/api/groups/${groupId}`);
}
