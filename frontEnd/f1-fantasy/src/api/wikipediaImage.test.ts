import { describe, it, expect, vi, beforeEach } from "vitest";
import { getWikipediaImageUrl } from "./wikipediaImage";

describe("getWikipediaImageUrl", () => {
  beforeEach(() => {
    vi.stubGlobal(
      "fetch",
      vi.fn((url: string) => {
        if (url.includes("Max_Verstappen")) {
          return Promise.resolve({
            ok: true,
            json: () =>
              Promise.resolve({
                thumbnail: { source: "https://upload.wikimedia.org/example.jpg" },
              }),
          } as Response);
        }
        return Promise.resolve({ ok: false } as Response);
      })
    );
  });

  it("returns null for non-Wikipedia URL", async () => {
    const result = await getWikipediaImageUrl("https://example.com/foo");
    expect(result).toBeNull();
  });

  it("returns thumbnail URL for valid Wikipedia page URL", async () => {
    const result = await getWikipediaImageUrl(
      "https://en.wikipedia.org/wiki/Max_Verstappen"
    );
    expect(result).toBe("https://upload.wikimedia.org/example.jpg");
  });

  it("returns null when API response has no thumbnail", async () => {
    vi.mocked(fetch).mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve({}),
    } as Response);
    const result = await getWikipediaImageUrl(
      "https://en.wikipedia.org/wiki/Some_Page"
    );
    expect(result).toBeNull();
  });
});
