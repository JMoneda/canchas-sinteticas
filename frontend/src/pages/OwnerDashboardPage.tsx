import { Link } from 'react-router-dom';
import { api } from '../api/client';
import { useAsync } from '../lib/useAsync';
import { formatCurrency } from '../lib/format';
import { buttonClasses, Card, ErrorBanner, Spinner } from '../components/ui';

export function OwnerDashboardPage() {
  const report = useAsync(() => api.owner.reports.get(), []);
  const venues = useAsync(() => api.owner.venues.list(), []);

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-bold text-slate-800">Panel de dueño</h1>
        <div className="flex gap-2">
          <Link to="/panel/sedes" className={buttonClasses('secondary')}>
            Gestionar sedes
          </Link>
          <Link to="/panel/agenda" className={buttonClasses('primary')}>
            Ver agenda
          </Link>
        </div>
      </div>

      {report.loading && <Spinner label="Cargando reportes..." />}
      {report.error && <ErrorBanner message={report.error} />}

      {report.data && (
        <>
          <div className="grid gap-4 sm:grid-cols-3">
            <Kpi label="Reservas (rango)" value={String(report.data.total_reservations)} icon="📅" />
            <Kpi label="Ingresos (rango)" value={formatCurrency(report.data.total_revenue)} icon="💰" />
            <Kpi label="Ocupación" value={`${Math.round(report.data.occupancy_rate * 100)}%`} icon="📊" />
          </div>

          <div className="grid gap-4 lg:grid-cols-2">
            <Card className="p-5">
              <h2 className="mb-3 font-semibold text-slate-800">Rendimiento por cancha</h2>
              {report.data.by_court.length === 0 ? (
                <p className="text-sm text-slate-500">Sin datos en el rango.</p>
              ) : (
                <table className="w-full text-sm">
                  <thead>
                    <tr className="text-left text-slate-400">
                      <th className="pb-2 font-medium">Cancha</th>
                      <th className="pb-2 text-right font-medium">Reservas</th>
                      <th className="pb-2 text-right font-medium">Ingresos</th>
                    </tr>
                  </thead>
                  <tbody>
                    {report.data.by_court.map((c) => (
                      <tr key={c.court_id} className="border-t border-slate-100">
                        <td className="py-2">
                          <span className="font-medium text-slate-700">{c.court_name}</span>
                          <span className="block text-xs text-slate-400">{c.venue_name}</span>
                        </td>
                        <td className="py-2 text-right text-slate-600">{c.reservations}</td>
                        <td className="py-2 text-right font-medium text-slate-800">
                          {formatCurrency(c.revenue)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </Card>

            <Card className="p-5">
              <h2 className="mb-3 font-semibold text-slate-800">Horarios más solicitados</h2>
              {report.data.top_hours.length === 0 ? (
                <p className="text-sm text-slate-500">Sin datos en el rango.</p>
              ) : (
                <ul className="space-y-2">
                  {report.data.top_hours.map((h) => (
                    <li key={h.hour} className="flex items-center gap-3">
                      <span className="w-14 text-sm font-medium text-slate-700">{h.hour}</span>
                      <div className="h-2 flex-1 rounded-full bg-slate-100">
                        <div
                          className="h-2 rounded-full bg-brand-500"
                          style={{
                            width: `${(h.count / report.data!.top_hours[0].count) * 100}%`,
                          }}
                        />
                      </div>
                      <span className="w-8 text-right text-sm text-slate-500">{h.count}</span>
                    </li>
                  ))}
                </ul>
              )}
            </Card>
          </div>
        </>
      )}

      <Card className="p-5">
        <h2 className="mb-3 font-semibold text-slate-800">Mis sedes</h2>
        {venues.loading && <Spinner />}
        {venues.error && <ErrorBanner message={venues.error} />}
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {venues.data?.map((v) => (
            <Link
              key={v.id}
              to={`/panel/sedes/${v.id}`}
              className="rounded-xl border border-slate-200 p-4 transition hover:border-brand-300 hover:bg-brand-50"
            >
              <p className="font-medium text-slate-800">{v.name}</p>
              <p className="text-sm text-slate-500">{v.city}</p>
              <p className="mt-2 text-xs text-slate-400">{v.courts.length} cancha(s)</p>
            </Link>
          ))}
        </div>
      </Card>
    </div>
  );
}

function Kpi({ label, value, icon }: { label: string; value: string; icon: string }) {
  return (
    <Card className="flex items-center gap-4 p-5">
      <span className="grid h-12 w-12 place-items-center rounded-xl bg-brand-50 text-2xl">{icon}</span>
      <div>
        <p className="text-sm text-slate-500">{label}</p>
        <p className="text-xl font-bold text-slate-800">{value}</p>
      </div>
    </Card>
  );
}
