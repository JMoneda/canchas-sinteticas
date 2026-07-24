import type {
  AuthResponse,
  Blackout,
  Court,
  CourtAvailability,
  Match,
  OwnerReport,
  PaymentInitiation,
  PaymentStatusResult,
  VenuePaymentConfig,
  CancelResult,
  Reservation,
  UserProfile,
  VenueDetail,
  VenueSummary,
} from './types';

const BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:8080/api';
const TOKEN_KEY = 'cs_token';

/** Error de la API con el código de negocio y el mensaje del backend. */
export class ApiError extends Error {
  errorType: string;
  status: number;

  constructor(message: string, errorType: string, status: number) {
    super(message);
    this.name = 'ApiError';
    this.errorType = errorType;
    this.status = status;
  }
}

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function setToken(token: string | null): void {
  if (token) {
    localStorage.setItem(TOKEN_KEY, token);
  } else {
    localStorage.removeItem(TOKEN_KEY);
  }
}

async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
  const headers: Record<string, string> = {};
  const token = getToken();
  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }
  if (body !== undefined) {
    headers['Content-Type'] = 'application/json';
  }

  const response = await fetch(`${BASE_URL}${path}`, {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });

  if (response.status === 204) {
    return undefined as T;
  }

  const text = await response.text();
  const data = text ? JSON.parse(text) : null;

  if (!response.ok) {
    const message = data?.message ?? 'Ocurrió un error inesperado.';
    const errorType = data?.error_type ?? 'ERROR';
    throw new ApiError(message, errorType, response.status);
  }

  return data as T;
}

