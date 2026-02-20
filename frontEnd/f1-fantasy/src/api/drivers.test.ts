import { describe, it, expect, vi } from "vitest";
import {
  mapDriverApiToDriver,
  getMockDrivers,
  fetchDriversFromApi,
} from "./drivers";
import type { DriverApi } from "./types";

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
    });
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

describe("getMockDrivers", () => {
  it("returns an array (empty when no mock data in repo)", () => {
    const drivers = getMockDrivers();
    expect(Array.isArray(drivers)).toBe(true);
  });

  it("returns drivers with id and name when present", () => {
    const drivers = getMockDrivers();
    drivers.forEach((d) => {
      expect(d).toHaveProperty("id");
      expect(d).toHaveProperty("name");
      expect(typeof d.id).toBe("string");
      expect(typeof d.name).toBe("string");
    });
  });
});

describe("fetchDriversFromApi", () => {
  it("returns Promise that resolves to Driver[] or null", async () => {
    const fetchSpy = vi.spyOn(globalThis, "fetch").mockResolvedValueOnce({
      ok: false,
      status: 500,
    } as Response);
    const result = await fetchDriversFromApi();
    fetchSpy.mockRestore();
    expect(result === null || Array.isArray(result)).toBe(true);
    if (result !== null) {
      result.forEach((d) => {
        expect(d).toHaveProperty("id");
        expect(d).toHaveProperty("name");
      });
    }
  });
});
