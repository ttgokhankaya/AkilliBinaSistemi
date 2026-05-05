const API_BASE = import.meta.env.VITE_API_BASE ?? 'http://localhost:5176';

export interface Area {
  id: number;
  name: string | null;
  width: number;
  height: number;
  parentAreaId: number | null;
  areaTypeId: number | null;
}

export async function fetchAreas(): Promise<Area[]> {
  const res = await fetch(`${API_BASE}/api/areas`);
  if (!res.ok) throw new Error(`API error ${res.status}`);
  return res.json();
}