/** Descarga un archivo (p. ej. un PDF) autenticado y dispara la descarga en el navegador. */
async function downloadFile(path: string, filename: string): Promise<void> {
  const token = getToken();
  const response = await fetch(`${BASE_URL}${path}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });
  if (!response.ok) {
    const text = await response.text();
    const data = text ? JSON.parse(text) : null;
    throw new ApiError(data?.message ?? 'No se pudo descargar el comprobante.', data?.error_type ?? 'ERROR', response.status);
  }
  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

function query(params: Record<string, string | undefined>): string {
  const entries = Object.entries(params).filter(([, v]) => v !== undefined && v !== '');
  if (entries.length === 0) {
    return '';
  }
  return '?' + entries.map(([k, v]) => `${k}=${encodeURIComponent(v as string)}`).join('&');
}

export interface RegisterPayload {
  name: string;
  email: string;
  phone?: string;
  password: string;
  role: 'Owner' | 'Client';
}

export interface CreateVenuePayload {
  name: string;
  city: string;
  address: string;
  latitude?: number | null;
  longitude?: number | null;
  phone?: string | null;
  photos?: string[];
  services?: string[];
  opening_time: string;
  closing_time: string;
  cancellation_window_hours: number;
  active?: boolean;
}

export interface CreateCourtPayload {
  name: string;
  type: string;
  surface: string;
  covered: boolean;
  slot_duration_minutes: number;
  active?: boolean;
}

export interface PriceRulePayload {
  day_of_week?: string | null;
  start_time: string;
  end_time: string;
  price_per_hour: number;
  kind: string;
}

export interface OpenMatchPayload {
  court_id: string;
  date: string;
  start_time: string;
  end_time: string;
  max_players: number;
  split: boolean;
  notes?: string;
  payment_method?: string;
}

export const api = {
  auth: {
    login: (email: string, password: string) =>
      request<AuthResponse>('POST', '/auth/login', { email, password }),
    register: (payload: RegisterPayload) =>
      request<AuthResponse>('POST', '/auth/register', payload),
    me: () => request<UserProfile>('GET', '/auth/me'),
  },
  venues: {
    search: (city?: string) =>
      request<VenueSummary[]>('GET', `/venues${query({ city })}`),
    detail: (venueId: string) =>
      request<VenueDetail>('GET', `/venues/${venueId}`),
  },
  courts: {
    availability: (courtId: string, date: string) =>
      request<CourtAvailability>('GET', `/courts/${courtId}/availability${query({ date })}`),
  },
  reservations: {
    create: (courtId: string, date: string, startTime: string, endTime: string, paymentMethod?: string) =>
      request<Reservation>('POST', '/reservations', {
        court_id: courtId,
        date,
        start_time: startTime,
        end_time: endTime,
        payment_method: paymentMethod,
      }),
    mine: () => request<Reservation[]>('GET', '/reservations'),
    cancel: (id: string) => request<CancelResult>('DELETE', `/reservations/${id}`),
    pay: (id: string, method: string, returnUrl?: string) =>
      request<PaymentInitiation>('POST', `/reservations/${id}/pay`, {
        method,
        return_url: returnUrl,
      }),
    downloadReceipt: (id: string) =>
      downloadFile(`/reservations/${id}/receipt`, `comprobante-${id}.pdf`),
  },
  payments: {
    getStatus: (paymentId: string) =>
      request<PaymentStatusResult>('GET', `/payments/${paymentId}`),
  },
  owner: {
    venues: {
      list: () => request<VenueDetail[]>('GET', '/owner/venues'),
      create: (payload: CreateVenuePayload) =>
        request<VenueDetail>('POST', '/owner/venues', payload),
      update: (venueId: string, payload: CreateVenuePayload) =>
        request<VenueDetail>('PUT', `/owner/venues/${venueId}`, payload),
      remove: (venueId: string) => request<void>('DELETE', `/owner/venues/${venueId}`),
      courts: (venueId: string) => request<Court[]>('GET', `/owner/venues/${venueId}/courts`),
      createCourt: (venueId: string, payload: CreateCourtPayload) =>
        request<Court>('POST', `/owner/venues/${venueId}/courts`, payload),
      getPaymentConfig: (venueId: string) =>
        request<VenuePaymentConfig>('GET', `/owner/venues/${venueId}/payment-config`),
      setPaymentConfig: (venueId: string, settlementMode: string, gatewayMerchantRef?: string) =>
        request<VenuePaymentConfig>('PUT', `/owner/venues/${venueId}/payment-config`, {
          settlement_mode: settlementMode,
          gateway_merchant_ref: gatewayMerchantRef,
        }),
    },
    courts: {
      update: (courtId: string, payload: CreateCourtPayload) =>
        request<Court>('PUT', `/owner/courts/${courtId}`, payload),
      remove: (courtId: string) => request<void>('DELETE', `/owner/courts/${courtId}`),
      setPrices: (courtId: string, rules: PriceRulePayload[]) =>
        request<Court>('PUT', `/owner/courts/${courtId}/prices`, { rules }),
      blackouts: (courtId: string) =>
        request<Blackout[]>('GET', `/owner/courts/${courtId}/blackouts`),
      createBlackout: (courtId: string, date: string, startTime: string, endTime: string, reason: string) =>
        request<Blackout>('POST', `/owner/courts/${courtId}/blackouts`, {
          date,
          start_time: startTime,
          end_time: endTime,
          reason,
        }),
    },
    blackouts: {
      remove: (blackoutId: string) => request<void>('DELETE', `/owner/blackouts/${blackoutId}`),
    },
    reservations: {
      list: (date?: string) =>
        request<Reservation[]>('GET', `/owner/reservations${query({ date })}`),
      createManual: (courtId: string, date: string, startTime: string, endTime: string, clientName?: string, clientPhone?: string) =>
        request<Reservation>('POST', '/owner/reservations', {
          court_id: courtId,
          date,
          start_time: startTime,
          end_time: endTime,
          client_name: clientName,
          client_phone: clientPhone,
        }),
    },
    reports: {
      get: (from?: string, to?: string) =>
        request<OwnerReport>('GET', `/owner/reports${query({ from, to })}`),
    },
  },
  matches: {
    list: (city?: string) => request<Match[]>('GET', `/matches${query({ city })}`),
    detail: (id: string) => request<Match>('GET', `/matches/${id}`),
    open: (payload: OpenMatchPayload) => request<Match>('POST', '/matches', payload),
    join: (id: string) => request<Match>('POST', `/matches/${id}/join`),
    leave: (id: string) => request<Match>('POST', `/matches/${id}/leave`),
    payShare: (id: string, method: string, returnUrl?: string) =>
      request<PaymentInitiation>('POST', `/matches/${id}/pay-share`, {
        method,
        return_url: returnUrl,
      }),
    downloadShareReceipt: (id: string) =>
      downloadFile(`/matches/${id}/players/me/receipt`, `comprobante-partido-${id}.pdf`),
  },
};
