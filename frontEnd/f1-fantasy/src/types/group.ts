/**
 * How prediction lock is determined for this group.
 * - admin: Only the group admin can lock/unlock; they control it entirely.
 * - system: Backend + first-race date decide; no manual override.
 * - hybrid: System decides, but admin can override (default).
 */
export type PredictionLockMode = "admin" | "system" | "hybrid";

export interface Group {
  id: string;
  name: string;
  memberCount: number;
  createdAt: string;
  inviteCode?: string;
  /** User ID of the group creator; they are the admin and can manage lock (admin/hybrid mode). */
  adminUserId?: string;
  /** How prediction lock is determined. Default: "hybrid". */
  predictionLockMode?: PredictionLockMode;
}
