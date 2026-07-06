---
description: "Task list for Reservation System â€” Clean Architecture, layer-by-layer build order"
---

# Tasks: Reservation System for Synthetic Football Fields

**Input**: Design documents from `/specs/001-reservation-system/`

**Prerequisites**: plan.md âœ… Â· spec.md âœ… Â· research.md âœ… Â· data-model.md âœ… Â· contracts/ âœ…

**Build Order**: Domain â†’ Application â†’ Infrastructure â†’ API (per user story) â†’ Frontend (per user story)

**Constitution Rule**: Tests for each domain rule MUST be written and confirmed FAILING before the rule is implemented.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no cross-task dependency)
- **[Story]**: Maps task to a user story (US1â€“US4)
- Include exact file paths in every task description

---

## Phase 1: Setup

**Purpose**: Project initialization â€” no code, no tests yet.

- [x] T001 Create full directory tree per plan.md: `backend/{domain/{entities,value_objects,repositories},application/use_cases,infrastructure/{models,repositories},api/routes,tests/{unit/domain,integration}}/` and `frontend/src/{components,services}/`
- [x] T002 [P] Initialize backend Python project: create `backend/requirements.txt` (fastapi, uvicorn[standard], sqlalchemy, pytest, httpx, pytest-cov) and `backend/pyproject.toml` with `[tool.pytest.ini_options] testpaths = ["tests"]`
- [x] T003 [P] Initialize frontend: create `frontend/package.json` (react 18, react-dom, vite), `frontend/vite.config.js` (port 5173), `frontend/index.html`, and `frontend/src/App.jsx` as an empty placeholder

---

## Phase 2: Foundational â€” Domain Layer

**Purpose**: Pure domain objects, repository interfaces, and all business rules with unit tests. No database, no framework.

**âš ï¸ CRITICAL**: All phases from 3 onwards depend on this phase being complete and all unit tests passing.

- [x] T004 Create `backend/domain/exceptions.py`: define `DomainError(Exception)` base class and 9 typed subclasses â€” `OverlapError`, `DurationError`, `InvalidBlockError`, `OperatingHoursError`, `AdvanceNoticeError`, `ActiveLimitError`, `FieldNotFoundError`, `NotAuthorizedError`, `AlreadyCancelledError` â€” each with a default human-readable `message` string
- [x] T005 [P] Create `backend/domain/entities/field.py`: `Field` dataclass with `id: int` and `name: str`; add `backend/domain/entities/__init__.py`
- [x] T006 [P] Create `backend/domain/entities/reservation.py`: `Reservation` dataclass with `id: str` (UUID), `user_id: str`, `field_id: int`, `date: date`, `start_time: time`, `end_time: time`, `status: str` (literal `'active'`/`'cancelled'`), `created_at: datetime`, `cancelled_at: datetime | None`; add property `start_datetime` and `end_datetime` combining date + time
- [x] T007 [P] Create `backend/domain/repositories/field_repository.py`: abstract `FieldRepository(ABC)` with method `get_all() -> list[Field]`; add `__init__.py`
- [x] T008 [P] Create `backend/domain/repositories/reservation_repository.py`: abstract `ReservationRepository(ABC)` with methods: `save(r: Reservation) -> Reservation`, `get_by_id(id: str) -> Reservation | None`, `count_active_by_user(user_id: str, now: datetime) -> int`, `get_active_by_field_and_date(field_id: int, date: date) -> list[Reservation]`, `get_active_by_user(user_id: str, now: datetime) -> list[Reservation]`, `cancel(id: str, cancelled_at: datetime) -> None`, `add_no_show(reservation_id: str, user_id: str, cancelled_at: datetime) -> None`
- [x] T009 Create `backend/domain/value_objects/time_slot.py`: `TimeSlot` dataclass with `date: date`, `start_time: time`, `end_time: time`; implement `__post_init__` that raises `DurationError` if duration < 1 hour and `InvalidBlockError` if start or end minute not in `{0, 30}`; write unit tests in `backend/tests/unit/domain/test_time_slot.py` â€” **confirm tests FAIL before adding logic, then pass after** (covers: 30-min slot rejected, 1h slot accepted, 1.5h accepted, misaligned times rejected)
- [x] T010 Add operating hours validation to `TimeSlot.__post_init__` in `backend/domain/value_objects/time_slot.py`: raise `OperatingHoursError` if `start_time < time(6, 0)` or `end_time > time(23, 0)`; add unit tests to `backend/tests/unit/domain/test_time_slot.py` (covers: 05:30â€“06:30 rejected, 22:00â€“23:30 rejected, 06:00â€“07:00 accepted, 22:00â€“23:00 accepted)
- [x] T011 Add `is_bookable(now: datetime) -> bool` to `TimeSlot` in `backend/domain/value_objects/time_slot.py`: returns `True` if `self.start_datetime - now >= timedelta(hours=1)`; add unit tests covering exactly 60 min (accepted), 59 min (rejected), and past slots (rejected)
- [x] T012 Add `overlaps_with(other: "TimeSlot") -> bool` to `TimeSlot`: returns `True` only if `self.date == other.date` and the time ranges share any period (not merely adjacent); add unit tests covering: same-date overlap (True), same-date adjacent slots (False), same-date non-overlapping (False), different dates (False)

