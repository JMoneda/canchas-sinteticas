import { useState } from 'react';
import { Link } from 'react-router-dom';
import { api, type CreateVenuePayload } from '../api/client';
import type { VenueDetail } from '../api/types';
import { useAsync, errorMessage } from '../lib/useAsync';
import { Button, buttonClasses, Card, EmptyState, ErrorBanner, Field, ModalShell, Spinner, inputClasses } from '../components/ui';

const emptyForm: CreateVenuePayload = {
  name: '',
  city: '',
  address: '',
  phone: '',
  opening_time: '06:00',
  closing_time: '23:00',
  cancellation_window_hours: 3,
  services: [],
  active: true,
};

export function OwnerVenuesPage() {
  const { data, loading, error, reload } = useAsync(() => api.owner.venues.list(), []);
  const [editing, setEditing] = useState<VenueDetail | 'new' | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  async function remove(venue: VenueDetail) {
    if (!confirm(`¿Eliminar la sede "${venue.name}" y todas sus canchas?`)) {
      return;
    }
    setActionError(null);
    try {
      await api.owner.venues.remove(venue.id);
      reload();
    } catch (e) {
      setActionError(errorMessage(e));
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <Link to="/panel" className="text-sm text-brand-700 hover:underline">
            ← Panel
          </Link>
          <h1 className="text-2xl font-bold text-slate-800">Mis sedes</h1>
        </div>
        <Button onClick={() => setEditing('new')}>+ Nueva sede</Button>
      </div>

      {loading && <Spinner />}
      {error && <ErrorBanner message={error} />}
      {actionError && <ErrorBanner message={actionError} />}
      {data && data.length === 0 && (
        <EmptyState title="Aún no tienes sedes" subtitle="Crea tu primera sede para empezar a recibir reservas." />
      )}

      <div className="grid gap-3 sm:grid-cols-2">
        {data?.map((venue) => (
          <Card key={venue.id} className="p-4">
            <div className="flex items-start justify-between">
              <div>
                <h3 className="font-semibold text-slate-800">{venue.name}</h3>
                <p className="text-sm text-slate-500">
                  {venue.city} · {venue.address}
                </p>
                <p className="mt-1 text-xs text-slate-400">
                  {venue.opening_time}–{venue.closing_time} · {venue.courts.length} cancha(s) ·{' '}
                  {venue.active ? 'Activa' : 'Inactiva'}
                </p>
              </div>
            </div>
            <div className="mt-3 flex gap-2">
              <Link to={`/panel/sedes/${venue.id}`} className={buttonClasses('primary', 'sm')}>
                Canchas
              </Link>
              <Button size="sm" variant="secondary" onClick={() => setEditing(venue)}>
                Editar
              </Button>
              <Button size="sm" variant="danger" onClick={() => remove(venue)}>
                Eliminar
              </Button>
            </div>
          </Card>
        ))}
      </div>

      {editing && (
        <VenueFormModal
          venue={editing === 'new' ? null : editing}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null);
            reload();
          }}
        />
      )}
    </div>
  );
}

function VenueFormModal({
  venue,
  onClose,
  onSaved,
}: {
  venue: VenueDetail | null;
  onClose: () => void;
  onSaved: () => void;
}) {
  const [form, setForm] = useState<CreateVenuePayload>(
    venue
      ? {
          name: venue.name,
          city: venue.city,
          address: venue.address,
          phone: venue.phone ?? '',
          opening_time: venue.opening_time,
          closing_time: venue.closing_time,
          cancellation_window_hours: venue.cancellation_window_hours,
          services: venue.services,
          latitude: venue.latitude,
          longitude: venue.longitude,
          active: venue.active,
        }
      : emptyForm,
  );
  const [servicesText, setServicesText] = useState((venue?.services ?? []).join(', '));
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  function update<K extends keyof CreateVenuePayload>(key: K, value: CreateVenuePayload[K]) {
    setForm((f) => ({ ...f, [key]: value }));
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setErr(null);
    const payload: CreateVenuePayload = {
      ...form,
      services: servicesText.split(',').map((s) => s.trim()).filter(Boolean),
    };
    try {
      if (venue) {
        await api.owner.venues.update(venue.id, payload);
      } else {
        await api.owner.venues.create(payload);
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
          <h3 className="text-lg font-semibold text-slate-800">{venue ? 'Editar sede' : 'Nueva sede'}</h3>
          <Field label="Nombre">
            <input className={inputClasses} value={form.name} onChange={(e) => update('name', e.target.value)} required />
          </Field>
          <div className="grid grid-cols-2 gap-3">
            <Field label="Ciudad">
              <input className={inputClasses} value={form.city} onChange={(e) => update('city', e.target.value)} required />
            </Field>
            <Field label="Teléfono">
              <input className={inputClasses} value={form.phone ?? ''} onChange={(e) => update('phone', e.target.value)} />
            </Field>
          </div>
          <Field label="Dirección">
            <input className={inputClasses} value={form.address} onChange={(e) => update('address', e.target.value)} required />
          </Field>
          <div className="grid grid-cols-3 gap-3">
            <Field label="Apertura">
              <input type="time" className={inputClasses} value={form.opening_time} onChange={(e) => update('opening_time', e.target.value)} required />
            </Field>
            <Field label="Cierre">
              <input type="time" className={inputClasses} value={form.closing_time} onChange={(e) => update('closing_time', e.target.value)} required />
            </Field>
            <Field label="Cancelación (h)">
              <input
                type="number"
                min={0}
                className={inputClasses}
                value={form.cancellation_window_hours}
                onChange={(e) => update('cancellation_window_hours', Number(e.target.value))}
              />
            </Field>
          </div>
          <Field label="Servicios" hint="Separados por coma (ej. parqueo, camerinos, cafetería)">
            <input className={inputClasses} value={servicesText} onChange={(e) => setServicesText(e.target.value)} />
          </Field>
          {venue && (
            <label className="flex items-center gap-2 text-sm text-slate-600">
              <input
                type="checkbox"
                checked={form.active ?? true}
                onChange={(e) => update('active', e.target.checked)}
              />
              Sede activa (visible en el marketplace)
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
