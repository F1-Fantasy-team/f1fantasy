/** Prediction category identifiers */
export type PredictionCategoryId =
  | "driversChampionship"
  | "constructorsChampionship"
  | "driverDraft"
  | "destructors"
  | "mrSaturday"
  | "zeroPointers"
  | "wildcard";

/** Driver championship: predicted final standings (position -> driver id) */
export type DriversChampionshipPrediction = { position: number; driverId: string }[];

/** Constructors championship: predicted final standings */
export type ConstructorsChampionshipPrediction = { position: number; constructorId: string }[];

/** Driver Draft: 2 drivers; score = combined season points */
export type DriverDraftPrediction = { driverId1: string; driverId2: string };

/** Destructors: 2 drivers; 10 pts per crash */
export type DestructorsPrediction = { driverId1: string; driverId2: string };

/** Mr Saturday: 2 drivers; 10 pts per quali beat of teammate */
export type MrSaturdayPrediction = { driverId1: string; driverId2: string };

/** 0 Pointers: drivers predicted to finish with 0 points */
export type ZeroPointersPrediction = { driverIds: string[] };

/** Wildcard: one free-form statement per user; points if it comes true. Admin sets points potential and marks fulfilled. */
export type WildcardPrediction = {
  statement: string;
  /** Admin-set: higher = more "unhinged" prediction, higher score potential when fulfilled. */
  pointsPotential?: number;
  /** Admin ticks when prediction has come true so points can be counted. */
  fulfilled?: boolean;
};

export interface UserPredictions {
  userId: string;
  displayName: string;
  driversChampionship?: DriversChampionshipPrediction;
  constructorsChampionship?: ConstructorsChampionshipPrediction;
  driverDraft?: DriverDraftPrediction;
  destructors?: DestructorsPrediction;
  mrSaturday?: MrSaturdayPrediction;
  zeroPointers?: ZeroPointersPrediction;
  wildcard?: WildcardPrediction;
}

/** Category score for one user */
export interface CategoryScore {
  categoryId: PredictionCategoryId;
  score: number;
}

/** One member's overall standing in the group */
export interface MemberStanding {
  userId: string;
  displayName: string;
  overallScore: number;
  rank: number;
  categoryScores: CategoryScore[];
}

/** Full predictions + standings for a group (for the predictions page) */
export interface GroupPredictionsData {
  groupId: string;
  standings: MemberStanding[];
  predictions: UserPredictions[];
  /** User IDs who have locked their predictions (no edits allowed). */
  lockedUserIds?: string[];
  /** From backend: when true, predictions stay locked forever (system/hybrid). */
  predictionLock?: boolean;
  /** From backend: ISO date of first race (e.g. "2026-03-01"). Used when predictionLock is false. */
  firstRaceDate?: string;
  /** Hybrid mode: admin override. When set, overrides system lock. */
  adminLockOverride?: boolean;
  /** Admin mode: admin has locked predictions for the whole group. */
  adminSetPredictionsLocked?: boolean;
}
