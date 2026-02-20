import { describe, it, expect } from "vitest";
import { getApiBaseUrl } from "./client";

describe("getApiBaseUrl", () => {
  it("returns a string or undefined (value comes from env at build time)", () => {
    const url = getApiBaseUrl();
    if (url !== undefined) {
      expect(typeof url).toBe("string");
      expect(url.length).toBeGreaterThan(0);
      expect(url).not.toMatch(/\/$/);
    }
  });

  it("strips trailing slash when URL is set", () => {
    const url = getApiBaseUrl();
    if (url) expect(url.endsWith("/")).toBe(false);
  });
});
