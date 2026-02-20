import { describe, it, expect, vi } from "vitest";
import {
  mapConstructorApiToConstructor,
  getMockConstructors,
  fetchConstructorsFromApi,
} from "./constructors";
import type { ConstructorApi } from "./types";

describe("mapConstructorApiToConstructor", () => {
  it("maps API constructor to app Constructor with lowercase id", () => {
    const api: ConstructorApi = {
      constructorId: "RED_BULL_RACING",
      url: "",
      name: "Red Bull Racing",
      nationality: "Austrian",
    };
    expect(mapConstructorApiToConstructor(api)).toEqual({
      id: "red_bull_racing",
      name: "Red Bull Racing",
    });
  });
});

describe("getMockConstructors", () => {
  it("returns an array (empty when no mock data in repo)", () => {
    const constructors = getMockConstructors();
    expect(Array.isArray(constructors)).toBe(true);
  });

  it("returns constructors with id and name when present", () => {
    const constructors = getMockConstructors();
    constructors.forEach((c) => {
      expect(c).toHaveProperty("id");
      expect(c).toHaveProperty("name");
      expect(typeof c.id).toBe("string");
      expect(typeof c.name).toBe("string");
    });
  });
});

describe("fetchConstructorsFromApi", () => {
  it("returns Promise that resolves to Constructor[] or null", async () => {
    const fetchSpy = vi.spyOn(globalThis, "fetch").mockResolvedValueOnce({
      ok: false,
      status: 500,
    } as Response);
    const result = await fetchConstructorsFromApi();
    fetchSpy.mockRestore();
    expect(result === null || Array.isArray(result)).toBe(true);
    if (result !== null) {
      result.forEach((c) => {
        expect(c).toHaveProperty("id");
        expect(c).toHaveProperty("name");
      });
    }
  });
});
