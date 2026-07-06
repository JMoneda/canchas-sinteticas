# Data Model: Reservation System for Synthetic Football Fields

**Phase 1 output** — entities, value objects, state transitions, and persistence schema.

---

## Domain Entities

### Field

Represents a synthetic football field available for reservation.

| Attribute | Type   | Constraints |
|-----------|--------|-------------|
| id        | int    | Primary key, auto-increment |
| name      | string | Non-empty, unique |

Fields are pre-seeded at startup. No lifecycle transitions — fields are permanent
within the MVP.

---

### Reservation

Represents a booking of a specific field by a user for a defined time range.

| Attribute    | Type     | Constraints |
|--------------|----------|-------------|
| id           | UUID     | Primary key, generated on creation |
| user_id      | string   | Non-empty; self-provided by user |
| field_id     | int      | Foreign key → Field.id |
| date         | date     | The calendar date of the reservation |
| start_time   | time     | Aligns to 30-min boundary; ≥ 06:00 |
| end_time     | time     | Aligns to 30-min boundary; ≤ 23:00; > start_time + 1h |
| status       | enum     | `active` · `cancelled` — stored in DB |
| created_at   | datetime | Set at creation, UTC |
| cancelled_at | datetime | Nullable; set when cancelled |

**Computed status — `completed`**: A reservation with `status = active` whose
`date + end_time` is in the past is treated as `completed` by the application.
This is NOT stored in the database; it is computed at query time.

**Status transitions**:

```
created ──► active ──► cancelled   (explicit user action; may trigger no-show)
                  └──► [completed]  (computed — end_datetime < now())
```

**Active definition** (used for the 2-reservation limit):
A reservation is active if `status = 'active'` AND `(date, end_time)` is in the future.
Cancelled reservations and past reservations never count toward the limit.

---

### NoShow

Records that a user cancelled a reservation with less than 2 hours of advance notice.

| Attribute       | Type     | Constraints |
|-----------------|----------|-------------|
| id              | int      | Primary key, auto-increment |
| reservation_id  | UUID     | Foreign key → Reservation.id |
| user_id         | string   | Copied from the cancelled reservation |
| cancelled_at    | datetime | Timestamp of the cancellation action, UTC |

A NoShow record is created alongside the cancellation when:
`reservation.start_datetime - cancellation_time < 2 hours`

No automated consequence is enforced in the MVP (no bans or penalties).

---

## Value Object: TimeSlot

Encapsulates the date + time range of a reservation and enforces all booking
constraints as pure, framework-agnostic logic.

| Attribute  | Type | Description |
|------------|------|-------------|
| date       | date | Calendar date |
| start_time | time | Start of the booking window |
| end_time   | time | End of the booking window |

**Validation rules** (all enforced on construction):

| Rule | Condition |
|------|-----------|
| Minimum duration | `end_time - start_time >= 1 hour` |
| 30-minute alignment | `start_time.minute in {0, 30}` AND `end_time.minute in {0, 30}` |
| Operating hours — start | `start_time >= 06:00` |
| Operating hours — end | `end_time <= 23:00` |
| No midnight crossing | `end_time > start_time` (same calendar day) |

**Behaviors**:

| Method | Returns | Description |
|--------|---------|-------------|
| `is_bookable(now: datetime)` | bool | `True` if `date + start_time - now >= 1 hour` |
| `overlaps_with(other: TimeSlot)` | bool | `True` if the two slots share any time on the same date and field |

---

## Persistence Schema (SQLite)

The ORM models in `infrastructure/models/` map to these tables. They are separate
Python classes from the domain entities — no shared base class or inheritance.

```sql
CREATE TABLE fields (
    id   INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT    NOT NULL UNIQUE
);

CREATE TABLE reservations (
    id           TEXT     PRIMARY KEY,          -- UUID as text
    user_id      TEXT     NOT NULL,
    field_id     INTEGER  NOT NULL REFERENCES fields(id),
    date         TEXT     NOT NULL,             -- ISO-8601 date: YYYY-MM-DD
    start_time   TEXT     NOT NULL,             -- HH:MM (24h)
    end_time     TEXT     NOT NULL,             -- HH:MM (24h)
    status       TEXT     NOT NULL DEFAULT 'active',  -- 'active' | 'cancelled'
    created_at   TEXT     NOT NULL,             -- ISO-8601 datetime UTC
    cancelled_at TEXT     NULL                  -- ISO-8601 datetime UTC
);

CREATE TABLE no_shows (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    reservation_id  TEXT    NOT NULL REFERENCES reservations(id),
    user_id         TEXT    NOT NULL,
    cancelled_at    TEXT    NOT NULL             -- ISO-8601 datetime UTC
);
```

**Index for overlap checking** (created at startup):
```sql
CREATE INDEX idx_reservations_field_date
    ON reservations(field_id, date, status);
```

**Query for active reservation count** (used by max-2-limit rule):
```sql
SELECT COUNT(*) FROM reservations
WHERE user_id = :user_id
  AND status  = 'active'
  AND (date > :today
       OR (date = :today AND end_time > :current_time));
```

---

## Mapper: Domain Entity ↔ ORM Model

Each repository implementation maps between ORM models and domain entities. The domain
never imports SQLAlchemy. The infrastructure never exposes SQLAlchemy models outside
its own package.

| Domain Entity | ORM Model | Mapper Location |
|---------------|-----------|-----------------|
| `Field` | `FieldModel` | `sqlite_field_repository.py` |
| `Reservation` | `ReservationModel` | `sqlite_reservation_repository.py` |
| `NoShow` | `NoShowModel` | `sqlite_reservation_repository.py` |
