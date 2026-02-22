import { describe, it, expect, vi, beforeEach } from "vitest";
import {
  fetchRacesForSeasonFromApi,
  getFirstRaceDateFromRaces,
} from "./races";
import { getApiBaseUrl, apiGet } from "./client";
import { getCurrentSeason } from "../constants/season";
import type { RaceApi } from "./types";

vi.mock("./client", () => ({
  getApiBaseUrl: vi.fn(),
  apiGet: vi.fn(),
}));

describe("getFirstRaceDateFromRaces", () => {
  it("returns null for empty array", () => {
    expect(getFirstRaceDateFromRaces([])).toBeNull();
  });

  it("returns date of first race by round order", () => {
    const races: RaceApi[] = [
      {
        season: "2026",
        round: "2",
        url: "",
        raceName: "Race 2",
        circuit: { circuitId: "c2", url: "", circuitName: "C2", location: { lat: "0", long: "0", locality: "", country: "" } },
        date: "2026-04-01",
        time: "14:00:00Z",
      },
      {
        season: "2026",
        round: "1",
        url: "",
        raceName: "Race 1",
        circuit: { circuitId: "c1", url: "", circuitName: "C1", location: { lat: "0", long: "0", locality: "", country: "" } },
        date: "2026-03-01",
        time: "14:00:00Z",
      },
    ];
    expect(getFirstRaceDateFromRaces(races)).toBe("2026-03-01");
  });

  it("trims date and returns null when date is empty", () => {
    const races: RaceApi[] = [
      {
        season: "2026",
        round: "1",
        url: "",
        raceName: "Race 1",
        circuit: { circuitId: "c1", url: "", circuitName: "C1", location: { lat: "0", long: "0", locality: "", country: "" } },
        date: "  2026-03-01  ",
        time: "14:00:00Z",
      },
    ];
    expect(getFirstRaceDateFromRaces(races)).toBe("2026-03-01");
  });

  it("returns null when first race has no date", () => {
    const races: RaceApi[] = [
      {
        season: "2026",
        round: "1",
        url: "",
        raceName: "Race 1",
        circuit: { circuitId: "c1", url: "", circuitName: "C1", location: { lat: "0", long: "0", locality: "", country: "" } },
        date: "",
        time: "14:00:00Z",
      },
    ];
    expect(getFirstRaceDateFromRaces(races)).toBeNull();
  });
});

describe("fetchRacesForSeasonFromApi", () => {
  beforeEach(() => {
    vi.mocked(getApiBaseUrl).mockReturnValue(undefined);
    vi.mocked(apiGet).mockReset();
  });

  it("returns null when getApiBaseUrl is not set", async () => {
    vi.mocked(getApiBaseUrl).mockReturnValue(undefined);
    const result = await fetchRacesForSeasonFromApi();
    expect(result).toBeNull();
  });

  it("returns races when API returns non-empty array", async () => {
    vi.mocked(getApiBaseUrl).mockReturnValue("https://api.example.com");
    const races: RaceApi[] = [
      {
        season: "2026",
        round: "1",
        url: "",
        raceName: "Bahrain",
        circuit: { circuitId: "bahrain", url: "", circuitName: "Bahrain", location: { lat: "0", long: "0", locality: "", country: "" } },
        date: "2026-03-01",
        time: "14:00:00Z",
      },
    ];
    vi.mocked(apiGet).mockResolvedValue(races);
    const result = await fetchRacesForSeasonFromApi();
    expect(result).toEqual(races);
    expect(apiGet).toHaveBeenCalledWith(`/api/race/${getCurrentSeason()}`);
  });

  it("returns null when API throws", async () => {
    vi.mocked(getApiBaseUrl).mockReturnValue("https://api.example.com");
    vi.mocked(apiGet).mockRejectedValue(new Error("Network error"));
    const result = await fetchRacesForSeasonFromApi();
    expect(result).toBeNull();
  });

  it("returns null when API returns empty array", async () => {
    vi.mocked(getApiBaseUrl).mockReturnValue("https://api.example.com");
    vi.mocked(apiGet).mockResolvedValue([]);
    const result = await fetchRacesForSeasonFromApi();
    expect(result).toBeNull();
  });
});
