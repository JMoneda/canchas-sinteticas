// Tipos que reflejan los DTOs del backend (JSON en snake_case).

export type Role = 'SuperAdmin' | 'Owner' | 'Client';

export type CourtType = 'Futbol5' | 'Futbol6' | 'Futbol7' | 'Futbol8' | 'Futbol11';

export type ReservationStatus = 'Pending' | 'Confirmed' | 'Cancelled' | 'Completed' | 'NoShow';

export type ReservationChannel = 'Online' | 'Manual';

export type PaymentStatus =
  | 'Pending'
  | 'Processing'
  | 'Paid'
  | 'Rejected'
  | 'Expired'
  | 'RefundRequested'
  | 'Refunded'
  | 'Failed';

/** Métodos de pago reales soportados en Colombia. */
export type PaymentMethod =
  | 'nequi'
  | 'pse'
  | 'bancolombia_transfer'
  | 'bancolombia_button'
  | 'bancolombia_qr'
  | 'card';

export type SlotStatus = 'available' | 'reserved' | 'blocked' | 'past';

export interface AuthResponse {
  token: string;
  user_id: string;
  name: string;
  email: string;
  role: Role;
}

export interface UserProfile {
  id: string;
  name: string;
  email: string;
  phone: string | null;
  role: Role;
}

export interface VenueSummary {
  id: string;
  name: string;
  city: string;
  address: string;
  latitude: number | null;
  longitude: number | null;
  phone: string | null;
  photos: string[];
  services: string[];
  min_price: number | null;
  court_count: number;
}

export interface CourtSummary {
  id: string;
  name: string;
  type: CourtType;
  surface: string;
  covered: boolean;
  slot_duration_minutes: number;
  min_price: number | null;
}

export interface VenueDetail {
  id: string;
  owner_id: string;
  name: string;
  city: string;
  address: string;
  latitude: number | null;
  longitude: number | null;
  phone: string | null;
  photos: string[];
  services: string[];
  opening_time: string;
  closing_time: string;
  cancellation_window_hours: number;
  active: boolean;
  courts: CourtSummary[];
}

export interface PriceRule {
  id: string;
  day_of_week: string | null;
  start_time: string;
  end_time: string;
  price_per_hour: number;
  kind: string;
}

export interface Court {
  id: string;
  venue_id: string;
  name: string;
  type: CourtType;
  surface: string;
  covered: boolean;
  slot_duration_minutes: number;
  active: boolean;
  prices: PriceRule[];
}

export interface Slot {
  start_time: string;
  end_time: string;
  price: number;
  available: boolean;
  status: SlotStatus;
}

export interface CourtAvailability {
  court_id: string;
  court_name: string;
  type: CourtType;
  date: string;
  slots: Slot[];
}

export interface Reservation {
  id: string;
  court_id: string;
  court_name: string;
  venue_id: string;
  venue_name: string;
  client_id: string;
  client_name: string | null;
  client_phone: string | null;
  date: string;
  start_time: string;
  end_time: string;
  total_price: number;
  status: ReservationStatus;
  channel: ReservationChannel;
  payment_status: PaymentStatus;
  created_at: string;
}

export interface CancelResult {
  reservation_id: string;
  status: ReservationStatus;
  no_show: boolean;
  refunded: boolean;
  refund_status: 'none' | 'refund_requested' | 'refunded';
}

export interface PaymentResult {
  reservation_id: string;
  amount: number;
  method: string;
  status: string;
  reference: string | null;
}

/** Respuesta al iniciar un pago: información de checkout del proveedor. */
export interface PaymentInitiation {
  payment_id: string;
  reservation_id: string;
  status: string;
  amount: number;
  method: string;
  checkout_url: string | null;
  expires_at: string | null;
}

/** Configuración de recaudo de una sede. */
export interface VenuePaymentConfig {
  venue_id: string;
  settlement_mode: 'marketplace' | 'direct';
  gateway_merchant_ref: string | null;
}

/** Estado de un pago para consulta/polling. */
export interface PaymentStatusResult {
  payment_id: string;
  reservation_id: string;
  status: PaymentStatus;
  amount: number;
  method: string;
  gateway_reference: string | null;
  paid_at: string | null;
  has_receipt: boolean;
}

export interface Blackout {
  id: string;
  court_id: string;
  date: string;
  start_time: string;
  end_time: string;
  reason: string;
}

export interface CourtReport {
  court_id: string;
  court_name: string;
  venue_name: string;
  reservations: number;
  revenue: number;
}

export interface HourStat {
  hour: string;
  count: number;
}

export interface OwnerReport {
  from: string;
  to: string;
  total_reservations: number;
  total_revenue: number;
  occupancy_rate: number;
  by_court: CourtReport[];
  top_hours: HourStat[];
}

export type MatchStatus = 'Open' | 'Full' | 'Cancelled' | 'Completed';

export interface MatchPlayer {
  user_id: string;
  name: string;
  has_paid: boolean;
}

export interface Match {
  id: string;
  reservation_id: string;
  organizer_id: string;
  venue_id: string;
  venue_name: string;
  city: string;
  court_name: string;
  court_type: CourtType;
  date: string;
  start_time: string;
  end_time: string;
  total_price: number;
  max_players: number;
  spots_left: number;
  split_enabled: boolean;
  price_per_player: number;
  amount_collected: number;
  status: MatchStatus;
  notes: string | null;
  players: MatchPlayer[];
}
