const BASE_URL = import.meta.env.VITE_API_BASE_URL as string | undefined;

/** Optional: set from app so API requests include Clerk session token. */
let authTokenGetter: (() => Promise<string | null>) | null = null;

export function setAuthTokenGetter(getter: (() => Promise<string | null>) | null): void {
  authTokenGetter = getter;
}

export function getApiBaseUrl(): string | undefined {
  if (!BASE_URL || BASE_URL.trim() === "") return undefined;
  return BASE_URL.replace(/\/$/, "");
}

async function getAuthHeaders(): Promise<Record<string, string>> {
  const headers: Record<string, string> = { Accept: "application/json" };
  if (authTokenGetter) {
    try {
      const token = await authTokenGetter();
      if (token) headers.Authorization = `Bearer ${token}`;
    } catch (_err) {
      // Session may be expired or getToken failed; proceed without token so caller can handle 401
    }
  }
  return headers;
}

export async function apiGet<T>(path: string): Promise<T> {
  const base = getApiBaseUrl();
  if (!base) throw new Error("VITE_API_BASE_URL is not set");
  const url = path.startsWith("http") ? path : `${base}${path.startsWith("/") ? path : `/${path}`}`;
  const headers = await getAuthHeaders();
  const res = await fetch(url, { method: "GET", headers });
  if (!res.ok) throw new Error(`API ${res.status}: ${res.statusText}`);
  return res.json() as Promise<T>;
}