**Checkpoint**: Run `pytest backend/tests/unit/` â€” all tests must pass before Phase 3.

---

## Phase 3: Foundational â€” Application Layer

**Purpose**: Use cases orchestrate domain rules via injected repository fakes. No SQLite, no FastAPI.

**âš ï¸ CRITICAL**: All API and frontend phases depend on this phase being complete.

- [x] T013 [P] Create `backend/application/dtos.py`: define dataclasses `CreateReservationInput` (user_id, field_id, date, start_time, end_time), `ReservationOutput` (reservation_id, user_id, field_id, field_name, date, start_time, end_time, status), `SlotOutput` (start_time, end_time), `FieldAvailabilityOutput` (field_id, field_name, available_slots), `CancelOutput` (reservation_id, status, no_show: bool); add `backend/application/__init__.py` and `backend/application/use_cases/__init__.py`
- [x] T014 Create `backend/application/use_cases/list_available_slots.py`: `ListAvailableSlots` use case takes `FieldRepository` + `ReservationRepository`; method `execute(date: date, now: datetime) -> list[FieldAvailabilityOutput]` generates all 30-min slots from 06:00â€“23:00, removes occupied ranges (from active reservations), removes slots not bookable (is_bookable fails); write unit tests in `backend/tests/unit/use_cases/test_list_available_slots.py` using inline fake repository classes (covers: no reservations â†’ full slot list; occupied slot â†’ removed from list; slot within 1h of now â†’ removed)
- [x] T015 Create `backend/application/use_cases/create_reservation.py`: `CreateReservation` use case; `execute(input: CreateReservationInput, now: datetime) -> ReservationOutput`; steps: (1) get field or raise `FieldNotFoundError`, (2) construct `TimeSlot` (raises duration/block/hours errors), (3) check `is_bookable` or raise `AdvanceNoticeError`, (4) `count_active_by_user` â€” raise `ActiveLimitError` if â‰¥ 2, (5) `get_active_by_field_and_date` + `overlaps_with` check â€” raise `OverlapError` if any overlap, (6) save and return; write unit tests in `backend/tests/unit/use_cases/test_create_reservation.py` covering all 6 rule violations (one test per rule) + happy path
- [x] T016 Create `backend/application/use_cases/list_reservations.py`: `ListReservations` use case; `execute(user_id: str, now: datetime) -> list[ReservationOutput]` calls `get_active_by_user` (future active reservations only); write unit tests in `backend/tests/unit/use_cases/test_list_reservations.py` (covers: returns only future active, excludes past, returns empty list for unknown user)
- [x] T017 Create `backend/application/use_cases/cancel_reservation.py`: `CancelReservation` use case; `execute(reservation_id: str, user_id: str, now: datetime) -> CancelOutput`; steps: (1) get by id or raise `NotFoundError`, (2) check `r.user_id == user_id` or raise `NotAuthorizedError`, (3) check `r.status != 'cancelled'` or raise `AlreadyCancelledError`, (4) determine no_show = `r.start_datetime - now < timedelta(hours=2)`, (5) cancel, (6) if no_show add no_show record; write unit tests in `backend/tests/unit/use_cases/test_cancel_reservation.py` (covers: clean cancel, late cancel â†’ no_show=True, unauthorized, already cancelled, not found)

