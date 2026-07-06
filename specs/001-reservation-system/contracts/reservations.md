# Contract: Reservations API

Base path: `/api/reservations`

---

## POST /api/reservations

Creates a new reservation. All domain rules are enforced; a specific error is returned
for each violation.

### Request Body

```json
{
  "user_id":    "string (required, non-empty)",
  "field_id":   1,
  "date":       "YYYY-MM-DD",
  "start_time": "HH:MM",
  "end_time":   "HH:MM"
}
```

### Responses

**201 Created** — Reservation confirmed.

```json
{
  "reservation_id": "550e8400-e29b-41d4-a716-446655440000",
  "user_id":        "maria",
  "field_id":       1,
  "field_name":     "Cancha A",
  "date":           "2026-07-01",
  "start_time":     "10:00",
  "end_time":       "12:00",
  "status":         "active"
}
```

**422 Unprocessable Entity** — Domain rule violated.

```json
{
  "error_type": "<ERROR_TYPE>",
  "message":    "Human-readable explanation of the rule violation."
}
```

| `error_type` | Triggered when |
|--------------|----------------|
| `OVERLAP` | The requested time range overlaps with an existing active reservation on the same field |
| `DURATION_INVALID` | The duration is less than 1 hour |
| `INVALID_BLOCK` | Start or end time does not align to a 30-minute boundary |
| `OPERATING_HOURS` | Start time is before 06:00 or end time is after 23:00 |
| `ADVANCE_NOTICE` | Start time is less than 1 hour from the current moment |
| `ACTIVE_LIMIT` | The user already holds 2 active (future, non-cancelled) reservations |
| `FIELD_NOT_FOUND` | The provided `field_id` does not exist |

---

## GET /api/reservations

Returns all upcoming active reservations for the session user. Only reservations
whose end time is in the future and that have not been cancelled are returned.

### Request

| Parameter | Location | Type   | Required | Description |
|-----------|----------|--------|----------|-------------|
| `user_id` | query    | string | Yes      | The session user's identifier |

### Responses

**200 OK** — List returned (may be empty).

```json
[
  {
    "reservation_id": "550e8400-e29b-41d4-a716-446655440000",
    "field_name":     "Cancha A",
    "date":           "2026-07-01",
    "start_time":     "10:00",
    "end_time":       "12:00",
    "status":         "active"
  }
]
```

- Returns `[]` if the user has no upcoming active reservations (not an error).
- Completed (past) and cancelled reservations are excluded.

---

## DELETE /api/reservations/{reservation_id}

Cancels an active reservation. If the cancellation is submitted with less than
2 hours of advance notice before the reservation start time, a no-show is also recorded.

### Request

| Parameter        | Location | Type   | Required | Description |
|------------------|----------|--------|----------|-------------|
| `reservation_id` | path     | string | Yes      | UUID of the reservation |

**Request body**:

```json
{
  "user_id": "string (required)"
}
```

### Responses

**200 OK** — Reservation cancelled.

```json
{
  "reservation_id": "550e8400-e29b-41d4-a716-446655440000",
  "status":         "cancelled",
  "no_show":        false
}
```

- `no_show: true` when the cancellation was recorded with less than 2 hours of
  advance notice.

**403 Forbidden** — Reservation does not belong to the requesting user.

```json
{
  "error_type": "NOT_AUTHORIZED",
  "message":    "This reservation does not belong to the provided user identifier."
}
```

**404 Not Found** — Reservation does not exist.

```json
{
  "error_type": "NOT_FOUND",
  "message":    "No reservation found with the provided identifier."
}
```

**400 Bad Request** — Reservation is already cancelled.

```json
{
  "error_type": "ALREADY_CANCELLED",
  "message":    "This reservation has already been cancelled."
}
```

---

## Common Conventions

- All times use 24-hour format: `"HH:MM"` (e.g., `"06:00"`, `"22:30"`).
- All dates use ISO-8601: `"YYYY-MM-DD"`.
- `reservation_id` is a UUID v4 string.
- The backend operates in a single fixed timezone (local server time). No timezone
  conversion is performed.
