import { useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import { useAsync } from '../lib/useAsync';
import { formatCurrency } from '../lib/format';
import { Card, EmptyState, ErrorBanner, Spinner, inputClasses } from '../components/ui';

export function MarketplacePage() {
  const [cityInput, setCityInput] = useState('');
  const [query, setQuery] = useState('');
  const { data: venues, loading, error } = useAsync(() => api.venues.search(query || undefined), [query]);

  return (
    <div className="space-y-8">
      <section className="overflow-hidden rounded-3xl bg-gradient-to-br from-brand-700 to-brand-500 px-6 py-10 text-white sm:px-10">
        <h1 className="max-w-2xl text-3xl font-bold sm:text-4xl">
          Encuentra y reserva tu cancha sintética
        </h1>
        <p className="mt-2 max-w-xl text-brand-50">
          Compara sedes, mira la disponibilidad en tiempo real y reserva en segundos.
        </p>
        <form
          className="mt-6 flex max-w-md gap-2"
          onSubmit={(e) => {
            e.preventDefault();
            setQuery(cityInput.trim());
          }}
        >
          <input
            className="w-full rounded-lg border-0 px-4 py-2.5 text-sm text-slate-800 shadow-sm focus:outline-none focus:ring-2 focus:ring-white/60"
            placeholder="Busca por ciudad (ej. Bogotá, Medellín)"
            value={cityInput}
            onChange={(e) => setCityInput(e.target.value)}
          />
          <button className="rounded-lg bg-white px-5 py-2.5 text-sm font-semibold text-brand-700 hover:bg-brand-50">
            Buscar
          </button>
        </form>
      </section>

      <section>
        <div className="mb-4 flex items-baseline justify-between">
          <h2 className="text-lg font-semibold text-slate-800">
            {query ? `Sedes en "${query}"` : 'Todas las sedes'}
          </h2>
          {venues && <span className="text-sm text-slate-500">{venues.length} resultado(s)</span>}
        </div>

        {loading && <Spinner label="Cargando sedes..." />}
        {error && <ErrorBanner message={error} />}
        {venues && venues.length === 0 && (
          <EmptyState title="No encontramos sedes" subtitle="Prueba con otra ciudad o limpia el filtro." />
        )}

        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {venues?.map((venue) => (
            <Link key={venue.id} to={`/sedes/${venue.id}`}>
              <Card className="flex h-full flex-col overflow-hidden transition hover:-translate-y-0.5 hover:shadow-md">
                <div className="flex h-32 items-center justify-center bg-gradient-to-br from-brand-100 to-brand-200 text-5xl">
                  🥅
                </div>
                <div className="flex flex-1 flex-col p-4">
                  <h3 className="font-semibold text-slate-800">{venue.name}</h3>
                  <p className="text-sm text-slate-500">
                    {venue.city} · {venue.address}
                  </p>
                  <div className="mt-3 flex flex-wrap gap-1.5">
                    {venue.services.slice(0, 3).map((service) => (
                      <span key={service} className="rounded-full bg-slate-100 px-2 py-0.5 text-xs text-slate-600">
                        {service}
                      </span>
                    ))}
                  </div>
                  <div className="mt-4 flex items-end justify-between border-t border-slate-100 pt-3">
                    <span className="text-xs text-slate-500">
                      {venue.court_count} cancha(s)
                    </span>
                    {venue.min_price != null && (
                      <span className="text-sm font-semibold text-brand-700">
                        desde {formatCurrency(venue.min_price)}/h
                      </span>
                    )}
                  </div>
                </div>
              </Card>
            </Link>
          ))}
        </div>
      </section>
    </div>
  );
}