**Checkpoint**: Run `pytest backend/tests/unit/` â€” all use case tests must pass before Phase 4.

---

## Phase 4: Foundational â€” Infrastructure & API Bootstrap

**Purpose**: SQLite persistence + FastAPI app wiring. Enables all API user stories.

**âš ï¸ CRITICAL**: All API phases depend on this phase.

- [x] T018 Create `backend/infrastructure/database.py`: SQLAlchemy `create_engine` pointing to `backend/reservations.db` (use `check_same_thread=False`); `SessionLocal` factory; `Base = declarative_base()`; function `create_tables()` that calls `Base.metadata.create_all()`; add `backend/infrastructure/__init__.py`
- [x] T019 [P] Create `backend/infrastructure/models/field_model.py` (`FieldModel`: id, name), `backend/infrastructure/models/reservation_model.py` (`ReservationModel`: all columns from data-model.md schema), `backend/infrastructure/models/no_show_model.py` (`NoShowModel`); all inherit from `Base`; add `backend/infrastructure/models/__init__.py`
- [x] T020 [P] Create `backend/infrastructure/seed.py`: `seed_fields(session)` function that inserts `FieldModel(name="Cancha A")`, `Cancha B`, `Cancha C` only if `session.query(FieldModel).count() == 0`; idempotent
- [x] T021 Create `backend/infrastructure/repositories/sqlite_field_repository.py`: `SQLiteFieldRepository(FieldRepository)` implements `get_all()` by querying `FieldModel` and mapping each to `Field` domain entity; add `backend/infrastructure/repositories/__init__.py`
- [x] T022 Create `backend/infrastructure/repositories/sqlite_reservation_repository.py`: `SQLiteReservationRepository(ReservationRepository)` implements all abstract methods; `count_active_by_user` filters by `status='active'` AND `(date > today OR (date = today AND end_time > current_time))`; `save` uses UUID v4 for id; `cancel` sets `status='cancelled'` and `cancelled_at`; `add_no_show` inserts `NoShowModel`; include ORMâ†”domain mapper methods
- [x] T023 Create `backend/api/main.py`: FastAPI app with `lifespan` context that calls `create_tables()` then `seed_fields()`; create `backend/api/dependencies.py` with `get_db_session()` dependency and factory functions `get_field_repo()`, `get_reservation_repo()`, `get_create_reservation_uc()`, `get_cancel_reservation_uc()`, `get_list_reservations_uc()`, `get_list_slots_uc()`; add `backend/api/__init__.py` and `backend/api/routes/__init__.py`

**Checkpoint**: Start backend with `uvicorn api.main:app --reload` from `backend/` â€” server starts, tables created, 3 fields seeded, no errors.

---

## Phase 5: User Story 1 â€” View Field Availability (Priority: P1) ðŸŽ¯

**Goal**: Users can view available 30-minute time slots per field for any future date.

**Independent Test**: `GET /api/fields/availability?date=<tomorrow>` returns 3 fields with slot arrays; `GET` for a past date returns 400; UI shows slot grid.

- [x] T024 [US1] Create `backend/api/routes/fields.py`: `GET /api/fields/availability` â€” parse `date` query param, call `ListAvailableSlots.execute()`, return `FieldAvailabilityOutput` list; add `_domain_error_to_http()` helper in `backend/api/routes/fields.py` that maps `DomainError` subclasses to correct HTTP status codes and `{error_type, message}` body; add integration tests in `backend/tests/integration/test_api.py` (covers: valid future date â†’ 200 + 3 fields; past date â†’ 400)
- [x] T025 [US1] Create `frontend/src/services/api.js`: export `fetchAvailability(date)` â€” `GET /api/fields/availability?date={date}`; returns parsed JSON; throws with `{error_type, message}` on non-2xx
- [x] T026 [US1] Create `frontend/src/components/FieldAvailability.jsx`: date `<input type="date">` defaulting to tomorrow; on change calls `fetchAvailability`; renders one card per field showing field name and grid of available slot buttons (`HH:MMâ€“HH:MM`); emits `onSlotSelect(field, slot)` prop; shows "No slots available" when array is empty; renders inline error string on API failure

