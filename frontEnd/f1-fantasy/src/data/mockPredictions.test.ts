import { describe, it, expect } from "vitest";
import {
  getGroupPredictionsData,
  getOrCreateGroupPredictionsData,
  MOCK_GROUP_PREDICTIONS,
} from "./mockPredictions";

describe("getGroupPredictionsData", () => {
  it("returns undefined when no mock data for group (repo has no mock data)", () => {
    const data = getGroupPredictionsData("grp-1");
    expect(data).toBeUndefined();
  });

  it("returns undefined for unknown group id", () => {
    expect(getGroupPredictionsData("grp-unknown")).toBeUndefined();
  });

  it("MOCK_GROUP_PREDICTIONS is empty by default", () => {
    expect(Object.keys(MOCK_GROUP_PREDICTIONS)).toHaveLength(0);
  });
});

describe("getOrCreateGroupPredictionsData", () => {
  it("creates minimal data for any group when no mock data", () => {
    const data = getOrCreateGroupPredictionsData("grp-1", "user-x", "Test User");
    expect(data.groupId).toBe("grp-1");
    expect(data.standings).toHaveLength(1);
    expect(data.standings[0].userId).toBe("user-x");
    expect(data.standings[0].displayName).toBe("Test User");
    expect(data.standings[0].overallScore).toBe(0);
    expect(data.standings[0].rank).toBe(1);
    expect(data.predictions).toHaveLength(1);
    expect(data.predictions[0].userId).toBe("user-x");
    expect(data.predictions[0].displayName).toBe("Test User");
  });

  it("creates new data for group without mock data", () => {
    const data = getOrCreateGroupPredictionsData(
      "grp-new",
      "user-123",
      "New Member"
    );
    expect(data.groupId).toBe("grp-new");
    expect(data.standings).toHaveLength(1);
    expect(data.standings[0].userId).toBe("user-123");
    expect(data.standings[0].displayName).toBe("New Member");
    expect(data.standings[0].overallScore).toBe(0);
    expect(data.standings[0].rank).toBe(1);
    expect(data.standings[0].categoryScores).toEqual([]);
    expect(data.predictions).toHaveLength(1);
    expect(data.predictions[0].userId).toBe("user-123");
    expect(data.predictions[0].displayName).toBe("New Member");
  });
});
