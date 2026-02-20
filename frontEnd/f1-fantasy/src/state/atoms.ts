import { atom, atomFamily } from "recoil";
import type { Group } from "../types/group";
import type { GroupPredictionsData } from "../types/predictions";
import type { Driver } from "../types/driver";
import type { Constructor } from "../types/constructor";
import { MOCK_DRIVERS } from "../data/mockDrivers";
import { MOCK_CONSTRUCTORS } from "../data/mockConstructors";

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

/** Per-group predictions data (including lock state). Initialized from mock when group is first opened. */
export const groupPredictionsState = atomFamily<GroupPredictionsData | null, string>({
  key: "groupPredictions",
  default: null,
});

/** Drivers list. Fetched from API when VITE_API_BASE_URL is set; otherwise mock. */
export const driversState = atom<Driver[]>({
  key: "drivers",
  default: MOCK_DRIVERS,
});

/** Constructors list. Fetched from API when VITE_API_BASE_URL is set; otherwise mock. */
export const constructorsState = atom<Constructor[]>({
  key: "constructors",
  default: MOCK_CONSTRUCTORS,
});

/** True when drivers were loaded from the API (false = using offline mock list). */
export const driversFromApiState = atom<boolean>({
  key: "driversFromApi",
  default: false,
});

/** True when constructors were loaded from the API (false = using offline mock list). */
export const constructorsFromApiState = atom<boolean>({
  key: "constructorsFromApi",
  default: false,
});
