import { apiGet, getApiBaseUrl } from "./client";
import type { DriverApi } from "./types";
import type { Driver } from "../types/driver";
import { getCurrentSeason } from "../constants/season";

/** Map backend API shape to app Driver (exported for tests). */
export function mapDriverApiToDriver(api: DriverApi): Driver {
  return {
    id: api.driverId.toLowerCase(),
    name: `${api.givenName} ${api.familyName}`.trim(),
    teamId: undefined,
    wikipediaUrl: api.url?.trim() || undefined,
  };
}

/**
 * Fetch drivers for the current season from the API.
 * Returns null if API is not configured or request fails.
 */
export async function fetchDriversFromApi(): Promise<Driver[] | null> {
  if (!getApiBaseUrl()) return null;
  try {
    const bySeason = await apiGet<DriverApi[]>(`/api/driver/season/${getCurrentSeason()}`);
    if (Array.isArray(bySeason) && bySeason.length > 0) {
      return bySeason.map(mapDriverApiToDriver);
    }
  } catch (err) {
    if (import.meta.env.DEV) console.warn("Drivers API failed:", err);
  }
  return null;
}
