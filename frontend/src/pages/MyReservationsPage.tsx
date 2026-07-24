import { useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import type { Reservation } from '../api/types';
import { useAsync, errorMessage } from '../lib/useAsync';
import {
  formatCurrency,
  formatLongDate,
  reservationStatusClasses,
  reservationStatusLabel,
} from '../lib/format';
import { Badge, Button, Card, EmptyState, ErrorBanner, Spinner } from '../components/ui';
import { PaymentMethodDialog } from '../components/PaymentMethodDialog';

const TERMINAL_STATUSES = new Set(['Paid', 'Rejected', 'Expired', 'Failed', 'Refunded']);

export function MyReservationsPage() {
  const { data, loading, error, reload } = useAsync(() => api.reservations.mine(), []);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [payTarget, setPayTarget] = useState<Reservation | null>(null);
  const [waitingId, setWaitingId] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const pollRef = useRef<number | null>(null);

  useEffect(() => {
    return () => {
      if (pollRef.current) {
        window.clearInterval(pollRef.current);
      }
    };
  }, []);

  async function downloadReceipt(id: string) {
    setActionError(null);
    try {
      await api.reservations.downloadReceipt(id);
    } catch (e) {
      setActionError(errorMessage(e));
    }
  }

  async function cancel(id: string) {
    if (!confirm('¿Seguro que quieres cancelar esta reserva?')) {
      return;
    }
    setBusyId(id);
    setActionError(null);
    setNotice(null);
    try {
      const result = await api.reservations.cancel(id);
      if (result.refund_status === 'refunded') {
        setNotice('Reserva cancelada. El reembolso fue procesado.');
      } else if (result.refund_status === 'refund_requested') {
        setNotice('Reserva cancelada. El reembolso quedó solicitado y se confirmará en breve.');
      } else if (result.no_show) {
        setNotice('Reserva cancelada fuera del plazo: no aplica reembolso.');
      }
      reload();
    } catch (e) {
      setActionError(errorMessage(e));
    } finally {
      setBusyId(null);
    }
  }

  // Consulta el estado del pago hasta que sea terminal; luego recarga la lista.
  function pollPaymentStatus(paymentId: string, reservationId: string) {
    setWaitingId(reservationId);
    if (pollRef.current) {
      window.clearInterval(pollRef.current);
    }
    pollRef.current = window.setInterval(async () => {
      try {
        const status = await api.payments.getStatus(paymentId);
        if (TERMINAL_STATUSES.has(status.status)) {
          if (pollRef.current) window.clearInterval(pollRef.current);
          pollRef.current = null;
          setWaitingId(null);
          reload();
        }
      } catch {
        // Reintenta en el siguiente tick; el webhook aún puede no haber llegado.
      }
    }, 4000);
  }

  async function confirmPay(method: string) {
    const reservation = payTarget;
    if (!reservation) {
      return;
    }
    setBusyId(reservation.id);
    setActionError(null);
    try {
      const result = await api.reservations.pay(reservation.id, method, window.location.href);
      setPayTarget(null);
      if (result.checkout_url) {
        // Abre el checkout del proveedor y espera la confirmación por webhook.
        window.open(result.checkout_url, '_blank', 'noopener');
      }
      pollPaymentStatus(result.payment_id, reservation.id);
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
      {notice && (
        <div className="rounded-lg border border-brand-200 bg-brand-50 px-4 py-3 text-sm text-brand-800">
          {notice}
        </div>
      )}
      {data && data.length === 0 && (
        <EmptyState
          title="Aún no tienes reservas"
          subtitle="Busca una cancha y haz tu primera reserva."
        />
      )}

      <div className="space-y-3">
        {data?.map((r) => {
          const isPaid = r.payment_status === 'Paid';
          const isActive = r.status === 'Pending' || r.status === 'Confirmed';
          const canPay = isActive && !isPaid;
          const waiting = waitingId === r.id;
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
                {waiting && (
                  <p className="mt-1 text-xs text-slate-500">
                    Esperando la confirmación del pago…
                  </p>
                )}
              </div>
              <div className="flex gap-2">
                {canPay && (
                  <Button
                    size="sm"
                    onClick={() => setPayTarget(r)}
                    disabled={busyId === r.id || waiting}
                  >
                    {waiting ? 'Procesando…' : 'Pagar'}
                  </Button>
                )}
                {isPaid && (
                  <Button size="sm" variant="secondary" onClick={() => downloadReceipt(r.id)} disabled={busyId === r.id}>
                    Comprobante
                  </Button>
                )}
                {isActive && (
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

      {payTarget && (
        <PaymentMethodDialog
          amount={payTarget.total_price}
          busy={busyId === payTarget.id}
          onConfirm={confirmPay}
          onClose={() => setPayTarget(null)}
        />
      )}
    </div>
  );
}
