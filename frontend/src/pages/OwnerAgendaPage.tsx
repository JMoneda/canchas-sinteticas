import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import { useAsync, errorMessage } from '../lib/useAsync';
import {
  formatCurrency,
  formatLongDate,
  reservationStatusClasses,
  reservationStatusLabel,
  todayIso,
} from '../lib/format';
import { Badge, Button, Card, EmptyState, ErrorBanner, Field, ModalShell, Spinner, inputClasses } from '../components/ui';
import { validatePhone } from '../lib/validation';

export function OwnerAgendaPage() {
  const [date, setDate] = useState(todayIso());
  const reservations = useAsync(() => api.owner.reservations.list(date), [date]);
  const venues = useAsync(() => api.owner.venues.list(), []);
  const [showManual, setShowManual] = useState(false);

  const courtOptions = useMemo(() => {
    if (!venues.data) {
      return [];
    }
    return venues.data.flatMap((v) => v.courts.map((c) => ({ id: c.id, label: `${v.name} · ${c.name}` })));
  }, [venues.data]);

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <Link to="/panel" className="text-sm text-brand-700 hover:underline">
            ← Panel
          </Link>
          <h1 className="text-2xl font-bold text-slate-800">Agenda</h1>
        </div>
        <Button onClick={() => setShowManual(true)} disabled={courtOptions.length === 0}>
          + Reserva manual
        </Button>
      </div>

      <div className="flex items-end gap-3">
        <div className="w-52">
          <Field label="Fecha">
            <input type="date" className={inputClasses} value={date} onChange={(e) => setDate(e.target.value)} />
          </Field>
        </div>
        <p className="pb-2 text-sm capitalize text-slate-500">{formatLongDate(date)}</p>
      </div>

      {reservations.loading && <Spinner />}
      {reservations.error && <ErrorBanner message={reservations.error} />}
      {reservations.data && reservations.data.length === 0 && (
        <EmptyState title="Sin reservas para esta fecha" />
      )}

      <div className="space-y-2">
        {reservations.data?.map((r) => (
          <Card key={r.id} className="flex items-center justify-between gap-4 p-4">
            <div className="flex items-center gap-4">
              <div className="w-24 text-center">
                <p className="text-lg font-bold text-slate-800">{r.start_time}</p>
                <p className="text-xs text-slate-400">{r.end_time}</p>
              </div>
              <div>
                <p className="font-medium text-slate-800">{r.court_name}</p>
                <p className="text-sm text-slate-500">
                  {r.venue_name} · {r.channel === 'Manual' ? 'Manual' : 'Online'} ·{' '}
                  {r.channel === 'Manual' ? r.client_name ?? 'Walk-in' : 'Cliente'}
                </p>
              </div>
            </div>
            <div className="flex items-center gap-3">
              <span className="font-semibold text-slate-800">{formatCurrency(r.total_price)}</span>
              <Badge className={reservationStatusClasses(r.status)}>{reservationStatusLabel(r.status)}</Badge>
            </div>
          </Card>
        ))}
      </div>

      {showManual && (
        <ManualReservationModal
          courtOptions={courtOptions}
          defaultDate={date}
          onClose={() => setShowManual(false)}
          onSaved={() => {
            setShowManual(false);
            reservations.reload();
          }}
        />
      )}
    </div>
  );
}

function ManualReservationModal({
  courtOptions,
  defaultDate,
  onClose,
  onSaved,
}: {
  courtOptions: { id: string; label: string }[];
  defaultDate: string;
  onClose: () => void;
  onSaved: () => void;
}) {
  const [courtId, setCourtId] = useState(courtOptions[0]?.id ?? '');
  const [date, setDate] = useState(defaultDate);
  const [startTime, setStartTime] = useState('18:00');
  const [endTime, setEndTime] = useState('19:00');
  const [clientName, setClientName] = useState('');
  const [clientPhone, setClientPhone] = useState('');
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState<string | null>(null);
  const [attempted, setAttempted] = useState(false);

  const timeError = endTime > startTime ? undefined : 'La hora de fin debe ser posterior al inicio.';
  const phoneError = validatePhone(clientPhone) ?? undefined;
  const isValid = !timeError && !phoneError && courtId !== '';

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setAttempted(true);
    if (!isValid) {
      return;
    }
    setBusy(true);
    setErr(null);
    try {
      await api.owner.reservations.createManual(courtId, date, startTime, endTime, clientName || undefined, clientPhone || undefined);
      onSaved();
    } catch (e2) {
      setErr(errorMessage(e2));
    } finally {
      setBusy(false);
    }
  }

  return (
    <ModalShell onClose={onClose} size="md">
      <form onSubmit={submit} className="space-y-4">
          <h3 className="text-lg font-semibold text-slate-800">Reserva manual</h3>
          <Field label="Cancha">
            <select className={inputClasses} value={courtId} onChange={(e) => setCourtId(e.target.value)}>
              {courtOptions.map((o) => (
                <option key={o.id} value={o.id}>
                  {o.label}
                </option>
              ))}
            </select>
          </Field>
          <Field label="Fecha">
            <input type="date" className={inputClasses} value={date} onChange={(e) => setDate(e.target.value)} required />
          </Field>
          <div className="grid grid-cols-2 gap-3">
            <Field label="Desde">
              <input type="time" className={inputClasses} value={startTime} onChange={(e) => setStartTime(e.target.value)} required />
            </Field>
            <Field label="Hasta" error={attempted ? timeError : undefined}>
              <input type="time" className={inputClasses} value={endTime} onChange={(e) => setEndTime(e.target.value)} required />
            </Field>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <Field label="Cliente" hint="Opcional">
              <input className={inputClasses} value={clientName} onChange={(e) => setClientName(e.target.value)} />
            </Field>
            <Field label="Teléfono" hint="Opcional" error={attempted ? phoneError : undefined}>
              <input className={inputClasses} value={clientPhone} onChange={(e) => setClientPhone(e.target.value)} />
            </Field>
          </div>
          {err && <ErrorBanner message={err} />}
          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="secondary" onClick={onClose} disabled={busy}>
              Cancelar
            </Button>
            <Button type="submit" loading={busy} disabled={attempted && !isValid}>
              {busy ? 'Creando...' : 'Crear reserva'}
            </Button>
          </div>
      </form>
    </ModalShell>
  );
}
