import { describe, it, expect, vi, beforeEach } from "vitest";
import {
  mapConstructorApiToConstructor,
  fetchConstructorsFromApi,
} from "./constructors";
import { getApiBaseUrl, apiGet } from "./client";
import { getCurrentSeason } from "../constants/season";
import type { ConstructorApi } from "./types";

vi.mock("./client", () => ({
  getApiBaseUrl: vi.fn(),
  apiGet: vi.fn(),
}));

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

describe("fetchConstructorsFromApi", () => {
  beforeEach(() => {
    vi.mocked(getApiBaseUrl).mockReturnValue("https://api.example.com");
    vi.mocked(apiGet).mockReset();
  });

  it("returns null when getApiBaseUrl is not set", async () => {
    vi.mocked(getApiBaseUrl).mockReturnValue(undefined);
    const result = await fetchConstructorsFromApi();
    expect(result).toBeNull();
  });

  it("returns constructors when API returns non-empty array", async () => {
    const apiConstructors: ConstructorApi[] = [
      {
        constructorId: "RED_BULL_RACING",
        url: "",
        name: "Red Bull Racing",
        nationality: "Austrian",
      },
    ];
    vi.mocked(apiGet).mockResolvedValue(apiConstructors);
    const result = await fetchConstructorsFromApi();
    expect(result).toEqual([
      { id: "red_bull_racing", name: "Red Bull Racing" },
    ]);
    expect(apiGet).toHaveBeenCalledWith(
      `/api/constructor/season/${getCurrentSeason()}`
    );
  });

  it("returns null when API throws", async () => {
    vi.mocked(apiGet).mockRejectedValue(new Error("API 500"));
    const result = await fetchConstructorsFromApi();
    expect(result).toBeNull();
  });

  it("returns null when API returns empty array", async () => {
    vi.mocked(apiGet).mockResolvedValue([]);
    const result = await fetchConstructorsFromApi();
    expect(result).toBeNull();
  });
});
