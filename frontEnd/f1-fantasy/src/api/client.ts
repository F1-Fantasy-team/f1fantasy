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

const baseUrl = (path: string) => {
  const base = getApiBaseUrl();
  if (!base) throw new Error("Service base address is not configured.");
  return path.startsWith("http") ? path : `${base}${path.startsWith("/") ? path : `/${path}`}`;
};

export async function apiGet<T>(path: string): Promise<T> {
  const url = baseUrl(path);
  const headers = await getAuthHeaders();
  const res = await fetch(url, { method: "GET", headers });
  if (!res.ok) throw new Error(`Request failed (${res.status}: ${res.statusText})`);
  return res.json() as Promise<T>;
}

/** GET that returns null on 404 (e.g. no prediction yet). Use for optional resources. */
export async function apiGetOptional<T>(path: string): Promise<T | null> {
  const url = baseUrl(path);
  const headers = await getAuthHeaders();
  const res = await fetch(url, { method: "GET", headers });
  if (res.status === 404) return null;
  if (!res.ok) throw new Error(`Request failed (${res.status}: ${res.statusText})`);
  return res.json() as Promise<T>;
}

export async function apiPost<T>(path: string, body?: unknown): Promise<T> {
  const url = baseUrl(path);
  const headers = await getAuthHeaders();
  headers["Content-Type"] = "application/json";
  const res = await fetch(url, {
    method: "POST",
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });
  if (!res.ok) throw new Error(`Request failed (${res.status}: ${res.statusText})`);
  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

export async function apiPut<T>(path: string, body: unknown): Promise<T> {
  const url = baseUrl(path);
  const headers = await getAuthHeaders();
  headers["Content-Type"] = "application/json";
  const res = await fetch(url, { method: "PUT", headers, body: JSON.stringify(body) });
  if (!res.ok) throw new Error(`Request failed (${res.status}: ${res.statusText})`);
  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

export async function apiDelete(path: string): Promise<void> {
  const url = baseUrl(path);
  const headers = await getAuthHeaders();
  const res = await fetch(url, { method: "DELETE", headers });
  if (!res.ok) throw new Error(`Request failed (${res.status}: ${res.statusText})`);
}
