import { apiGet, getApiBaseUrl } from "./client";
import type { RaceApi } from "./types";
import { getCurrentSeason } from "../constants/season";

/**
 * Fetch races for a season from the backend.
 * Returns null if API is not configured or request fails.
 */
export async function fetchRacesForSeasonFromApi(
  season: string = getCurrentSeason()
): Promise<RaceApi[] | null> {
  if (!getApiBaseUrl()) return null;
  try {
    const races = await apiGet<RaceApi[]>(`/api/race/${season}`);
    if (Array.isArray(races) && races.length > 0) {
      return races;
    }
  } catch (err) {
    if (import.meta.env.DEV) {
      console.warn("Races API failed:", err);
    }
  }
  return null;
}

/**
 * Get the date of the first race of the season (ISO date string, e.g. "2026-03-01").
 * Races are sorted by round; the first round's date is used for prediction lock.
 */
export function getFirstRaceDateFromRaces(races: RaceApi[]): string | null {
  if (races.length === 0) return null;
  const sorted = [...races].sort((a, b) => parseInt(a.round, 10) - parseInt(b.round, 10));
  const first = sorted[0];
  return first?.date?.trim() || null;
}
