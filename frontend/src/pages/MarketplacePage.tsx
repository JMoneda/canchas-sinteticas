import { useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import { useAsync } from '../lib/useAsync';
import { formatCurrency } from '../lib/format';
import { Card, EmptyState, ErrorBanner, SectionHeading, Skeleton } from '../components/ui';

export function MarketplacePage() {
  const [cityInput, setCityInput] = useState('');
  const [query, setQuery] = useState('');
  const { data: venues, loading, error } = useAsync(() => api.venues.search(query || undefined), [query]);

  return (
    <div className="space-y-10">
      {/* Hero deportivo premium */}
      <section className="relative overflow-hidden rounded-[calc(var(--radius-card)+0.5rem)] bg-gradient-to-br from-brand-800 via-brand-700 to-brand-600 px-6 py-12 text-white shadow-[var(--shadow-lifted)] sm:px-10 sm:py-16">
        <div className="pointer-events-none absolute -right-16 -top-16 h-64 w-64 rounded-full bg-accent-400/20 blur-3xl" />
        <div className="pointer-events-none absolute -bottom-24 -left-10 h-72 w-72 rounded-full bg-brand-400/20 blur-3xl" />
        <div className="relative">
          <span className="inline-flex items-center gap-2 rounded-full bg-white/10 px-3 py-1 text-xs font-semibold uppercase tracking-wider text-brand-50 ring-1 ring-white/20">
            ⚽ Reserva en segundos
          </span>
          <h1 className="mt-4 max-w-2xl text-4xl font-extrabold leading-tight sm:text-5xl">
            Tu próxima cancha, lista para jugar
          </h1>
          <p className="mt-3 max-w-xl text-lg text-brand-50/90">
            Compara sedes, mira disponibilidad en tiempo real y asegura tu horario al instante.
          </p>
          <form
            className="mt-7 flex max-w-lg flex-col gap-2 sm:flex-row"
            onSubmit={(e) => {
              e.preventDefault();
              setQuery(cityInput.trim());
            }}
          >
            <input
              className="w-full rounded-[var(--radius-control)] border-0 px-4 py-3 text-sm text-slate-800 shadow-sm focus:outline-none focus-visible:ring-2 focus-visible:ring-white/70"
              placeholder="Busca por ciudad (ej. Bogotá, Medellín)"
              value={cityInput}
              onChange={(e) => setCityInput(e.target.value)}
              aria-label="Buscar sede por ciudad"
            />
            <button className="shrink-0 rounded-[var(--radius-control)] bg-accent-400 px-6 py-3 text-sm font-bold text-brand-900 shadow-soft transition-colors hover:bg-accent-300 focus-visible:ring-2 focus-visible:ring-white/70">
              Buscar canchas
            </button>
          </form>
        </div>
      </section>

      <section>
        <SectionHeading
          title={query ? `Sedes en "${query}"` : 'Todas las sedes'}
          subtitle={venues ? `${venues.length} resultado(s) disponibles` : 'Explora las canchas disponibles'}
        />

        {loading && (
          <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
            {Array.from({ length: 6 }).map((_, i) => (
              <Card key={i} className="overflow-hidden">
                <Skeleton className="h-36 rounded-none" />
                <div className="space-y-3 p-4">
                  <Skeleton className="h-4 w-2/3" />
                  <Skeleton className="h-3 w-full" />
                  <Skeleton className="h-3 w-1/2" />
                </div>
              </Card>
            ))}
          </div>
        )}
        {error && <ErrorBanner message={error} />}
        {venues && venues.length === 0 && (
          <EmptyState
            icon="🔍"
            title="No encontramos sedes"
            subtitle="Prueba con otra ciudad o limpia el filtro de búsqueda."
          />
        )}

        {venues && venues.length > 0 && (
          <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
            {venues.map((venue) => (
              <Link key={venue.id} to={`/sedes/${venue.id}`} className="group focus:outline-none">
                <Card className="flex h-full flex-col overflow-hidden transition-all duration-200 group-hover:-translate-y-1 group-hover:shadow-[var(--shadow-lifted)] group-focus-visible:ring-2 group-focus-visible:ring-brand-500/40">
                  <div className="relative flex h-36 items-center justify-center bg-gradient-to-br from-brand-500 to-brand-700 text-6xl">
                    <span className="drop-shadow-md">🥅</span>
                    {venue.min_price != null && (
                      <span className="absolute bottom-3 right-3 rounded-full bg-white/95 px-3 py-1 text-xs font-bold text-brand-700 shadow-soft">
                        desde {formatCurrency(venue.min_price)}/h
                      </span>
                    )}
                  </div>
                  <div className="flex flex-1 flex-col p-4">
                    <h3 className="font-bold text-slate-900 group-hover:text-brand-700">{venue.name}</h3>
                    <p className="mt-0.5 text-sm text-slate-500">
                      📍 {venue.city} · {venue.address}
                    </p>
                    <div className="mt-3 flex flex-wrap gap-1.5">
                      {venue.services.slice(0, 3).map((service) => (
                        <span key={service} className="rounded-full bg-slate-100 px-2.5 py-0.5 text-xs font-medium text-slate-600">
                          {service}
                        </span>
                      ))}
                    </div>
                    <div className="mt-4 flex items-center justify-between border-t border-slate-100 pt-3">
                      <span className="text-xs font-medium text-slate-500">{venue.court_count} cancha(s)</span>
                      <span className="text-sm font-semibold text-brand-700 group-hover:translate-x-0.5 group-hover:transition-transform">
                        Ver disponibilidad →
                      </span>
                    </div>
                  </div>
                </Card>
              </Link>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
