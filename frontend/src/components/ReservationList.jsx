import { useState, useEffect } from 'react'
import { fetchReservations, cancelReservation } from '../services/api'
import ErrorMessage from './ErrorMessage'

export default function ReservationList({ userId, onCancelled }) {
  const [reservations, setReservations] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [cancelErrors, setCancelErrors] = useState({})
  const [noShowNotice, setNoShowNotice] = useState(null)
  const [cancelling, setCancelling] = useState(null)

  useEffect(() => {
    fetchReservations(userId)
      .then(setReservations)
      .catch((err) => setError(err))
      .finally(() => setLoading(false))
  }, [userId])

  async function handleCancel(reservation) {
    setCancelling(reservation.reservation_id)
    setCancelErrors((prev) => ({ ...prev, [reservation.reservation_id]: null }))
    try {
      const result = await cancelReservation(reservation.reservation_id, userId)
      setReservations((prev) => prev.filter((r) => r.reservation_id !== reservation.reservation_id))
      if (result.no_show) {
        setNoShowNotice('Cancelación con menos de 2 horas de anticipación — registrada como no-show.')
      }
      onCancelled()
    } catch (err) {
      setCancelErrors((prev) => ({ ...prev, [reservation.reservation_id]: err }))
    } finally {
      setCancelling(null)
    }
  }

  return (
    <div className="card">
      <div className="res-list-card">
        <h2 className="section-title">Mis reservas</h2>

        {noShowNotice && (
          <div className="banner-warning" style={{ marginTop: '0.75rem' }}>
            ⚠️ {noShowNotice}
          </div>
        )}

        <ErrorMessage error={error} />

        {loading ? (
          <div className="loading-row" style={{ marginTop: '1rem' }}>
            <div className="spinner" />
            Cargando reservas…
          </div>
        ) : reservations.length === 0 ? (
          <div className="empty-state">
            <span className="empty-state-icon">📅</span>
            <p>No tenés reservas activas próximas.</p>
          </div>
        ) : (
          <ul className="res-list" role="list">
            {reservations.map((r) => (
              <li key={r.reservation_id} className="res-item">
                <div className="res-item-icon">🏟️</div>
                <div className="res-item-info">
                  <div className="res-item-name">{r.field_name}</div>
                  <div className="res-item-meta">
                    <span className="meta-tag">📅 {r.date}</span>
                    <span className="meta-tag">🕐 {r.start_time}–{r.end_time}</span>
                  </div>
                  {cancelErrors[r.reservation_id] && (
                    <ErrorMessage error={cancelErrors[r.reservation_id]} />
                  )}
                </div>
                <button
                  className="btn-danger"
                  onClick={() => handleCancel(r)}
                  disabled={cancelling === r.reservation_id}
                >
                  {cancelling === r.reservation_id ? '…' : 'Cancelar'}
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  )
}
