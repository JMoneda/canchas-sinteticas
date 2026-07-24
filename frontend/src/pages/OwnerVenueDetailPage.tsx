import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { api, type CreateCourtPayload, type PriceRulePayload } from '../api/client';
import type { Court } from '../api/types';
import { useAsync, errorMessage } from '../lib/useAsync';
import {
  courtTypeLabel,
  courtTypeOptions,
  dayOfWeekOptions,
  formatCurrency,
} from '../lib/format';
import { Button, Card, EmptyState, ErrorBanner, Field, ModalShell, Spinner, inputClasses } from '../components/ui';

const emptyCourt: CreateCourtPayload = {
  name: '',
  type: 'Futbol5',
  surface: 'Sintética',
  covered: false,
  slot_duration_minutes: 60,
  active: true,
};

export function OwnerVenueDetailPage() {
  const { venueId = '' } = useParams();
  const venue = useAsync(() => api.venues.detail(venueId), [venueId]);
  const courts = useAsync(() => api.owner.venues.courts(venueId), [venueId]);

  const [courtForm, setCourtForm] = useState<Court | 'new' | null>(null);
  const [pricesFor, setPricesFor] = useState<Court | null>(null);
  const [blackoutsFor, setBlackoutsFor] = useState<Court | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  async function removeCourt(court: Court) {
    if (!confirm(`¿Eliminar la cancha "${court.name}"?`)) {
      return;
    }
    setActionError(null);
    try {
      await api.owner.courts.remove(court.id);
      courts.reload();
    } catch (e) {
      setActionError(errorMessage(e));
    }
  }

  return (
    <div className="space-y-4">
      <div>
        <Link to="/panel/sedes" className="text-sm text-brand-700 hover:underline">
          ← Mis sedes
        </Link>
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-bold text-slate-800">{venue.data?.name ?? 'Sede'}</h1>
          <Button onClick={() => setCourtForm('new')}>+ Nueva cancha</Button>
        </div>
        {venue.data && (
          <p className="text-sm text-slate-500">
            {venue.data.city} · {venue.data.opening_time}–{venue.data.closing_time}
          </p>
        )}
      </div>

      {courts.loading && <Spinner />}
      {courts.error && <ErrorBanner message={courts.error} />}
      {actionError && <ErrorBanner message={actionError} />}
      {courts.data && courts.data.length === 0 && (
        <EmptyState title="Esta sede no tiene canchas" subtitle="Agrega una cancha y configura sus tarifas." />
      )}

      <div className="grid gap-3 lg:grid-cols-2">
        {courts.data?.map((court) => (
          <Card key={court.id} className="p-4">
            <div className="flex items-start justify-between">
              <div>
                <h3 className="font-semibold text-slate-800">{court.name}</h3>
                <p className="text-sm text-slate-500">
                  {courtTypeLabel(court.type)} · {court.surface}
                  {court.covered ? ' · Techada' : ''} · bloques de {court.slot_duration_minutes} min
                </p>
              </div>
              {!court.active && (
                <span className="rounded-full bg-slate-200 px-2 py-0.5 text-xs text-slate-600">Inactiva</span>
              )}
            </div>

            <div className="mt-3 rounded-lg bg-slate-50 p-3">
              <p className="mb-1 text-xs font-semibold uppercase tracking-wide text-slate-400">Tarifas</p>
              {court.prices.length === 0 ? (
                <p className="text-sm text-rose-500">Sin tarifas configuradas (no se puede reservar).</p>
              ) : (
                <ul className="space-y-0.5 text-sm text-slate-600">
                  {court.prices.map((p) => (
                    <li key={p.id} className="flex justify-between">
                      <span>
                        {p.kind} · {p.day_of_week ?? 'Todos'} {p.start_time}–{p.end_time}
                      </span>
                      <span className="font-medium">{formatCurrency(p.price_per_hour)}/h</span>
                    </li>
                  ))}
                </ul>
              )}
            </div>

            <div className="mt-3 flex flex-wrap gap-2">
              <Button size="sm" onClick={() => setPricesFor(court)}>
                Tarifas
              </Button>
              <Button size="sm" variant="secondary" onClick={() => setBlackoutsFor(court)}>
                Bloqueos
              </Button>
              <Button size="sm" variant="secondary" onClick={() => setCourtForm(court)}>
                Editar
              </Button>
              <Button size="sm" variant="danger" onClick={() => removeCourt(court)}>
                Eliminar
              </Button>
            </div>
          </Card>
        ))}
      </div>

      {courtForm && (
        <CourtFormModal
          venueId={venueId}
          court={courtForm === 'new' ? null : courtForm}
          onClose={() => setCourtForm(null)}
          onSaved={() => {
            setCourtForm(null);
            courts.reload();
          }}
        />
      )}
      {pricesFor && (
        <PricesModal
          court={pricesFor}
          onClose={() => setPricesFor(null)}
          onSaved={() => {
            setPricesFor(null);
            courts.reload();
          }}
        />
      )}
      {blackoutsFor && <BlackoutsModal court={blackoutsFor} onClose={() => setBlackoutsFor(null)} />}
    </div>
  );
}

