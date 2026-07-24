import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { api } from '../api/client';
import type { Reservation, Slot } from '../api/types';
import { useAsync, errorMessage } from '../lib/useAsync';
import { useAuth } from '../auth/AuthContext';
import {
  courtTypeLabel,
  formatCurrency,
  formatLongDate,
  slotStatusClasses,
  todayIso,
} from '../lib/format';
import { Button, Card, ErrorBanner, Field, ModalShell, Spinner, inputClasses } from '../components/ui';

export function VenueDetailPage() {
  const { venueId = '' } = useParams();
  const { isAuthenticated, isOwner } = useAuth();
  const navigate = useNavigate();

  const { data: venue, loading, error } = useAsync(() => api.venues.detail(venueId), [venueId]);
  const [courtId, setCourtId] = useState<string>('');
  const [date, setDate] = useState<string>(todayIso());
  const [selectedSlot, setSelectedSlot] = useState<Slot | null>(null);

  useEffect(() => {
    if (venue && venue.courts.length > 0 && !courtId) {
      setCourtId(venue.courts[0].id);
    }
  }, [venue, courtId]);

  const availability = useAsync(
    () => (courtId ? api.courts.availability(courtId, date) : Promise.resolve(null)),
    [courtId, date],
  );

  if (loading) {
    return <Spinner label="Cargando sede..." />;
  }
  if (error || !venue) {
    return <ErrorBanner message={error ?? 'Sede no encontrada.'} />;
  }

  const selectedCourt = venue.courts.find((c) => c.id === courtId);

  function handleSlotClick(slot: Slot) {
    if (!slot.available) {
      return;
    }
    if (!isAuthenticated) {
      navigate('/login', { state: { from: `/sedes/${venueId}` } });
      return;
    }
    setSelectedSlot(slot);
  }

  return (
    <div className="space-y-6">
      <Link to="/" className="text-sm text-brand-700 hover:underline">
        ← Volver a la búsqueda
      </Link>

      <Card className="overflow-hidden">
        <div className="flex h-40 items-center justify-center bg-gradient-to-br from-brand-200 to-brand-400 text-6xl">
          🏟️
        </div>
        <div className="p-6">
          <h1 className="text-2xl font-bold text-slate-800">{venue.name}</h1>
          <p className="text-slate-500">
            {venue.city} · {venue.address}
          </p>
          <div className="mt-3 flex flex-wrap gap-2 text-sm text-slate-600">
            <span className="rounded-full bg-slate-100 px-3 py-1">
              🕐 {venue.opening_time} – {venue.closing_time}
            </span>
            <span className="rounded-full bg-slate-100 px-3 py-1">
              Cancelación hasta {venue.cancellation_window_hours}h antes
            </span>
            {venue.phone && <span className="rounded-full bg-slate-100 px-3 py-1">📞 {venue.phone}</span>}
          </div>
          {venue.services.length > 0 && (
            <div className="mt-3 flex flex-wrap gap-1.5">
              {venue.services.map((service) => (
                <span key={service} className="rounded-full bg-brand-50 px-2.5 py-0.5 text-xs text-brand-700">
                  {service}
                </span>
              ))}
            </div>
          )}
        </div>
      </Card>

      <div>
        <h2 className="mb-3 text-lg font-semibold text-slate-800">Elige una cancha</h2>
        <div className="flex flex-wrap gap-2">
          {venue.courts.map((court) => (
            <button
              key={court.id}
              onClick={() => {
                setCourtId(court.id);
                setSelectedSlot(null);
              }}
              className={`rounded-xl border px-4 py-3 text-left transition ${
                court.id === courtId
                  ? 'border-brand-500 bg-brand-50'
                  : 'border-slate-200 bg-white hover:border-brand-300'
              }`}
            >
              <p className="font-medium text-slate-800">{court.name}</p>
              <p className="text-xs text-slate-500">
                {courtTypeLabel(court.type)} · {court.surface}
                {court.covered ? ' · Techada' : ''}
              </p>
              {court.min_price != null && (
                <p className="mt-1 text-xs font-semibold text-brand-700">
                  desde {formatCurrency(court.min_price)}/h
                </p>
              )}
            </button>
          ))}
        </div>
      </div>

      <div className="grid gap-4 sm:grid-cols-[220px_1fr]">
        <div>
          <Field label="Fecha">
            <input
              type="date"
              className={inputClasses}
              min={todayIso()}
              value={date}
              onChange={(e) => {
                setDate(e.target.value);
                setSelectedSlot(null);
              }}
            />
          </Field>
          <p className="mt-2 text-sm capitalize text-slate-500">{formatLongDate(date)}</p>
          <div className="mt-4 space-y-1.5 text-xs text-slate-500">
            <LegendDot className="border-brand-300 bg-brand-50" label="Disponible" />
            <LegendDot className="border-slate-200 bg-slate-100" label="Reservado" />
            <LegendDot className="border-rose-200 bg-rose-50" label="Bloqueado" />
          </div>
        </div>

        <div>
          <h2 className="mb-3 text-lg font-semibold text-slate-800">
            Horarios {selectedCourt ? `· ${selectedCourt.name}` : ''}
          </h2>
          {availability.loading && <Spinner label="Consultando disponibilidad..." />}
          {availability.error && <ErrorBanner message={availability.error} />}
          {availability.data && availability.data.slots.length === 0 && (
            <p className="text-sm text-slate-500">No hay horarios configurados para esta fecha.</p>
          )}
          <div className="grid grid-cols-2 gap-2 sm:grid-cols-3 md:grid-cols-4">
            {availability.data?.slots.map((slot) => (
              <button
                key={slot.start_time}
                disabled={!slot.available}
                onClick={() => handleSlotClick(slot)}
                className={`rounded-lg border px-2 py-2.5 text-center transition ${slotStatusClasses(slot.status)}`}
              >
                <span className="block text-sm font-semibold">
                  {slot.start_time}–{slot.end_time}
                </span>
                <span className="block text-xs">{formatCurrency(slot.price)}</span>
              </button>
            ))}
          </div>
          {isOwner && (
            <p className="mt-4 rounded-lg bg-amber-50 px-3 py-2 text-xs text-amber-700">
              Estás con una cuenta de dueño. Para reservar como cliente usa una cuenta de cliente.
            </p>
          )}
        </div>
      </div>

      {selectedSlot && selectedCourt && (
        <BookingModal
          venueName={venue.name}
          courtName={selectedCourt.name}
          courtId={selectedCourt.id}
          date={date}
          slot={selectedSlot}
          onClose={() => setSelectedSlot(null)}
          onBooked={() => {
            setSelectedSlot(null);
            availability.reload();
          }}
        />
      )}
    </div>
  );
}

