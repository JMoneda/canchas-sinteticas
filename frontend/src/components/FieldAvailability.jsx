import { useState, useEffect } from 'react'
import { fetchAvailability } from '../services/api'
import ErrorMessage from './ErrorMessage'

function getDefaultDate() {
  const d = new Date()
  d.setDate(d.getDate() + 1)
  return d.toISOString().split('T')[0]
}

export default function FieldAvailability({ onSlotSelect }) {
  const [date, setDate] = useState(getDefaultDate())
  const [fields, setFields] = useState([])
  const [error, setError] = useState(null)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    if (!date) return
    setLoading(true)
    setError(null)
    fetchAvailability(date)
      .then(setFields)
      .catch((err) => setError(err))
      .finally(() => setLoading(false))
  }, [date])

  const todayStr = new Date().toISOString().split('T')[0]

  return (
    <div className="card">
      <div className="card-body">
        <div className="section-header">
          <h2 className="section-title">Disponibilidad</h2>
          <div className="date-pill">
            <span className="date-pill-label">Fecha</span>
            <input
              type="date"
              value={date}
              min={todayStr}
              onChange={(e) => setDate(e.target.value)}
            />
          </div>
        </div>

        {loading && (
          <div className="loading-row">
            <div className="spinner" />
            Cargando disponibilidad…
          </div>
        )}

        <ErrorMessage error={error} />

        {!loading && !error && (
          <div className="fields-grid">
            {fields.map((field) => (
              <FieldCard
                key={field.field_id}
                field={field}
                date={date}
                onSlotSelect={onSlotSelect}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  )
}

function FieldCard({ field, date, onSlotSelect }) {
  const count = field.available_slots.length
  return (
    <div className="field-card">
      <div className="field-card-head">
        <span className="field-name">{field.field_name}</span>
        <span className="field-count">
          {count === 0 ? 'Sin turnos' : `${count} turno${count !== 1 ? 's' : ''}`}
        </span>
      </div>
      <div className="field-card-body">
        {count === 0 ? (
          <p className="no-slots">Sin disponibilidad para este día</p>
        ) : (
          <div className="slots-grid">
            {field.available_slots.map((slot) => (
              <button
                key={slot.start_time}
                className="slot-btn"
                onClick={() => onSlotSelect(field, { ...slot, date })}
              >
                <span>{slot.start_time}</span>
                <span className="slot-end-time">{slot.end_time}</span>
              </button>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
