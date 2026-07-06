# Quickstart: Reservation System Validation Guide

Use this guide to validate the system works end-to-end after implementation.
It covers prerequisites, startup, and scenario-by-scenario verification.

---

## Prerequisites

- Python 3.11+ installed
- Node.js 20+ installed
- Repository root: `c:\Users\jmgonzaleh\canchas-sinteticas\`

---

## Backend Setup & Start

```bash
cd backend
python -m venv .venv
.venv\Scripts\activate          # Windows PowerShell
pip install -r requirements.txt
uvicorn api.main:app --reload --port 8000
```

On startup the backend will:
1. Create `reservations.db` (SQLite file) if it does not exist.
2. Run table creation.
3. Seed 3 fields: **Cancha A**, **Cancha B**, **Cancha C** (idempotent).

Verify startup: `GET http://localhost:8000/api/fields/availability?date=2026-07-01`
should return 3 fields with slots, HTTP 200.

---

## Run Backend Tests

```bash
cd backend
pytest tests/ -v --cov=. --cov-report=term-missing
```

All tests must pass before proceeding to frontend validation.
Domain unit tests MUST run without any database connection.

---

## Frontend Setup & Start

```bash
cd frontend
npm install
npm run dev
```

Open `http://localhost:5173` in a browser.

---

## Scenario Validation

### Scenario 1 — Identifier Entry (US1 prerequisite)

1. Open `http://localhost:5173`.
2. The app shows an identifier entry screen.
3. Enter `"maria"` and confirm.
4. The main view opens. The identifier is not requested again during the session.

**Expected**: The identifier gate is shown once; the main view is accessible after entry.

---

### Scenario 2 — View Field Availability (User Story 1)

1. On the main view, select today's date (or tomorrow).
2. The system displays Cancha A, Cancha B, Cancha C with their available 30-min slots.

**Expected**: Each field shows slots from 06:00 to 23:00 (minus any already booked,
minus slots within 1 hour of now).

API equivalent:
```
GET /api/fields/availability?date=<today>
→ 200, 3 fields each with available_slots array
```

---

### Scenario 3 — Create a Valid Reservation (User Story 2)

1. Select **Cancha A**, a date at least 2 hours in the future, 10:00–12:00.
2. Submit the reservation form.
3. Confirmation is shown with a reservation ID.

**Expected**: HTTP 201, reservation appears in "My Reservations" list.

API equivalent:
```
POST /api/reservations
Body: {"user_id":"maria","field_id":1,"date":"<future>","start_time":"10:00","end_time":"12:00"}
→ 201, body contains reservation_id and status: "active"
```

---

### Scenario 4 — Domain Rule: Overlap (User Story 2, error path)

1. Attempt to reserve **Cancha A** 10:00–11:00 on the same date as Scenario 3.

**Expected**: Error message "This time slot overlaps with an existing reservation."
API → 422, `error_type: "OVERLAP"`.

---

### Scenario 5 — Domain Rule: Advance Notice (User Story 2, error path)

1. Try to reserve any field for a time slot that starts in less than 60 minutes.

**Expected**: Error message about 1-hour advance notice requirement.
API → 422, `error_type: "ADVANCE_NOTICE"`.

---

### Scenario 6 — Domain Rule: Duration / Block Alignment (User Story 2, error path)

1. Try to reserve 10:00–10:30 (only 30 minutes, below the 1-hour minimum).

**Expected**: Error message about minimum duration.
API → 422, `error_type: "DURATION_INVALID"`.

---

### Scenario 7 — Domain Rule: Active Reservation Limit (User Story 2, error path)

1. As `"maria"`, create a second valid reservation on a different field or time.
2. Attempt to create a third reservation.

**Expected**: Error message about the 2-reservation active limit.
API → 422, `error_type: "ACTIVE_LIMIT"`.

---

### Scenario 8 — View My Reservations (User Story 3)

1. After creating reservations in Scenario 3, navigate to "My Reservations".

**Expected**: List shows active future reservations for `"maria"`.
API → `GET /api/reservations?user_id=maria` → 200, array with reservation items.

---

### Scenario 9 — Cancel with Sufficient Notice (User Story 4)

1. Cancel the reservation from Scenario 3 (reservation is hours in the future).

**Expected**: Status changes to `cancelled`. `no_show: false`.
API → `DELETE /api/reservations/<id>` with `{"user_id":"maria"}` → 200.

---

### Scenario 10 — Cancel with Late Notice / No-Show (User Story 4)

Requires a reservation with start time less than 2 hours away.

1. Create a reservation starting ~90 minutes from now.
2. Immediately cancel it.

**Expected**: Status `cancelled`, `no_show: true`.
API → 200, `{"no_show": true}`.

---

### Scenario 11 — Unauthorized Cancellation (User Story 4, error path)

1. As `"pedro"`, attempt to cancel a reservation that belongs to `"maria"`.

**Expected**: 403, `error_type: "NOT_AUTHORIZED"`.

---

### Scenario 12 — Empty State (User Story 3, edge case)

1. Use a user identifier that has never made a reservation (e.g., `"newuser"`).
2. View "My Reservations".

**Expected**: Empty list displayed with a clear message. No error.
API → `GET /api/reservations?user_id=newuser` → 200, `[]`.

---

## Completion Criteria

All 12 scenarios must pass before the feature is considered done:

- [ ] Scenario 1: Identifier gate works
- [ ] Scenario 2: Availability view returns correct slots
- [ ] Scenario 3: Valid reservation created
- [ ] Scenario 4: Overlap rejected
- [ ] Scenario 5: Advance notice rejected
- [ ] Scenario 6: Duration/block rejected
- [ ] Scenario 7: Active limit rejected
- [ ] Scenario 8: My reservations list works
- [ ] Scenario 9: Clean cancellation works
- [ ] Scenario 10: Late cancellation creates no-show
- [ ] Scenario 11: Unauthorized cancellation rejected
- [ ] Scenario 12: Empty state handled
