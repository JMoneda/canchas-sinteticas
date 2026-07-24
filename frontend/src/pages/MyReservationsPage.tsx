import { useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import { useAsync, errorMessage } from '../lib/useAsync';
import {
  formatCurrency,
  formatLongDate,
  reservationStatusClasses,
  reservationStatusLabel,
} from '../lib/format';
import { Badge, Button, Card, EmptyState, ErrorBanner, Spinner } from '../components/ui';

export function MyReservationsPage() {
  const { data, loading, error, reload } = useAsync(() => api.reservations.mine(), []);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  async function cancel(id: string) {
    if (!confirm('¿Seguro que quieres cancelar esta reserva?')) {
      return;
    }
    setBusyId(id);
    setActionError(null);
    try {
      await api.reservations.cancel(id);
      reload();
    } catch (e) {
      setActionError(errorMessage(e));
    } finally {
      setBusyId(null);
    }
  }

  async function pay(id: string) {
    setBusyId(id);
    setActionError(null);
    try {
      await api.reservations.pay(id, 'OnlineGateway');
      reload();
    } catch (e) {
      setActionError(errorMessage(e));
    } finally {
      setBusyId(null);
    }
  }

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold text-slate-800">Mis reservas</h1>

      {loading && <Spinner label="Cargando reservas..." />}
      {error && <ErrorBanner message={error} />}
      {actionError && <ErrorBanner message={actionError} />}
      {data && data.length === 0 && (
        <EmptyState
          title="Aún no tienes reservas"
          subtitle="Busca una cancha y haz tu primera reserva."
        />
      )}

      <div className="space-y-3">
        {data?.map((r) => {
          const isConfirmed = r.status === 'Confirmed';
          const isPaid = r.payment_status === 'Paid';
          return (
            <Card key={r.id} className="flex flex-col gap-4 p-4 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <div className="flex items-center gap-2">
                  <h3 className="font-semibold text-slate-800">{r.venue_name}</h3>
                  <Badge className={reservationStatusClasses(r.status)}>
                    {reservationStatusLabel(r.status)}
                  </Badge>
                </div>
                <p className="text-sm text-slate-500">
                  {r.court_name} · <span className="capitalize">{formatLongDate(r.date)}</span> ·{' '}
                  {r.start_time}–{r.end_time}
                </p>
                <p className="mt-1 text-sm">
                  <span className="font-semibold text-slate-800">{formatCurrency(r.total_price)}</span>{' '}
                  <span className={isPaid ? 'text-brand-600' : 'text-amber-600'}>
                    · {isPaid ? 'Pagado' : 'Pago pendiente'}
                  </span>
                </p>
              </div>
              <div className="flex gap-2">
                {isConfirmed && !isPaid && (
                  <Button size="sm" onClick={() => pay(r.id)} disabled={busyId === r.id}>
                    Pagar
                  </Button>
                )}
                {isConfirmed && (
                  <Button size="sm" variant="danger" onClick={() => cancel(r.id)} disabled={busyId === r.id}>
                    Cancelar
                  </Button>
                )}
              </div>
            </Card>
          );
        })}
      </div>

      <div className="pt-2">
        <Link to="/" className="text-sm font-medium text-brand-700 hover:underline">
          + Reservar otra cancha
        </Link>
      </div>
    </div>
  );
}
