import { useState } from 'react'

export default function IdentifierGate({ onSubmit }) {
  const [identifier, setIdentifier] = useState('')

  function handleSubmit(e) {
    e.preventDefault()
    const trimmed = identifier.trim()
    if (trimmed) onSubmit(trimmed)
  }

  return (
    <div className="gate-page">
      <div className="gate-card">
        <div className="gate-brand">
          <div className="gate-logo-box">⚽</div>
        </div>
        <h1 className="gate-title">Canchas Sintéticas</h1>
        <p className="gate-subtitle">Reservá tu turno en segundos</p>

        <form onSubmit={handleSubmit} className="gate-form">
          <div>
            <label className="form-label" htmlFor="identifier">
              Tu nombre o identificador
            </label>
            <input
              id="identifier"
              type="text"
              className="form-input"
              value={identifier}
              onChange={(e) => setIdentifier(e.target.value)}
              placeholder="Ej: maria, juan123"
              autoFocus
              autoComplete="off"
            />
          </div>
          <button
            type="submit"
            disabled={!identifier.trim()}
            className="btn-primary btn-primary-full"
            style={{ marginTop: '0.25rem' }}
          >
            Entrar
          </button>
        </form>
      </div>
    </div>
  )
}