function LegendDot({ className, label }: { className: string; label: string }) {
  return (
    <div className="flex items-center gap-2">
      <span className={`inline-block h-3 w-3 rounded border ${className}`} />
      {label}
    </div>
  );
}

interface BookingModalProps {
  venueName: string;
  courtName: string;
  courtId: string;
  date: string;
  slot: Slot;
  onClose: () => void;
  onBooked: () => void;
}

function BookingModal({ venueName, courtName, courtId, date, slot, onClose, onBooked }: BookingModalProps) {
  const navigate = useNavigate();
  const [step, setStep] = useState<'confirm' | 'done'>('confirm');
  const [reservation, setReservation] = useState<Reservation | null>(null);
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState<string | null>(null);
  const [paid, setPaid] = useState(false);
  const [openMatch, setOpenMatch] = useState(false);
  const [maxPlayers, setMaxPlayers] = useState(10);
  const [split, setSplit] = useState(false);
  const [notes, setNotes] = useState('');
  const [matchDone, setMatchDone] = useState(false);

  async function confirm() {
    setBusy(true);
    setErr(null);
    try {
      if (openMatch) {
        await api.matches.open({
          court_id: courtId,
          date,
          start_time: slot.start_time,
          end_time: slot.end_time,
          max_players: maxPlayers,
          split,
          notes: notes || undefined,
          payment_method: 'OnlineGateway',
        });
        setMatchDone(true);
      } else {
        const created = await api.reservations.create(courtId, date, slot.start_time, slot.end_time, 'OnlineGateway');
        setReservation(created);
      }
      setStep('done');
    } catch (e) {
      setErr(errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  async function pay() {
    if (!reservation) {
      return;
    }
    setBusy(true);
    setErr(null);
    try {
      await api.reservations.pay(reservation.id, 'OnlineGateway');
      setPaid(true);
    } catch (e) {
      setErr(errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  return (
    <ModalShell onClose={onClose} size="md">
      <div>
          {step === 'confirm' && (
            <>
              <h3 className="text-lg font-semibold text-slate-800">Confirmar reserva</h3>
              <dl className="mt-4 space-y-2 text-sm">
                <Row label="Sede" value={venueName} />
                <Row label="Cancha" value={courtName} />
                <Row label="Fecha" value={formatLongDate(date)} />
                <Row label="Horario" value={`${slot.start_time} – ${slot.end_time}`} />
                <Row label="Total" value={formatCurrency(slot.price)} strong />
              </dl>

              <div className="mt-4 rounded-lg border border-slate-200 p-3">
                <label className="flex items-center gap-2 text-sm font-medium text-slate-700">
                  <input type="checkbox" checked={openMatch} onChange={(e) => setOpenMatch(e.target.checked)} />
                  Abrir como partido (otros jugadores pueden unirse)
                </label>
                {openMatch && (
                  <div className="mt-3 space-y-3">
                    <Field label="Cupo de jugadores">
                      <input
                        type="number"
                        min={2}
                        max={22}
                        className={inputClasses}
                        value={maxPlayers}
                        onChange={(e) => setMaxPlayers(Number(e.target.value))}
                      />
                    </Field>
                    <Field label="Nota (opcional)" hint="Ej. nivel, si falta arquero, etc.">
                      <input className={inputClasses} value={notes} onChange={(e) => setNotes(e.target.value)} />
                    </Field>
                    <label className="flex items-center gap-2 text-sm text-slate-700">
                      <input type="checkbox" checked={split} onChange={(e) => setSplit(e.target.checked)} />
                      Dividir el costo entre los jugadores
                    </label>
                    {split && maxPlayers >= 2 && (
                      <p className="text-xs text-slate-500">
                        Cada jugador pagaría ~{formatCurrency(Math.round(slot.price / maxPlayers))}.
                      </p>
                    )}
                  </div>
                )}
              </div>

              {err && <div className="mt-3"><ErrorBanner message={err} /></div>}
              <div className="mt-6 flex justify-end gap-2">
                <Button variant="secondary" onClick={onClose} disabled={busy}>
                  Cancelar
                </Button>
                <Button onClick={confirm} disabled={busy}>
                  {busy ? 'Procesando...' : openMatch ? 'Abrir partido' : 'Confirmar'}
                </Button>
              </div>
            </>
          )}

          {step === 'done' && matchDone && (
            <>
              <div className="text-center">
                <div className="mx-auto grid h-12 w-12 place-items-center rounded-full bg-brand-100 text-2xl">
                  🤝
                </div>
                <h3 className="mt-3 text-lg font-semibold text-slate-800">¡Partido abierto!</h3>
                <p className="text-sm text-slate-500">
                  Reservaste {courtName} ({slot.start_time}–{slot.end_time}) y lo publicaste con {maxPlayers} cupos.
                </p>
                <p className="mt-1 text-sm text-slate-500">
                  {split
                    ? `Costo dividido: ~${formatCurrency(Math.round(slot.price / maxPlayers))} por jugador.`
                    : 'Otros jugadores ya pueden unirse.'}
                </p>
              </div>
              <div className="mt-6 flex justify-end gap-2">
                <Button variant="secondary" onClick={onBooked}>
                  Listo
                </Button>
                <Button onClick={() => navigate('/partidos')}>Ver partidos</Button>
              </div>
            </>
          )}

          {step === 'done' && reservation && (
            <>
              <div className="text-center">
                <div className="mx-auto grid h-12 w-12 place-items-center rounded-full bg-brand-100 text-2xl">
                  ✅
                </div>
                <h3 className="mt-3 text-lg font-semibold text-slate-800">¡Reserva confirmada!</h3>
                <p className="text-sm text-slate-500">
                  {courtName} · {slot.start_time}–{slot.end_time}
                </p>
              </div>
              <dl className="mt-4 space-y-2 text-sm">
                <Row label="Total" value={formatCurrency(reservation.total_price)} strong />
                <Row label="Pago" value={paid ? 'Pagado ✓' : 'Pendiente'} />
              </dl>
              {err && <div className="mt-3"><ErrorBanner message={err} /></div>}
              <div className="mt-6 flex justify-end gap-2">
                {!paid ? (
                  <>
                    <Button variant="secondary" onClick={onBooked} disabled={busy}>
                      Pagar luego
                    </Button>
                    <Button onClick={pay} disabled={busy}>
                      {busy ? 'Procesando...' : 'Pagar ahora'}
                    </Button>
                  </>
                ) : (
                  <Button onClick={onBooked}>Listo</Button>
                )}
              </div>
            </>
          )}
      </div>
    </ModalShell>
  );
}

function Row({ label, value, strong }: { label: string; value: string; strong?: boolean }) {
  return (
    <div className="flex justify-between gap-4">
      <dt className="text-slate-500">{label}</dt>
      <dd className={strong ? 'font-semibold text-slate-900' : 'text-slate-700'}>{value}</dd>
    </div>
  );
}