**Checkpoint**: Start backend + frontend; select a date; verify 3 field cards appear with available slots.

---

## Phase 6: User Story 2 â€” Create a Reservation (Priority: P2) ðŸŽ¯

**Goal**: Session user can select a slot and submit a reservation; all 6 domain rule violations produce distinct error messages.

**Independent Test**: Submit valid reservation â†’ 201 + confirmation; submit each invalid case â†’ 422 with correct `error_type`.

- [x] T027 [US2] Add `POST /api/reservations` to `backend/api/routes/reservations.py`: parse request body into `CreateReservationInput`, call `CreateReservation.execute(input, now=datetime.now())`; catch `DomainError` subclasses and return 422 with `{error_type, message}` (use error type name as string); catch `FieldNotFoundError` as 422; add integration tests in `backend/tests/integration/test_api.py` covering: valid request â†’ 201; each of the 6 rule violations â†’ 422 with correct `error_type`; register router on `api/main.py`
- [x] T028 [US2] Add `createReservation(data)` to `frontend/src/services/api.js`: `POST /api/reservations` with JSON body; returns parsed response on 201; throws `{error_type, message}` on 422
- [x] T029 [US2] Create `frontend/src/components/ErrorMessage.jsx`: receives `error` prop `{error_type, message}`; renders a styled error banner showing `message`; renders nothing when `error` is null
- [x] T030 [US2] Create `frontend/src/components/ReservationForm.jsx`: receives `field` and `slot` props pre-populated from slot selection; shows user identifier (read-only, from session); on submit calls `createReservation`; on success shows confirmation with reservation ID and clears form; on error passes `{error_type, message}` to `ErrorMessage`
- [x] T031 [US2] Create `frontend/src/components/IdentifierGate.jsx`: text input for user identifier + submit button; dispatches `{type: 'SET_USER_ID', payload: id}` to App reducer on submit; update `frontend/src/App.jsx` with `useReducer(reducer, {userId: null, view: 'gate'})` â€” reducer handles `SET_USER_ID` (sets userId, switches view to 'main'); main view renders `<FieldAvailability onSlotSelect={...} />` and conditionally renders `<ReservationForm />` when a slot is selected

**Checkpoint**: Enter identifier â†’ see availability â†’ click slot â†’ fill form â†’ submit â†’ see confirmation; submit each invalid case â†’ see specific error message.

---

## Phase 7: User Story 3 â€” View Own Reservations (Priority: P3)

**Goal**: Session user can see their upcoming active reservations.

**Independent Test**: `GET /api/reservations?user_id=maria` returns active future reservations; empty array for unknown user; UI list renders correctly.

- [x] T032 [US3] Add `GET /api/reservations` to `backend/api/routes/reservations.py`: parse `user_id` query param, call `ListReservations.execute(user_id, now=datetime.now())`; return list of `ReservationOutput`; add integration tests (covers: user with 2 reservations â†’ returns both; unknown user â†’ 200 empty array; past reservation â†’ excluded)
- [x] T033 [US3] Add `fetchReservations(userId)` to `frontend/src/services/api.js`: `GET /api/reservations?user_id={userId}`; returns parsed JSON array
- [x] T034 [US3] Create `frontend/src/components/ReservationList.jsx`: calls `fetchReservations(userId)` on mount; renders each reservation as a card (field name, date, startâ€“end time); shows "No upcoming reservations" empty state when array is empty; integrate into `App.jsx` main view alongside `FieldAvailability`

**Checkpoint**: Create a reservation â†’ navigate to list â†’ verify it appears; check that past or cancelled reservations are absent.

---

## Phase 8: User Story 4 â€” Cancel a Reservation (Priority: P4)

**Goal**: Session user can cancel one of their active reservations; late cancellation shows no-show notice.

**Independent Test**: `DELETE /api/reservations/{id}` with correct user â†’ 200 + `{no_show: bool}`; wrong user â†’ 403; already cancelled â†’ 400.

