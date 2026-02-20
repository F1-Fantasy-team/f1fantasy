import { apiGet, getApiBaseUrl } from "./client";
import type { DriverApi } from "./types";
import type { Driver } from "../types/driver";
import { MOCK_DRIVERS } from "../data/mockDrivers";

const CURRENT_SEASON = "2025";

/** Map backend API shape to app Driver (exported for tests). */
export function mapDriverApiToDriver(api: DriverApi): Driver {
  return {
    id: api.driverId.toLowerCase(),
    name: `${api.givenName} ${api.familyName}`.trim(),
    teamId: undefined,
  };
}

/**
 * Fetch drivers from backend. Returns null if API is not configured or request fails (caller should use mock).
 */
export async function fetchDriversFromApi(): Promise<Driver[] | null> {
  if (!getApiBaseUrl()) return null;
  try {
    const cached = await apiGet<DriverApi[] | { drivers?: DriverApi[] }>("/api/driver/cached");
    const list = Array.isArray(cached) ? cached : cached?.drivers;
    if (Array.isArray(list) && list.length > 0) {
      return list.map(mapDriverApiToDriver);
    }
    const bySeason = await apiGet<DriverApi[]>(`/api/driver/season/${CURRENT_SEASON}`);
    if (Array.isArray(bySeason) && bySeason.length > 0) {
      return bySeason.map(mapDriverApiToDriver);
    }
  } catch (err) {
    if (import.meta.env.DEV) {
      console.warn("Drivers API failed (using offline list):", err);
    }
  }
  return null;
}

export function getMockDrivers(): Driver[] {
  return MOCK_DRIVERS;
}
