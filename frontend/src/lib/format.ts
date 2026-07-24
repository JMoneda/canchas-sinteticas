import type { CourtType, ReservationStatus, SlotStatus } from '../api/types';

const currencyFormatter = new Intl.NumberFormat('es-CO', {
  style: 'currency',
  currency: 'COP',
  maximumFractionDigits: 0,
});

export function formatCurrency(value: number): string {
  return currencyFormatter.format(value);
}

const courtTypeLabels: Record<CourtType, string> = {
  Futbol5: 'Fútbol 5',
  Futbol6: 'Fútbol 6',
  Futbol7: 'Fútbol 7',
  Futbol8: 'Fútbol 8',
  Futbol11: 'Fútbol 11',
};

export function courtTypeLabel(type: CourtType): string {
  return courtTypeLabels[type] ?? type;
}

export const courtTypeOptions: { value: CourtType; label: string }[] = (
  Object.keys(courtTypeLabels) as CourtType[]
).map((value) => ({ value, label: courtTypeLabels[value] }));

const reservationStatusLabels: Record<ReservationStatus, string> = {
  Pending: 'Pendiente de pago',
  Confirmed: 'Confirmada',
  Cancelled: 'Cancelada',
  Completed: 'Completada',
  NoShow: 'No-show',
};

export function reservationStatusLabel(status: ReservationStatus): string {
  return reservationStatusLabels[status] ?? status;
}

export function reservationStatusClasses(status: ReservationStatus): string {
  switch (status) {
    case 'Confirmed':
      return 'bg-brand-100 text-brand-800';
    case 'Pending':
      return 'bg-amber-100 text-amber-700';
    case 'Completed':
      return 'bg-sky-100 text-sky-800';
    case 'Cancelled':
      return 'bg-slate-200 text-slate-600';
    case 'NoShow':
      return 'bg-rose-100 text-rose-700';
    default:
      return 'bg-slate-200 text-slate-600';
  }
}

/** Métodos de pago disponibles en el checkout (Colombia). */
export const paymentMethodOptions: { value: string; label: string }[] = [
  { value: 'nequi', label: 'Nequi' },
  { value: 'pse', label: 'PSE (débito bancario)' },
  { value: 'bancolombia_transfer', label: 'Transferencia Bancolombia' },
  { value: 'bancolombia_button', label: 'Botón Bancolombia' },
  { value: 'bancolombia_qr', label: 'Bancolombia QR' },
  { value: 'card', label: 'Tarjeta de crédito/débito' },
];

export function slotStatusClasses(status: SlotStatus): string {
  switch (status) {
    case 'available':
      return 'border-brand-300 bg-brand-50 text-brand-800 hover:border-brand-500 hover:bg-brand-100';
    case 'reserved':
      return 'border-slate-200 bg-slate-100 text-slate-400 cursor-not-allowed';
    case 'blocked':
      return 'border-rose-200 bg-rose-50 text-rose-400 cursor-not-allowed';
    case 'past':
      return 'border-slate-200 bg-slate-50 text-slate-300 cursor-not-allowed';
    default:
      return 'border-slate-200 bg-slate-50 text-slate-400';
  }
}

export const dayOfWeekOptions: { value: string; label: string }[] = [
  { value: '', label: 'Todos los días' },
  { value: 'Monday', label: 'Lunes' },
  { value: 'Tuesday', label: 'Martes' },
  { value: 'Wednesday', label: 'Miércoles' },
  { value: 'Thursday', label: 'Jueves' },
  { value: 'Friday', label: 'Viernes' },
  { value: 'Saturday', label: 'Sábado' },
  { value: 'Sunday', label: 'Domingo' },
];

export function todayIso(): string {
  const now = new Date();
  const offset = now.getTimezoneOffset() * 60000;
  return new Date(now.getTime() - offset).toISOString().slice(0, 10);
}

export function formatLongDate(iso: string): string {
  const [year, month, day] = iso.split('-').map(Number);
  const date = new Date(year, month - 1, day);
  return date.toLocaleDateString('es-CO', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });
}