- [x] T035 [US4] Add `DELETE /api/reservations/{reservation_id}` to `backend/api/routes/reservations.py`: parse body `{user_id}`, call `CancelReservation.execute(reservation_id, user_id, now=datetime.now())`; return `CancelOutput`; map `NotAuthorizedError` â†’ 403, `NotFoundError` â†’ 404, `AlreadyCancelledError` â†’ 400, all with `{error_type, message}` body; add integration tests (covers: clean cancel, late cancel â†’ no_show=true, unauthorized â†’ 403, already cancelled â†’ 400)
- [x] T036 [US4] Add `cancelReservation(reservationId, userId)` to `frontend/src/services/api.js`: `DELETE /api/reservations/{reservationId}` with JSON body `{user_id: userId}`; returns parsed response; throws `{error_type, message}` on error
- [x] T037 [US4] Add cancel button to each reservation card in `frontend/src/components/ReservationList.jsx`: on click calls `cancelReservation`; on success removes reservation from local list and shows no-show banner if `no_show === true`; on error renders `ErrorMessage` (403: "not authorized", 400: "already cancelled"); refresh list after successful cancellation

**Checkpoint**: All 4 user stories independently functional. Run quickstart.md scenarios 1â€“12.

---

## Phase 9: Polish & Cross-Cutting Concerns

- [x] T038 [P] Configure CORS in `backend/api/main.py`: add `CORSMiddleware` allowing `http://localhost:5173`; run full backend test suite `pytest backend/tests/ -v --cov=backend --cov-report=term-missing` â€” all tests must pass
- [x] T039 [P] Run quickstart.md validation scenarios 1â€“12 end-to-end with both servers running; confirm all checkboxes pass
- [x] T040 Constitution compliance review: verify (a) no business logic in `api/routes/` or any `frontend/src/` file, (b) no `import` from `infrastructure/` inside `domain/` or `application/`, (c) all 6 domain rules have unit tests, (d) `TimeSlot` is the only place validation logic lives

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies â€” start immediately
- **Domain Layer (Phase 2)**: Depends on Setup â€” **BLOCKS all subsequent phases**
- **Application Layer (Phase 3)**: Depends on Domain Layer â€” BLOCKS API and Frontend
- **Infrastructure & API Bootstrap (Phase 4)**: Depends on Application Layer â€” BLOCKS all API phases
- **US1 (Phase 5)**: Depends on Phase 4 completion
- **US2 (Phase 6)**: Depends on Phase 5 (FieldAvailability renders slots that ReservationForm reads)
- **US3 (Phase 7)**: Depends on Phase 4; can start after Phase 4 independently of US2
- **US4 (Phase 8)**: Depends on Phase 7 (cancel button lives in ReservationList)
- **Polish (Phase 9)**: Depends on all user story phases

### Parallel Opportunities Within Phases

```bash
# Phase 2 â€” run in parallel (different files):
T005  # field.py
T006  # reservation.py
T007  # field_repository.py
T008  # reservation_repository.py

# Phase 4 â€” run in parallel after T018:
T019  # ORM models
T020  # seed.py

# Phase 9 â€” run in parallel:
T038  # CORS + test suite
T039  # quickstart validation
```

---

## Implementation Strategy

### MVP First (Domain + US1 + US2 only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Domain Layer + all unit tests green
3. Complete Phase 3: Application Layer + all unit tests green
4. Complete Phase 4: Infrastructure + API Bootstrap
5. Complete Phase 5: US1 (View Availability)
6. Complete Phase 6: US2 (Create Reservation)
7. **STOP and VALIDATE**: Full booking flow works end-to-end (quickstart scenarios 1â€“7)
8. Deploy / demo if ready

### Incremental Delivery

1. Phases 1â€“4 complete â†’ Domain + Application + Infrastructure ready
2. Phase 5 (US1) â†’ Availability view live, test independently
3. Phase 6 (US2) â†’ Booking flow live, test independently
4. Phase 7 (US3) â†’ Reservation list live, test independently
5. Phase 8 (US4) â†’ Cancel flow live, test independently
6. Phase 9 â†’ Polish + full regression pass

---

## Notes

- `[P]` = different files, no dependency on a sibling task in the same phase
- `[US?]` = maps to user story for traceability
- Tests for domain rules are written FIRST and must FAIL before the rule logic is implemented (TDD Redâ†’Green)
- Each phase ends with a runnable checkpoint â€” stop and verify before advancing
- No business logic in `api/routes/` â€” only HTTP in/out and `DomainError` mapping
- No SQLAlchemy or infrastructure imports inside `domain/` or `application/`

