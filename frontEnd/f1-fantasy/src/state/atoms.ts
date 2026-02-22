import { atom, atomFamily } from "recoil";
import type { Group } from "../types/group";
import type { GroupPredictionsData } from "../types/predictions";
import type { Driver } from "../types/driver";
import type { Constructor } from "../types/constructor";

export const selectedGroupIdState = atom<string | null>({
  key: "selectedGroupId",
  default: null,
});

export const userGroupsState = atom<Group[]>({
  key: "userGroups",
  default: [],
});

/** All groups (for join-by-code lookup); backend would replace this. */
export const allGroupsState = atom<Group[]>({
  key: "allGroups",
  default: [],
});

/** Selected prediction category (clicked from group page); null = show group overview. */
export const selectedCategoryIdState = atom<string | null>({
  key: "selectedCategoryId",
  default: null,
});

/** Per-group predictions data (including lock state). Filled from API when group is opened. */
export const groupPredictionsState = atomFamily<GroupPredictionsData | null, string>({
  key: "groupPredictions",
  default: null,
});

/** Drivers list. Fetched from API when a group is selected. */
export const driversState = atom<Driver[]>({
  key: "drivers",
  default: [],
});

/** Constructors list. Fetched from API when a group is selected. */
export const constructorsState = atom<Constructor[]>({
  key: "constructors",
  default: [],
});

/** True when drivers were loaded from the API. */
export const driversFromApiState = atom<boolean>({
  key: "driversFromApi",
  default: false,
});

/** True when constructors were loaded from the API. */
export const constructorsFromApiState = atom<boolean>({
  key: "constructorsFromApi",
  default: false,
});

/** First race date (ISO) for current season from GET /api/race/{season}. Used for system prediction lock. */
export const firstRaceDateState = atom<string | null>({
  key: "firstRaceDate",
  default: null,
});

/** True while drivers/constructors/races (or other app data) are being loaded on demand. Show global loading UI when true. */
export const appDataLoadingState = atom<boolean>({
  key: "appDataLoading",
  default: false,
});
