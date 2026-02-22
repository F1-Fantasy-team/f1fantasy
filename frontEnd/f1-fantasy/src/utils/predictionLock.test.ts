import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import {
  getSystemPredictionsLocked,
  getEffectiveGroupLocked,
  isUserLocked,
  canUserUnlockSelf,
  isGroupAdmin,
} from "./predictionLock";
import type { Group } from "../types/group";
import type { GroupPredictionsData } from "../types/predictions";

function minimalData(overrides: Partial<GroupPredictionsData> = {}): GroupPredictionsData {
  return {
    groupId: "g1",
    standings: [],
    predictions: [],
    ...overrides,
  };
}

function minimalGroup(overrides: Partial<Group> = {}): Group {
  return {
    id: "g1",
    name: "Test",
    memberCount: 1,
    createdAt: "2025-01-01T00:00:00Z",
    adminUserId: "admin-1",
    ...overrides,
  };
}

describe("getSystemPredictionsLocked", () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it("returns true when data.predictionLock is true", () => {
    expect(getSystemPredictionsLocked(minimalData({ predictionLock: true }))).toBe(true);
    expect(getSystemPredictionsLocked(minimalData({ predictionLock: true }), "2026-03-01")).toBe(true);
  });

  it("uses firstRaceDateFromRaces when data.firstRaceDate is not set", () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-02-15T12:00:00Z")); // before first race
    expect(getSystemPredictionsLocked(minimalData({}), "2026-03-01")).toBe(false);
    vi.setSystemTime(new Date("2026-03-01T00:00:00Z")); // on first race day
    expect(getSystemPredictionsLocked(minimalData({}), "2026-03-01")).toBe(true);
    vi.useRealTimers();
  });

  it("prefers data.firstRaceDate over firstRaceDateFromRaces", () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-02-20T12:00:00Z"));
    const data = minimalData({ firstRaceDate: "2026-04-01" }); // later than today
    expect(getSystemPredictionsLocked(data, "2026-03-01")).toBe(false);
    vi.useRealTimers();
  });
});

describe("getEffectiveGroupLocked", () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it("admin mode: uses adminSetPredictionsLocked only", () => {
    const group = minimalGroup({ predictionLockMode: "admin" });
    expect(getEffectiveGroupLocked(group, minimalData({ adminSetPredictionsLocked: true }))).toBe(true);
    expect(getEffectiveGroupLocked(group, minimalData({ adminSetPredictionsLocked: false }))).toBe(false);
  });

  it("system mode: uses system lock with firstRaceDateFromRaces", () => {
    vi.useFakeTimers();
    const group = minimalGroup({ predictionLockMode: "system" });
    vi.setSystemTime(new Date("2026-02-15T12:00:00Z"));
    expect(getEffectiveGroupLocked(group, minimalData({}), "2026-03-01")).toBe(false);
    vi.setSystemTime(new Date("2026-03-02T12:00:00Z"));
    expect(getEffectiveGroupLocked(group, minimalData({}), "2026-03-01")).toBe(true);
    vi.useRealTimers();
  });

  it("hybrid mode: adminLockOverride overrides system lock", () => {
    vi.useFakeTimers();
    const group = minimalGroup({ predictionLockMode: "hybrid" });
    vi.setSystemTime(new Date("2026-03-02T12:00:00Z")); // system would say locked
    expect(getEffectiveGroupLocked(group, minimalData({ adminLockOverride: false }), "2026-03-01")).toBe(false);
    expect(getEffectiveGroupLocked(group, minimalData({ adminLockOverride: true }), "2026-03-01")).toBe(true);
    vi.useRealTimers();
  });
});

describe("isUserLocked", () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-02-15T12:00:00Z"));
  });
  afterEach(() => {
    vi.useRealTimers();
  });

  it("returns true when group is effectively locked", () => {
    const group = minimalGroup({ predictionLockMode: "system" });
    vi.setSystemTime(new Date("2026-03-02T12:00:00Z"));
    expect(isUserLocked(group, minimalData({}), "user-1", "2026-03-01")).toBe(true);
  });

  it("returns true when user is in lockedUserIds", () => {
    const group = minimalGroup({ predictionLockMode: "admin" });
    const data = minimalData({ adminSetPredictionsLocked: false, lockedUserIds: ["user-1"] });
    expect(isUserLocked(group, data, "user-1")).toBe(true);
    expect(isUserLocked(group, data, "user-2")).toBe(false);
  });

  it("accepts optional firstRaceDateFromRaces", () => {
    const group = minimalGroup({ predictionLockMode: "system" });
    expect(isUserLocked(group, minimalData({}), "user-1", "2026-03-01")).toBe(false);
    vi.setSystemTime(new Date("2026-03-02T12:00:00Z"));
    expect(isUserLocked(group, minimalData({}), "user-1", "2026-03-01")).toBe(true);
  });
});

describe("canUserUnlockSelf", () => {
  it("admin mode: returns false (only admin toggles)", () => {
    const group = minimalGroup({ predictionLockMode: "admin" });
    const data = minimalData({ lockedUserIds: ["user-1"] });
    expect(canUserUnlockSelf(group, data, "user-1")).toBe(false);
  });

  it("returns true when user is in lockedUserIds and group is not locked", () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-02-15T12:00:00Z"));
    const group = minimalGroup({ predictionLockMode: "hybrid" });
    const data = minimalData({ lockedUserIds: ["user-1"] });
    expect(canUserUnlockSelf(group, data, "user-1", "2026-03-01")).toBe(true);
    vi.useRealTimers();
  });

  it("returns false when group is effectively locked", () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-03-02T12:00:00Z"));
    const group = minimalGroup({ predictionLockMode: "system" });
    const data = minimalData({ lockedUserIds: ["user-1"] });
    expect(canUserUnlockSelf(group, data, "user-1", "2026-03-01")).toBe(false);
    vi.useRealTimers();
  });
});

describe("isGroupAdmin", () => {
  it("returns true when userId is adminUserId", () => {
    expect(isGroupAdmin(minimalGroup({ adminUserId: "admin-1" }), "admin-1")).toBe(true);
  });

  it("returns false when userId is not admin", () => {
    expect(isGroupAdmin(minimalGroup({ adminUserId: "admin-1" }), "user-2")).toBe(false);
  });

  it("returns false when adminUserId is undefined", () => {
    expect(isGroupAdmin(minimalGroup({ adminUserId: undefined }), "user-1")).toBe(false);
  });
});
