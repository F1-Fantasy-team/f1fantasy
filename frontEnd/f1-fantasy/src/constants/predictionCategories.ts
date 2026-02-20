import type { PredictionCategoryId } from "../types/predictions";

export const CATEGORY_LABELS: Record<PredictionCategoryId, string> = {
  driversChampionship: "Drivers Championship",
  constructorsChampionship: "Constructors Championship",
  driverDraft: "Driver Draft",
  destructors: "Destructors Championship",
  mrSaturday: "Mr Saturday",
  zeroPointers: "0 Pointers",
  wildcard: "Wildcard",
};

export const CATEGORY_DESCRIPTIONS: Record<PredictionCategoryId, string> = {
  driversChampionship: "Predict the drivers' championship standings at the end of the season.",
  constructorsChampionship: "Predict the constructors' championship standings at the end of the season.",
  driverDraft: "Pick 2 drivers; you get their combined points through the season.",
  destructors: "Pick 2 drivers; 10 points every time they have a crash.",
  mrSaturday: "Pick 2 drivers; 10 points every time they beat their teammate in qualifying.",
  zeroPointers: "Predict who will finish the season with 0 points.",
  wildcard: "Make a wildcard statement; big points if it comes true.",
};

export const CATEGORY_IDS: PredictionCategoryId[] = [
  "driversChampionship",
  "constructorsChampionship",
  "driverDraft",
  "destructors",
  "mrSaturday",
  "zeroPointers",
  "wildcard",
];
