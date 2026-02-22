/** Once true, locked predictions cannot be unlocked. Set at season start (e.g. first race). */
export const SEASON_STARTED = false;

/** Current F1 season year (e.g. "2026"). Derived from calendar year so it rolls over automatically. */
export function getCurrentSeason(): string {
  return String(new Date().getFullYear());
}

/** Fallback first race date (ISO) when backend does not send firstRaceDate. Used for system lock: before this = unlocked. */
export function getDefaultFirstRaceDate(): string {
  return `${getCurrentSeason()}-03-01`;
}
