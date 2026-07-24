// Tipos que reflejan los DTOs del backend (JSON en snake_case).

export type Role = 'SuperAdmin' | 'Owner' | 'Client';

export type CourtType = 'Futbol5' | 'Futbol6' | 'Futbol7' | 'Futbol8' | 'Futbol11';

export type ReservationStatus = 'Confirmed' | 'Cancelled' | 'Completed' | 'NoShow';

export type ReservationChannel = 'Online' | 'Manual';

export type PaymentStatus = 'Pending' | 'Paid' | 'Refunded' | 'Failed';

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
}

export interface PaymentResult {
  reservation_id: string;
  amount: number;
  method: string;
  status: string;
  reference: string | null;
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
