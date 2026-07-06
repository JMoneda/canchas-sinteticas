const API_BASE = 'http://localhost:8080/api'

async function handleResponse(res) {
  const data = await res.json()
  if (!res.ok) throw data
  return data
}

export async function fetchAvailability(date) {
  const res = await fetch(`${API_BASE}/fields/availability?date=${date}`)
  return handleResponse(res)
}

export async function createReservation(data) {
  const res = await fetch(`${API_BASE}/reservations`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  })
  return handleResponse(res)
}

export async function fetchReservations(userId) {
  const res = await fetch(`${API_BASE}/reservations?user_id=${encodeURIComponent(userId)}`)
  return handleResponse(res)
}

export async function cancelReservation(reservationId, userId) {
  const res = await fetch(`${API_BASE}/reservations/${reservationId}`, {
    method: 'DELETE',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ user_id: userId }),
  })
  return handleResponse(res)
}
