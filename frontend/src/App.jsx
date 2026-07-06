import { useReducer, useState } from 'react'
import FieldAvailability from './components/FieldAvailability'
import IdentifierGate from './components/IdentifierGate'
import ReservationForm from './components/ReservationForm'
import ReservationList from './components/ReservationList'

const initialState = { userId: null, selectedField: null, selectedSlot: null }

function reducer(state, action) {
  switch (action.type) {
    case 'SET_USER':   return { ...state, userId: action.payload }
    case 'SELECT_SLOT': return { ...state, selectedField: action.payload.field, selectedSlot: action.payload.slot }
    case 'CLEAR_SLOT': return { ...state, selectedField: null, selectedSlot: null }
    default:           return state
  }
}

export default function App() {
  const [state, dispatch] = useReducer(reducer, initialState)
  const [refreshKey, setRefreshKey] = useState(0)

  if (!state.userId) {
    return <IdentifierGate onSubmit={(id) => dispatch({ type: 'SET_USER', payload: id })} />
  }

  const initials = state.userId.slice(0, 2).toUpperCase()

  return (
    <div className="app-root">
      <header className="app-header">
        <div className="app-logo">
          <div className="app-logo-icon">⚽</div>
          Canchas Sintéticas
        </div>
        <div className="user-pill">
          <div className="user-avatar">{initials}</div>
          {state.userId}
        </div>
      </header>

      <main className="app-main">
        <FieldAvailability
          onSlotSelect={(field, slot) =>
            dispatch({ type: 'SELECT_SLOT', payload: { field, slot } })
          }
        />

        {state.selectedField && state.selectedSlot && (
          <ReservationForm
            userId={state.userId}
            field={state.selectedField}
            slot={state.selectedSlot}
            onSuccess={() => {
              dispatch({ type: 'CLEAR_SLOT' })
              setRefreshKey((k) => k + 1)
            }}
            onCancel={() => dispatch({ type: 'CLEAR_SLOT' })}
          />
        )}

        <ReservationList
          key={refreshKey}
          userId={state.userId}
          onCancelled={() => setRefreshKey((k) => k + 1)}
        />
      </main>
    </div>
  )
}