function CourtFormModal({
  venueId,
  court,
  onClose,
  onSaved,
}: {
  venueId: string;
  court: Court | null;
  onClose: () => void;
  onSaved: () => void;
}) {
  const [form, setForm] = useState<CreateCourtPayload>(
    court
      ? {
          name: court.name,
          type: court.type,
          surface: court.surface,
          covered: court.covered,
          slot_duration_minutes: court.slot_duration_minutes,
          active: court.active,
        }
      : emptyCourt,
  );
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  function update<K extends keyof CreateCourtPayload>(key: K, value: CreateCourtPayload[K]) {
    setForm((f) => ({ ...f, [key]: value }));
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setErr(null);
    try {
      if (court) {
        await api.owner.courts.update(court.id, form);
      } else {
        await api.owner.venues.createCourt(venueId, form);
      }
      onSaved();
    } catch (e2) {
      setErr(errorMessage(e2));
    } finally {
      setBusy(false);
    }
  }

  return (
    <ModalShell onClose={onClose}>
      <form onSubmit={submit} className="space-y-4">
        <h3 className="text-lg font-semibold text-slate-800">{court ? 'Editar cancha' : 'Nueva cancha'}</h3>
        <Field label="Nombre">
          <input className={inputClasses} value={form.name} onChange={(e) => update('name', e.target.value)} required />
        </Field>
        <div className="grid grid-cols-2 gap-3">
          <Field label="Modalidad">
            <select className={inputClasses} value={form.type} onChange={(e) => update('type', e.target.value)}>
              {courtTypeOptions.map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
            </select>
          </Field>
          <Field label="Bloque (min)">
            <select
              className={inputClasses}
              value={form.slot_duration_minutes}
              onChange={(e) => update('slot_duration_minutes', Number(e.target.value))}
            >
              {[30, 60, 90, 120].map((m) => (
                <option key={m} value={m}>
                  {m} min
                </option>
              ))}
            </select>
          </Field>
        </div>
        <Field label="Superficie">
          <input className={inputClasses} value={form.surface} onChange={(e) => update('surface', e.target.value)} />
        </Field>
        <label className="flex items-center gap-2 text-sm text-slate-600">
          <input type="checkbox" checked={form.covered} onChange={(e) => update('covered', e.target.checked)} />
          Cancha techada
        </label>
        {court && (
          <label className="flex items-center gap-2 text-sm text-slate-600">
            <input type="checkbox" checked={form.active ?? true} onChange={(e) => update('active', e.target.checked)} />
            Cancha activa
          </label>
        )}
        {err && <ErrorBanner message={err} />}
        <div className="flex justify-end gap-2 pt-2">
          <Button type="button" variant="secondary" onClick={onClose} disabled={busy}>
            Cancelar
          </Button>
          <Button type="submit" disabled={busy}>
            {busy ? 'Guardando...' : 'Guardar'}
          </Button>
        </div>
      </form>
    </ModalShell>
  );
}

interface PriceRow extends PriceRulePayload {}

function PricesModal({ court, onClose, onSaved }: { court: Court; onClose: () => void; onSaved: () => void }) {
  const [rows, setRows] = useState<PriceRow[]>(
    court.prices.length > 0
      ? court.prices.map((p) => ({
          day_of_week: p.day_of_week ?? '',
          start_time: p.start_time,
          end_time: p.end_time,
          price_per_hour: p.price_per_hour,
          kind: p.kind,
        }))
      : [{ day_of_week: '', start_time: '06:00', end_time: '18:00', price_per_hour: 60000, kind: 'diurno' }],
  );
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  function updateRow(index: number, patch: Partial<PriceRow>) {
    setRows((rs) => rs.map((r, i) => (i === index ? { ...r, ...patch } : r)));
  }

  function addRow() {
    setRows((rs) => [...rs, { day_of_week: '', start_time: '18:00', end_time: '23:00', price_per_hour: 90000, kind: 'nocturno' }]);
  }

  function removeRow(index: number) {
    setRows((rs) => rs.filter((_, i) => i !== index));
  }

  async function save() {
    setBusy(true);
    setErr(null);
    try {
      await api.owner.courts.setPrices(
        court.id,
        rows.map((r) => ({ ...r, day_of_week: r.day_of_week || null })),
      );
      onSaved();
    } catch (e) {
      setErr(errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  return (
    <ModalShell onClose={onClose}>
      <div className="space-y-4">
        <div>
          <h3 className="text-lg font-semibold text-slate-800">Tarifas · {court.name}</h3>
          <p className="text-sm text-slate-500">Define precios por franja horaria y día.</p>
        </div>

        <div className="space-y-2">
          {rows.map((row, i) => (
            <div key={i} className="grid grid-cols-12 items-end gap-2 rounded-lg border border-slate-200 p-2">
              <div className="col-span-4">
                <span className="mb-0.5 block text-xs text-slate-500">Día</span>
                <select
                  className={inputClasses}
                  value={row.day_of_week ?? ''}
                  onChange={(e) => updateRow(i, { day_of_week: e.target.value })}
                >
                  {dayOfWeekOptions.map((o) => (
                    <option key={o.value} value={o.value}>
                      {o.label}
                    </option>
                  ))}
                </select>
              </div>
              <div className="col-span-3">
                <span className="mb-0.5 block text-xs text-slate-500">Desde</span>
                <input type="time" className={inputClasses} value={row.start_time} onChange={(e) => updateRow(i, { start_time: e.target.value })} />
              </div>
              <div className="col-span-3">
                <span className="mb-0.5 block text-xs text-slate-500">Hasta</span>
                <input type="time" className={inputClasses} value={row.end_time} onChange={(e) => updateRow(i, { end_time: e.target.value })} />
              </div>
              <div className="col-span-2 flex justify-end">
                <button onClick={() => removeRow(i)} className="rounded-lg px-2 py-2 text-rose-500 hover:bg-rose-50" title="Quitar">
                  ✕
                </button>
              </div>
              <div className="col-span-7">
                <span className="mb-0.5 block text-xs text-slate-500">Etiqueta</span>
                <input className={inputClasses} value={row.kind} onChange={(e) => updateRow(i, { kind: e.target.value })} />
              </div>
              <div className="col-span-5">
                <span className="mb-0.5 block text-xs text-slate-500">Precio/hora</span>
                <input
                  type="number"
                  min={0}
                  step={1000}
                  className={inputClasses}
                  value={row.price_per_hour}
                  onChange={(e) => updateRow(i, { price_per_hour: Number(e.target.value) })}
                />
              </div>
            </div>
          ))}
        </div>

        <Button size="sm" variant="secondary" onClick={addRow}>
          + Agregar franja
        </Button>

        {err && <ErrorBanner message={err} />}
        <div className="flex justify-end gap-2 pt-2">
          <Button variant="secondary" onClick={onClose} disabled={busy}>
            Cancelar
          </Button>
          <Button onClick={save} disabled={busy}>
            {busy ? 'Guardando...' : 'Guardar tarifas'}
          </Button>
        </div>
      </div>
    </ModalShell>
  );
}

function BlackoutsModal({ court, onClose }: { court: Court; onClose: () => void }) {
  const { data, loading, error, reload } = useAsync(() => api.owner.courts.blackouts(court.id), [court.id]);
  const [form, setForm] = useState({ date: '', start_time: '06:00', end_time: '23:00', reason: 'Mantenimiento' });
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  async function add(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setErr(null);
    try {
      await api.owner.courts.createBlackout(court.id, form.date, form.start_time, form.end_time, form.reason);
      reload();
    } catch (e2) {
      setErr(errorMessage(e2));
    } finally {
      setBusy(false);
    }
  }

  async function remove(id: string) {
    try {
      await api.owner.blackouts.remove(id);
      reload();
    } catch (e2) {
      setErr(errorMessage(e2));
    }
  }

  return (
    <ModalShell onClose={onClose}>
      <div className="space-y-4">
        <h3 className="text-lg font-semibold text-slate-800">Bloqueos · {court.name}</h3>

        {loading && <Spinner />}
        {error && <ErrorBanner message={error} />}
        {data && data.length === 0 && <p className="text-sm text-slate-500">No hay bloqueos.</p>}
        <ul className="space-y-1.5">
          {data?.map((b) => (
            <li key={b.id} className="flex items-center justify-between rounded-lg bg-slate-50 px-3 py-2 text-sm">
              <span>
                {b.date} · {b.start_time}–{b.end_time} · {b.reason}
              </span>
              <button onClick={() => remove(b.id)} className="text-rose-500 hover:underline">
                Quitar
              </button>
            </li>
          ))}
        </ul>

        <form onSubmit={add} className="space-y-3 border-t border-slate-100 pt-4">
          <p className="text-sm font-medium text-slate-700">Nuevo bloqueo</p>
          <div className="grid grid-cols-3 gap-2">
            <Field label="Fecha">
              <input type="date" className={inputClasses} value={form.date} onChange={(e) => setForm({ ...form, date: e.target.value })} required />
            </Field>
            <Field label="Desde">
              <input type="time" className={inputClasses} value={form.start_time} onChange={(e) => setForm({ ...form, start_time: e.target.value })} />
            </Field>
            <Field label="Hasta">
              <input type="time" className={inputClasses} value={form.end_time} onChange={(e) => setForm({ ...form, end_time: e.target.value })} />
            </Field>
          </div>
          <Field label="Motivo">
            <input className={inputClasses} value={form.reason} onChange={(e) => setForm({ ...form, reason: e.target.value })} />
          </Field>
          {err && <ErrorBanner message={err} />}
          <div className="flex justify-end">
            <Button type="submit" disabled={busy}>
              {busy ? 'Agregando...' : 'Agregar bloqueo'}
            </Button>
          </div>
        </form>
      </div>
    </ModalShell>
  );
}
