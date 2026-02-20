import { apiGet, getApiBaseUrl } from "./client";
import type { ConstructorApi } from "./types";
import type { Constructor } from "../types/constructor";
import { MOCK_CONSTRUCTORS } from "../data/mockConstructors";

const CURRENT_SEASON = "2026";

/** Map backend API shape to app Constructor (exported for tests). */
export function mapConstructorApiToConstructor(api: ConstructorApi): Constructor {
  return {
    id: api.constructorId.toLowerCase(),
    name: api.name,
  };
}

/**
 * Fetch constructors for the current season from backend.
 * Returns null if API is not configured or request fails (caller should use mock).
 * Only uses season endpoint so we only display current-season constructors.
 */
export async function fetchConstructorsFromApi(): Promise<Constructor[] | null> {
  if (!getApiBaseUrl()) return null;
  try {
    const bySeason = await apiGet<ConstructorApi[]>(`/api/constructor/season/${CURRENT_SEASON}`);
    if (Array.isArray(bySeason) && bySeason.length > 0) {
      return bySeason.map(mapConstructorApiToConstructor);
    }
  } catch (err) {
    if (import.meta.env.DEV) {
      console.warn("Constructors API failed (using offline list):", err);
    }
  }
  return null;
}

export function getMockConstructors(): Constructor[] {
  return MOCK_CONSTRUCTORS;
}
