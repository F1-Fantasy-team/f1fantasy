/**
 * Resolve a Wikipedia page URL to its main image URL via Wikipedia REST API.
 * Cached per session to avoid repeated requests.
 */

const CACHE = new Map<string, string | null>();

function titleFromWikipediaUrl(url: string): string | null {
  try {
    const u = new URL(url);
    if (!/^https?:\/\/(?:en\.)?wikipedia\.org\/wiki\//i.test(url)) return null;
    const segment = u.pathname.split("/wiki/")[1];
    return segment ? decodeURIComponent(segment.replace(/_/g, " ")) : null;
  } catch {
    return null;
  }
}

/**
 * Fetch the main image URL for a Wikipedia page. Returns null if not a Wikipedia URL or no image.
 */
export async function getWikipediaImageUrl(wikipediaUrl: string): Promise<string | null> {
  const cached = CACHE.get(wikipediaUrl);
  if (cached !== undefined) return cached;

  const title = titleFromWikipediaUrl(wikipediaUrl);
  if (!title) {
    CACHE.set(wikipediaUrl, null);
    return null;
  }

  try {
    const encoded = encodeURIComponent(title.replace(/ /g, "_"));
    const res = await fetch(
      `https://en.wikipedia.org/api/rest_v1/page/summary/${encoded}`,
      { headers: { "Accept": "application/json" } }
    );
    if (!res.ok) {
      CACHE.set(wikipediaUrl, null);
      return null;
    }
    const data = await res.json();
    const imageUrl = data.thumbnail?.source ?? null;
    CACHE.set(wikipediaUrl, imageUrl);
    return imageUrl;
  } catch {
    CACHE.set(wikipediaUrl, null);
    return null;
  }
}
