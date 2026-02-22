import { describe, it, expect } from "vitest";
import { createInitialGroupPredictionsData } from "./groupPredictionsData";

describe("createInitialGroupPredictionsData", () => {
  it("creates minimal data with current user in standings and predictions", () => {
    const data = createInitialGroupPredictionsData("grp-1", "user-x", "Test User");
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

  it("creates new data for any group", () => {
    const data = createInitialGroupPredictionsData(
      "grp-new",
      "user-123",
      "New Member"
    );
    expect(data.groupId).toBe("grp-new");
    expect(data.standings).toHaveLength(1);
    expect(data.standings[0].userId).toBe("user-123");
    expect(data.standings[0].displayName).toBe("New Member");
    expect(data.standings[0].categoryScores).toEqual([]);
    expect(data.predictions).toHaveLength(1);
    expect(data.predictions[0].userId).toBe("user-123");
    expect(data.predictions[0].displayName).toBe("New Member");
  });
});
