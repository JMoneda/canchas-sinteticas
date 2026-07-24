import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../api/client';
import type { Match } from '../api/types';
import { useAsync, errorMessage } from '../lib/useAsync';
import { useAuth } from '../auth/AuthContext';
import { courtTypeLabel, formatCurrency, formatLongDate } from '../lib/format';
import { Badge, Button, Card, EmptyState, ErrorBanner, Spinner } from '../components/ui';

export function OpenMatchesPage() {
  const { data, loading, error, reload } = useAsync(() => api.matches.list(), []);
  const { user, isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const [busyId, setBusyId] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  async function join(match: Match) {
    if (!isAuthenticated) {
      navigate('/login', { state: { from: '/partidos' } });
      return;
    }
    setBusyId(match.id);
    setActionError(null);
    try {
      await api.matches.join(match.id);
      reload();
    } catch (e) {
      setActionError(errorMessage(e));
    } finally {
      setBusyId(null);
    }
  }

  async function leave(match: Match) {
    setBusyId(match.id);
    setActionError(null);
    try {
      await api.matches.leave(match.id);
      reload();
    } catch (e) {
      setActionError(errorMessage(e));
    } finally {
      setBusyId(null);
    }
  }

  async function pay(match: Match) {
    setBusyId(match.id);
    setActionError(null);
    try {
      await api.matches.pay(match.id);
      reload();
    } catch (e) {
      setActionError(errorMessage(e));
    } finally {
      setBusyId(null);
    }
  }

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-bold text-slate-800">Partidos abiertos</h1>
        <p className="text-slate-500">Únete a un partido con cupos disponibles y completa el equipo.</p>
      </div>

      {loading && <Spinner label="Cargando partidos..." />}
      {error && <ErrorBanner message={error} />}
      {actionError && <ErrorBanner message={actionError} />}
      {data && data.length === 0 && (
        <EmptyState
          title="No hay partidos abiertos"
          subtitle="Abre uno desde una cancha para que otros se unan."
        />
      )}

      <div className="grid gap-4 sm:grid-cols-2">
        {data?.map((match) => {
          const myPlayer = match.players.find((p) => p.user_id === user?.id);
          const isPlayer = myPlayer !== undefined;
          const isOrganizer = match.organizer_id === user?.id;
          const full = match.spots_left <= 0 || match.status !== 'Open';
          const iOwe = match.split_enabled && isPlayer && !myPlayer!.has_paid;
          return (
            <Card key={match.id} className="flex flex-col p-5">
              <div className="flex items-start justify-between">
                <div>
                  <h3 className="font-semibold text-slate-800">{match.venue_name}</h3>
                  <p className="text-sm text-slate-500">
                    {match.court_name} · {courtTypeLabel(match.court_type)} · {match.city}
                  </p>
                </div>
                <Badge className={match.spots_left > 0 ? 'bg-brand-100 text-brand-800' : 'bg-slate-200 text-slate-600'}>
                  {match.spots_left > 0 ? `${match.spots_left} cupos` : 'Completo'}
                </Badge>
              </div>

              <p className="mt-2 text-sm capitalize text-slate-600">
                📅 {formatLongDate(match.date)} · {match.start_time}–{match.end_time}
              </p>
              {match.notes && <p className="mt-1 text-sm text-slate-500">“{match.notes}”</p>}

              <div className="mt-3">
                <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">
                  Jugadores ({match.players.length}/{match.max_players})
                </p>
                <div className="mt-1 flex flex-wrap gap-1.5">
                  {match.players.map((p) => (
                    <span
                      key={p.user_id}
                      className={`rounded-full px-2.5 py-0.5 text-xs ${
                        p.user_id === match.organizer_id
                          ? 'bg-brand-600 text-white'
                          : 'bg-slate-100 text-slate-600'
                      }`}
                    >
                      {p.name}
                      {p.user_id === match.organizer_id ? ' (org.)' : ''}
                      {match.split_enabled && p.has_paid ? ' ✓' : ''}
                    </span>
                  ))}
                </div>
              </div>

              {match.split_enabled && (
                <p className="mt-3 text-sm text-slate-600">
                  💵 Tu parte: <span className="font-semibold text-slate-800">{formatCurrency(match.price_per_player)}</span>
                  <span className="text-slate-400">
                    {' '}· recaudado {formatCurrency(match.amount_collected)} / {formatCurrency(match.total_price)}
                  </span>
                </p>
              )}

              <div className="mt-4 flex items-center justify-between gap-2 border-t border-slate-100 pt-3">
                <span className="text-sm text-slate-500">
                  {match.split_enabled ? 'Pago dividido' : `Cancha: ${formatCurrency(match.total_price)}`}
                  {isOrganizer && ' · organizas'}
                </span>
                <div className="flex items-center gap-2">
                  {iOwe && (
                    <Button size="sm" onClick={() => pay(match)} disabled={busyId === match.id}>
                      Pagar mi parte
                    </Button>
                  )}
                  {isPlayer && match.split_enabled && myPlayer!.has_paid && (
                    <Badge className="bg-brand-100 text-brand-800">Pagaste ✓</Badge>
                  )}
                  {isPlayer && !isOrganizer && (
                    <Button size="sm" variant="secondary" onClick={() => leave(match)} disabled={busyId === match.id}>
                      Salir
                    </Button>
                  )}
                  {!isPlayer && (
                    <Button size="sm" onClick={() => join(match)} disabled={full || busyId === match.id}>
                      {full ? 'Completo' : 'Unirme'}
                    </Button>
                  )}
                </div>
              </div>
            </Card>
          );
        })}
      </div>
    </div>
  );
}
