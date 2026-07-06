import { useState } from 'react'
import { createReservation } from '../services/api'
import ErrorMessage from './ErrorMessage'

export default function ReservationForm({ userId, field, slot, onSuccess, onCancel }) {
  const [error, setError] = useState(null)
  const [loading, setLoading] = useState(false)
  const [confirmed, setConfirmed] = useState(null)

  async function handleSubmit() {
    setLoading(true)
    setError(null)
    try {
      const result = await createReservation({
        user_id: userId,
        field_id: field.field_id,
        date: slot.date,
        start_time: slot.start_time,
        end_time: slot.end_time,
      })
      setConfirmed(result)
    } catch (err) {
      setError(err)
    } finally {
      setLoading(false)
    }
  }

  if (confirmed) {
    return (
      <div className="card">
        <div className="success-wrap">
          <span className="success-icon">✅</span>
          <h3 className="success-title">¡Reserva confirmada!</h3>
          <p className="success-detail">
            <strong>{field.field_name}</strong> · {slot.date} · {slot.start_time}–{slot.end_time}
          </p>
          <span className="success-id">ID {confirmed.reservation_id}</span>
          <button className="btn-primary" onClick={onSuccess}>
            Aceptar
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className="card">
      <div className="res-form-card">
        <h3 className="res-form-title">Confirmar reserva</h3>

        <div className="info-grid">
          <InfoRow label="Cancha"  value={field.field_name} />
          <InfoRow label="Fecha"   value={slot.date} />
          <InfoRow label="Horario" value={`${slot.start_time} – ${slot.end_time}`} />
          <InfoRow label="Usuario" value={userId} />
        </div>

        <ErrorMessage error={error} />

        <div className="form-actions" style={{ marginTop: error ? '0.75rem' : 0 }}>
          <button className="btn-confirm" onClick={handleSubmit} disabled={loading}>
            {loading ? 'Reservando…' : 'Confirmar'}
          </button>
          <button className="btn-secondary" onClick={onCancel}>
            Cancelar
          </button>
        </div>
      </div>
    </div>
  )
}

function InfoRow({ label, value }) {
  return (
    <div className="info-row">
      <span className="info-label">{label}</span>
      <span className="info-value">{value}</span>
    </div>
  )
}
