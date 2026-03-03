import { apiGet, apiPost, apiPut, apiDelete, getApiBaseUrl } from "./client";
import type { Group } from "../types/group";
import type { PredictionLockMode } from "../types/group";

/** Backend group member (in group response). */
export interface GroupMemberApi {
  id: number;
  groupId: number;
  userId: string;
  /** Optional display name for this user within the group. */
  displayName?: string;
  joinedAt: string;
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
