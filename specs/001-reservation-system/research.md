# Research: Reservation System for Synthetic Football Fields

**Phase 0 output** — all technical decisions resolved before design begins.

---

## Decision 1: Architecture Pattern

**Decision**: Clean Architecture with four explicit layers (domain, application,
infrastructure, API).

**Rationale**: Mandated by Constitution Principles I and II. Keeps business rules
testable in isolation, decouples persistence from logic, and allows the API layer to
be replaced without touching the domain.

**Alternatives considered**: Layered MVC (rejected — business logic leaks into
controllers); Active Record (rejected — couples domain to persistence, violates
inward dependency rule).

---

## Decision 2: "Completed" State Implementation

**Decision**: Reservations are stored as `active` in the database. At query time,
any reservation whose `end_datetime` (date + end_time) is in the past is treated as
`completed`. No background job or cron task is used. The repository's
"count active for user" query filters by `status = 'active' AND end_datetime > now()`.

**Rationale**: SQLite has no native scheduler. Adding a background task (APScheduler,
Celery) would violate Constitution Principle III (no message queues, simplicity first).
Computing completion at query time is correct, requires no extra infrastructure, and is
fully testable.

**Alternatives considered**: Storing `completed` as an explicit DB status updated by a
cron job (rejected — over-engineering for MVP, requires scheduler dependency); SQLAlchemy
event hooks to auto-update status (rejected — magic behavior, hard to test, hides logic).

---

## Decision 3: Concurrency / Double-Booking Prevention

**Decision**: SQLite opened in WAL mode with IMMEDIATE transactions for write
operations. The `create_reservation` use case acquires a transaction, checks for
overlaps via a SELECT query, and inserts only if none exist. SQLite IMMEDIATE mode
serializes concurrent writes, preventing two simultaneous bookings from both succeeding.

**Rationale**: SQLite WAL mode with IMMEDIATE transactions is the simplest reliable
solution. At MVP scale (handful of users), SQLite write serialization is sufficient.

**Alternatives considered**: Optimistic locking with version columns (rejected —
complexity not justified at MVP scale); application-level in-memory locks (rejected —
doesn't work across processes, fragile).

---

## Decision 4: Repository Pattern Implementation

**Decision**: Abstract repository interfaces (`FieldRepository`, `ReservationRepository`)
defined in `domain/repositories/` as Python ABCs. Concrete SQLAlchemy implementations
live in `infrastructure/repositories/`. Use cases receive repositories via constructor
injection (dependency inversion). Unit tests inject in-memory fakes; integration tests
use real SQLite.

**Rationale**: Enables pure domain unit tests with no DB dependency. Enforces the
inward dependency rule — domain never imports from infrastructure. Matches the port/
adapter pattern of Clean Architecture.

**Alternatives considered**: Direct SQLAlchemy queries in use cases (rejected —
couples application layer to infrastructure, violates Constitution Principle II);
Django ORM / Active Record (rejected — no Django in this stack).

---

## Decision 5: Domain Exceptions Strategy

**Decision**: A base `DomainError` exception in `domain/exceptions.py` with typed
subclasses per rule violation (`OverlapError`, `OperatingHoursError`, `AdvanceNoticeError`,
`DurationError`, `ActiveLimitError`, `FieldNotFoundError`, `NotAuthorizedError`,
`AlreadyCancelledError`). The API layer catches `DomainError` subclasses and maps them
to appropriate HTTP status codes (422 Unprocessable Entity for business rule violations,
404/403 for resource/auth errors).

**Rationale**: Typed exceptions make it impossible to accidentally swallow a specific
rule violation. The API layer's mapping is explicit and testable. Each error type
carries a human-readable `message` that FR-011 requires.

**Alternatives considered**: Returning result objects / discriminated unions (rejected
— more complex than exceptions for this scale); generic `ValueError` (rejected — loses
type information, harder to map to HTTP codes).

---

## Decision 6: Frontend State Architecture

**Decision**: `App.jsx` holds a single `useReducer` store with shape
`{ userId: string | null, view: 'gate' | 'main' }`. Each child component manages its
own local state with `useState` (e.g., selected date in `FieldAvailability`, form
fields in `ReservationForm`). `ReservationList` fetches on mount and after cancellation.
All HTTP calls are centralized in `services/api.js`.

**Rationale**: Matches constitution constraint (useState/useReducer only). `useReducer`
at App level for session state is a well-established React pattern for global-ish state
without Redux. Local `useState` for ephemeral UI state keeps components self-contained.

**Alternatives considered**: React Context for userId propagation (rejected — adds
abstraction not needed at this scale; prop drilling at 2 levels is fine); Redux Toolkit
(prohibited by constitution).

---

## Decision 7: API Error Response Shape

**Decision**: All domain rule violations return HTTP 422 with body:
```json
{
  "error_type": "OVERLAP | DURATION_INVALID | OPERATING_HOURS | ADVANCE_NOTICE | ACTIVE_LIMIT | FIELD_NOT_FOUND | NOT_AUTHORIZED | ALREADY_CANCELLED | INVALID_BLOCK",
  "message": "Human-readable explanation of the violation"
}
```
Resource errors (not found, unauthorized) return 404/403 with the same shape.
FastAPI validation errors (malformed request) return 422 with FastAPI's default shape.

**Rationale**: A machine-readable `error_type` lets the frontend display targeted
messages per rule (FR-011). Consistent shape across all error responses simplifies
frontend error handling.

**Alternatives considered**: Single `detail` string (rejected — frontend cannot
distinguish overlap from advance notice without parsing text); HTTP 400 for all domain
errors (rejected — 422 is semantically correct for validation failures, 400 for
malformed requests).

---

## Decision 8: Testing Strategy

**Decision**:
- **Unit tests** (`tests/unit/domain/`): Test `TimeSlot` and domain entities with
  no database. Inject fake/stub repositories. Cover all 6 domain rules, boundary
  conditions, and the `completed` state transition logic.
- **Integration tests** (`tests/integration/test_api.py`): Use FastAPI's `TestClient`
  (backed by httpx) with an in-memory SQLite database. Test each endpoint's happy path
  and primary error paths. Do not duplicate unit-level boundary testing.

**Rationale**: Unit tests are fast and isolated — ideal for the exhaustive domain
rule coverage TDD requires. Integration tests verify the wiring (FastAPI → use cases →
repository → SQLite) without duplicating domain logic tests.

**Alternatives considered**: Only integration tests (rejected — too slow for TDD
cycle, DB setup makes boundary testing verbose); mocking SQLAlchemy in unit tests
(rejected — we use fake repositories instead, which is cleaner and DB-agnostic).

---

## Decision 9: Field Seeding

**Decision**: `infrastructure/seed.py` contains a `seed_fields()` function called
during FastAPI's `lifespan` startup event. Seeds 3 fields ("Cancha A", "Cancha B",
"Cancha C") if the fields table is empty. Idempotent — safe to call on every restart.

**Rationale**: Admin panel is out of scope. Fields must exist before reservations can
be made. Seed on startup is the simplest approach with no migration tooling overhead.

**Alternatives considered**: Migration scripts (rejected — over-engineering for 3 static
records); fixtures in tests (still needed, but separately from production seed).
