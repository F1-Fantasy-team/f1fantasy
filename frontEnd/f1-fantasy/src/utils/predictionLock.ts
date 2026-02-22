import type { Group } from "../types/group";
import type { GroupPredictionsData } from "../types/predictions";
import { getDefaultFirstRaceDate } from "../constants/season";

function isTodayBeforeOrOnRaceDay(firstRaceDateIso: string): boolean {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const firstRace = new Date(firstRaceDateIso);
  firstRace.setHours(0, 0, 0, 0);
  return today.getTime() >= firstRace.getTime();
}

/**
 * System-controlled lock: predictionLock true → locked;
 * predictionLock false (or no backend data) → locked when today >= firstRaceDate (season started).
 * firstRaceDateFromRaces: from GET /api/race/{season}, used when data.firstRaceDate is not set.
 */
export function getSystemPredictionsLocked(
  data: GroupPredictionsData,
  firstRaceDateFromRaces?: string | null
): boolean {
  if (data.predictionLock === true) return true;
  const first =
    data.firstRaceDate ?? firstRaceDateFromRaces ?? getDefaultFirstRaceDate();
  return isTodayBeforeOrOnRaceDay(first);
}

/**
 * Effective group lock from group settings and data.
 * - admin: use adminSetPredictionsLocked (default false).
 * - system: use system lock.
 * - hybrid: use adminLockOverride if set, else system lock.
 * firstRaceDateFromRaces: from GET /api/race/{season}, used for system lock when data.firstRaceDate is not set.
 */
export function getEffectiveGroupLocked(
  group: Group,
  data: GroupPredictionsData,
  firstRaceDateFromRaces?: string | null
): boolean {
  const mode = group.predictionLockMode ?? "hybrid";
  if (mode === "admin") {
    return data.adminSetPredictionsLocked ?? false;
  }
  const systemLocked = getSystemPredictionsLocked(data, firstRaceDateFromRaces);
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
  userId: string,
  firstRaceDateFromRaces?: string | null
): boolean {
  if (getEffectiveGroupLocked(group, data, firstRaceDateFromRaces)) return true;
  return (data.lockedUserIds ?? []).includes(userId);
}

/**
 * In hybrid/system mode, user can only unlock if not group-locked.
 * For admin mode, only admin can change lock state.
 */
export function canUserUnlockSelf(
  group: Group,
  data: GroupPredictionsData,
  userId: string,
  firstRaceDateFromRaces?: string | null
): boolean {
  const mode = group.predictionLockMode ?? "hybrid";
  if (mode === "admin") return false; // only admin toggles lock
  if (getEffectiveGroupLocked(group, data, firstRaceDateFromRaces)) return false; // group locked
  return (data.lockedUserIds ?? []).includes(userId);
}

export function isGroupAdmin(group: Group, userId: string): boolean {
  return group.adminUserId === userId;
}
