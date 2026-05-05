import { useEffect, useState } from 'react';
import { fetchAreas, type Area } from './api';

export default function App() {
  const [areas, setAreas] = useState<Area[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchAreas()
      .then(setAreas)
      .catch((e) => setError(String(e)))
      .finally(() => setLoading(false));
  }, []);

  return (
    <div style={{ padding: 24, fontFamily: 'system-ui, sans-serif' }}>
      <h1>ADLE — Areas</h1>
      {loading && <p>Loading…</p>}
      {error && <p style={{ color: 'crimson' }}>Error: {error}</p>}
      {!loading && !error && (
        <table style={{ borderCollapse: 'collapse', width: '100%' }}>
          <thead>
            <tr style={{ background: '#f3f4f6', textAlign: 'left' }}>
              <th style={cell}>ID</th>
              <th style={cell}>Name</th>
              <th style={cell}>Width</th>
              <th style={cell}>Height</th>
              <th style={cell}>Parent</th>
              <th style={cell}>Type</th>
            </tr>
          </thead>
          <tbody>
            {areas.length === 0 ? (
              <tr>
                <td style={cell} colSpan={6}>No areas found.</td>
              </tr>
            ) : (
              areas.map((a) => (
                <tr key={a.id}>
                  <td style={cell}>{a.id}</td>
                  <td style={cell}>{a.name ?? '—'}</td>
                  <td style={cell}>{a.width}</td>
                  <td style={cell}>{a.height}</td>
                  <td style={cell}>{a.parentAreaId ?? '—'}</td>
                  <td style={cell}>{a.areaTypeId ?? '—'}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      )}
    </div>
  );
}

const cell: React.CSSProperties = {
  border: '1px solid #e5e7eb',
  padding: '8px 12px',
};
