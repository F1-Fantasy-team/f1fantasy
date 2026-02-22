import type { Group } from "../types/group";
import type { GroupPredictionsData } from "../types/predictions";

/**
 * System-controlled lock: no backend data → locked; predictionLock true → locked;
 * predictionLock false → locked when today >= firstRaceDate (season started).
 */
export function getSystemPredictionsLocked(data: GroupPredictionsData): boolean {
  if (data.predictionLock === true) return true;
  if (data.predictionLock === false) {
    const first = data.firstRaceDate;
    if (!first) return true; // no date: stay locked to be safe
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const firstRace = new Date(first);
    firstRace.setHours(0, 0, 0, 0);
    return today.getTime() >= firstRace.getTime();
  }
  return true; // no backend data: assume locked until we get data
}

/**
 * Effective group lock from group settings and data.
 * - admin: use adminSetPredictionsLocked (default false).
 * - system: use system lock.
 * - hybrid: use adminLockOverride if set, else system lock.
 */
export function getEffectiveGroupLocked(
  group: Group,
  data: GroupPredictionsData
): boolean {
  const mode = group.predictionLockMode ?? "hybrid";
  if (mode === "admin") {
    return data.adminSetPredictionsLocked ?? false;
  }
  const systemLocked = getSystemPredictionsLocked(data);
  if (mode === "system") return systemLocked;
  if (data.adminLockOverride !== undefined) return data.adminLockOverride;
  return systemLocked;
}

/**
 * User is locked if the group is effectively locked or they are in lockedUserIds.
 */
export function isUserLocked(
  group: Group,
  data: GroupPredictionsData,
  userId: string
): boolean {
  if (getEffectiveGroupLocked(group, data)) return true;
  return (data.lockedUserIds ?? []).includes(userId);
}

/**
 * In hybrid/system mode, user can only unlock if not group-locked (and season not started was the old rule).
 * For admin mode, only admin can change lock state.
 */
export function canUserUnlockSelf(
  group: Group,
  data: GroupPredictionsData,
  userId: string
): boolean {
  const mode = group.predictionLockMode ?? "hybrid";
  if (mode === "admin") return false; // only admin toggles lock
  if (getEffectiveGroupLocked(group, data)) return false; // group locked
  return (data.lockedUserIds ?? []).includes(userId);
}

export function isGroupAdmin(group: Group, userId: string): boolean {
  return group.adminUserId === userId;
}
