const BASE_URL = import.meta.env.VITE_API_BASE_URL as string | undefined;

export function getApiBaseUrl(): string | undefined {
  if (!BASE_URL || BASE_URL.trim() === "") return undefined;
  return BASE_URL.replace(/\/$/, "");
}

export async function apiGet<T>(path: string): Promise<T> {
  const base = getApiBaseUrl();
  if (!base) throw new Error("VITE_API_BASE_URL is not set");
  const url = path.startsWith("http") ? path : `${base}${path.startsWith("/") ? path : `/${path}`}`;
  const res = await fetch(url, { method: "GET", headers: { Accept: "application/json" } });
  if (!res.ok) throw new Error(`API ${res.status}: ${res.statusText}`);
  return res.json() as Promise<T>;
}
