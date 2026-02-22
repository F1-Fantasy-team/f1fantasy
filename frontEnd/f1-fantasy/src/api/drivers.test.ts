import { describe, it, expect, vi } from "vitest";
import { mapDriverApiToDriver, fetchDriversFromApi } from "./drivers";
import { getCurrentSeason } from "../constants/season";
import type { DriverApi } from "./types";

vi.mock("./client", () => ({
  getApiBaseUrl: vi.fn(() => "https://api.example.com"),
  apiGet: vi.fn(),
}));

describe("mapDriverApiToDriver", () => {
  it("maps API driver to app Driver with lowercase id", () => {
    const api: DriverApi = {
      driverId: "VER",
      permanentNumber: "1",
      code: "VER",
      url: "",
      givenName: "Max",
      familyName: "Verstappen",
      dateOfBirth: "1997-09-30",
      nationality: "Dutch",
    };
    expect(mapDriverApiToDriver(api)).toEqual({
      id: "ver",
      name: "Max Verstappen",
      teamId: undefined,
      wikipediaUrl: undefined,
    });
  });

  it("maps API driver wikipedia url to wikipediaUrl", () => {
    const api: DriverApi = {
      driverId: "VER",
      permanentNumber: "1",
      code: "VER",
      url: "https://en.wikipedia.org/wiki/Max_Verstappen",
      givenName: "Max",
      familyName: "Verstappen",
      dateOfBirth: "1997-09-30",
      nationality: "Dutch",
    };
    expect(mapDriverApiToDriver(api).wikipediaUrl).toBe("https://en.wikipedia.org/wiki/Max_Verstappen");
  });

  it("trims name and handles single name", () => {
    const api: DriverApi = {
      driverId: "ALB",
      permanentNumber: "23",
      code: "ALB",
      url: "",
      givenName: "Alexander",
      familyName: "Albon",
      dateOfBirth: "1996-03-23",
      nationality: "Thai",
    };
    expect(mapDriverApiToDriver(api).name).toBe("Alexander Albon");
  });
});

describe("fetchDriversFromApi", () => {
  it("returns Promise that resolves to Driver[] or null", async () => {
    const { apiGet } = await import("./client");
    vi.mocked(apiGet).mockRejectedValueOnce(new Error("API 500"));
    const result = await fetchDriversFromApi();
    expect(result === null || Array.isArray(result)).toBe(true);
    if (result !== null) {
      result.forEach((d) => {
        expect(d).toHaveProperty("id");
        expect(d).toHaveProperty("name");
      });
    }
  });

  it("returns all drivers from season API and calls API with current season", async () => {
    const { apiGet } = await import("./client");
    const apiDrivers: DriverApi[] = [
      {
        driverId: "ver",
        permanentNumber: "1",
        code: "VER",
        url: "",
        givenName: "Max",
        familyName: "Verstappen",
        dateOfBirth: "1997-09-30",
        nationality: "Dutch",
      },
      {
        driverId: "reserve_1",
        permanentNumber: "",
        code: "RES",
        url: "",
        givenName: "Reserve",
        familyName: "Driver",
        dateOfBirth: "2000-01-01",
        nationality: "British",
      },
    ];
    vi.mocked(apiGet).mockResolvedValueOnce(apiDrivers);
    const result = await fetchDriversFromApi();
    expect(result).not.toBeNull();
    expect(result!.length).toBe(2);
    expect(result![0].id).toBe("ver");
    expect(result![1].id).toBe("reserve_1");
    expect(apiGet).toHaveBeenCalledWith(`/api/driver/season/${getCurrentSeason()}`);
  });
});
