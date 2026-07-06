# Feature Specification: Reservation System for Synthetic Football Fields

**Feature Branch**: `001-reservation-system`

**Created**: 2026-06-25

**Status**: Draft

**Input**: User description: "Build a reservation system for synthetic football fields. The system allows users to view available football fields and their time slots, create a reservation for a specific field, date, and time range, view their own existing reservations, cancel a reservation, and see clear validation errors when a reservation request violates a business rule."

## Clarifications

### Session 2026-06-25

- Q: What is the status of a reservation after its time slot has passed — does it count toward the 2-active-reservation limit? → A: Reservations automatically transition to `completed` once their end time passes; completed reservations do NOT count toward the 2-reservation limit.
- Q: How does the user identifier work in the UI — entered once per session or per action? → A: Entered once when the user opens the app; the interface retains it for all subsequent actions during the session.
- Q: Should the "my reservations" view show only upcoming active reservations, or also completed/cancelled history? → A: Only upcoming active reservations — history is out of scope for the MVP.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View Field Availability (Priority: P1)

A user wants to know which football fields are available and when they can be booked
on a given date, so they can choose a slot that fits their schedule before committing
to a reservation.

**Why this priority**: Without knowing availability, no booking decision can be made.
This is the entry point of the entire user journey and a prerequisite for every other
story. An independently delivered availability view already delivers value as a
read-only scheduling reference.

**Independent Test**: Can be tested by querying availability for a date with pre-seeded
fields and reservations, and verifying that occupied and available slots are correctly
distinguished. Delivers standalone value as a scheduling reference.

**Acceptance Scenarios**:

1. **Given** a date is selected, **When** the user requests available time slots for all fields, **Then** each field shows its open 30-minute blocks within operating hours (6:00 AM – 11:00 PM), excluding already-reserved ranges.
2. **Given** a field is fully booked for a date, **When** the user views availability for that date, **Then** the field shows no available slots for that day.
3. **Given** a date in the past is requested, **When** the user views availability, **Then** no bookable slots are shown (past slots cannot be booked).

---

### User Story 2 - Create a Reservation (Priority: P2)

A user identifies themselves by a user identifier, selects a field, a date, a start
time, and an end time, and submits a reservation request. The system either confirms
the booking or returns a specific error explaining which rule was violated.

**Why this priority**: Creating a reservation is the core value of the system. All
domain rules are exercised in this story. A working booking flow alone constitutes a
functional MVP.

**Independent Test**: Can be tested end-to-end by submitting a valid reservation
request and verifying it is persisted and returned in subsequent availability queries.
Error paths can be tested by submitting requests that violate each rule independently.

**Acceptance Scenarios**:

1. **Given** a user provides a valid user identifier, field, date, start time, and end time that satisfy all domain rules, **When** the reservation is submitted, **Then** the reservation is confirmed and assigned a unique identifier.
2. **Given** the requested time range overlaps with an existing reservation for the same field, **When** the reservation is submitted, **Then** the system rejects it with an error indicating the slot is not available.
3. **Given** the requested duration is less than 1 hour or not aligned to 30-minute blocks, **When** the reservation is submitted, **Then** the system rejects it with an error describing the duration constraint.
4. **Given** the start or end time falls outside 6:00 AM – 11:00 PM, **When** the reservation is submitted, **Then** the system rejects it with an error indicating the operating hours constraint.
5. **Given** the reservation start time is less than 1 hour from the current time, **When** the reservation is submitted, **Then** the system rejects it with an advance notice error.
6. **Given** the requesting user already has 2 active reservations, **When** a new reservation is submitted, **Then** the system rejects it with an error stating the active reservation limit has been reached.

---

### User Story 3 - View Own Reservations (Priority: P3)

A user views a list of their upcoming active reservations (future, non-cancelled),
showing the field name, date, time range for each. Completed and cancelled reservations
are not shown — reservation history is out of scope for the MVP.

**Why this priority**: Users need visibility into their existing bookings to avoid
duplicate requests and to decide which reservations to keep or cancel. Depends on
Story 2 for meaningful data but is independently testable with seeded data.

**Independent Test**: Can be tested by seeding reservations for a specific user
identifier and verifying the correct list is returned when queried.

**Acceptance Scenarios**:

1. **Given** a user has two active reservations, **When** they view their reservations, **Then** both reservations appear with field name, date, start time, end time, and status.
2. **Given** a user has no active reservations, **When** they view their reservations, **Then** an empty state is displayed with a clear message.
3. **Given** a user has previously cancelled a reservation, **When** they view their reservations, **Then** cancelled and completed reservations do not appear in the active list.

---

### User Story 4 - Cancel a Reservation (Priority: P4)

A user selects one of their active reservations and requests cancellation. The system
cancels the reservation and, if the cancellation is made with less than 2 hours of
advance notice, additionally records a no-show against the user.

**Why this priority**: Cancellation completes the reservation lifecycle and is essential
for field availability management. Depends on Stories 2 and 3 for a complete user
journey, but is testable with seeded data.

**Independent Test**: Can be tested by seeding an active reservation and submitting a
cancellation request, then verifying the reservation status changes and, when
applicable, a no-show record is created.

**Acceptance Scenarios**:

1. **Given** a user has an active reservation and cancels it with more than 2 hours of advance notice, **When** the cancellation is confirmed, **Then** the reservation status changes to cancelled and no no-show is recorded.
2. **Given** a user cancels a reservation with less than 2 hours before the reservation start time, **When** the cancellation is confirmed, **Then** the reservation status changes to cancelled AND a no-show record is created.
3. **Given** a user attempts to cancel a reservation that does not belong to them, **When** the cancellation is submitted, **Then** the system rejects it with an appropriate error.
4. **Given** a user attempts to cancel a reservation that is already cancelled, **When** the cancellation is submitted, **Then** the system rejects it with an appropriate error.

---

### Edge Cases

- What happens when two users try to book the same field and time slot simultaneously? The system must ensure only one succeeds; the other receives an overlap error.
- What happens when a reservation's start and end times span midnight? Operating hours end at 11:00 PM — no reservation may cross that boundary.
- What happens when the user provides a user identifier that has no existing reservations? An empty list is returned; no error is raised.
- What happens when a user requests a 30-minute slot (below the 1-hour minimum)? The system must reject with a minimum duration error.
- What happens when a user requests a slot starting exactly 60 minutes from now? It must be accepted (boundary is inclusive on 1-hour advance notice).
- What happens when a user tries to book a field that does not exist? The system must reject with a clear field-not-found error.
- What happens when a user has 2 active reservations but both have now passed? Both transition to `completed`; the user is immediately free to make new reservations up to the limit of 2 active simultaneously.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST display all available synthetic football fields and their open time slots for a user-specified date, showing only bookable 30-minute increments within operating hours.
- **FR-002**: System MUST present an identifier entry screen when the app is first opened; the entered identifier is retained for all subsequent actions within the session. All reservation operations (create, view, cancel) MUST use this session identifier without requiring the user to re-enter it.
- **FR-003**: System MUST enforce that every reservation spans a minimum of 1 hour and that both start and end times align to 30-minute increments (e.g., 10:00, 10:30, 11:00).
- **FR-004**: System MUST enforce operating hours: reservation start time MUST be 6:00 AM or later, and end time MUST be 11:00 PM or earlier.
- **FR-005**: System MUST reject any reservation request where the start time is less than 1 hour from the moment the request is submitted.
- **FR-006**: System MUST prevent two reservations for the same field from overlapping in time; the second conflicting request MUST be rejected.
- **FR-007**: System MUST reject a reservation request if the requesting user already holds 2 or more active reservations. A reservation is active only while its end time is in the future and it has not been cancelled. Reservations whose end time has passed automatically transition to `completed` and MUST NOT count toward this limit.
- **FR-008**: System MUST display all upcoming active reservations for the current session user — reservations whose end time is in the future and have not been cancelled. Completed and cancelled reservations MUST NOT appear in this view.
- **FR-009**: System MUST allow a user to cancel one of their active reservations by reservation identifier.
- **FR-010**: System MUST record a no-show when a cancellation is submitted with less than 2 hours of advance notice before the reservation start time.
- **FR-011**: System MUST return a clear, specific, human-readable error message for every domain rule violation, identifying which rule was violated.
- **FR-012**: System MUST reject cancellation requests for reservations that do not belong to the requesting user identifier.

### Key Entities

- **Field**: A synthetic football field available for reservation. Has a unique identifier and a human-readable name. Fields are pre-configured; creation and deletion are out of scope.
- **Reservation**: A booking of a specific field by a user for a continuous time range on a given date. Has a unique identifier, a user identifier, field reference, date, start time, end time, and status (`active` / `completed` / `cancelled`). Status transitions: `active` → `completed` automatically when end time passes; `active` → `cancelled` when explicitly cancelled by the user. Only `active` reservations count toward the 2-reservation limit.
- **NoShow**: A record that a user cancelled a reservation late. Linked to the original cancelled reservation. Contains the user identifier, reservation reference, and the timestamp of the cancellation.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can successfully complete a valid reservation in 3 steps or fewer (select field + time, submit, receive confirmation).
- **SC-002**: Every domain rule violation (overlap, operating hours, advance notice, duration, active reservation limit) results in a distinct, human-readable error message — 0 cases where a rule is violated silently or with a generic error.
- **SC-003**: Availability view accurately reflects real-time booking state — 0 cases where an available slot shown is actually occupied, or vice versa.
- **SC-004**: Cancellation with late notice reliably produces a no-show record in 100% of qualifying cases (cancellation < 2 hours before start).
- **SC-005**: A user with an existing active reservation for a field cannot create a second overlapping reservation for that same field — 0 double-bookings in the system at any point.

## Assumptions

- A user enters their identifier (e.g., a name or alias) once when opening the app; the interface retains it for the duration of the session and uses it automatically for all actions (creating, viewing, and cancelling reservations). No password, session token, or authentication is required. The system does not verify that the identifier belongs to a real person.
- Football fields are pre-seeded in the system and cannot be created or deleted through the user-facing interface (admin management is out of scope for the MVP).
- There is no explicit maximum reservation duration beyond the constraint that the end time must fall on or before 11:00 PM on the same day.
- The system operates in a single timezone; no timezone conversion or multi-timezone support is required.
- Concurrent reservation requests for the same slot are possible; the system must handle them correctly (first confirmed wins, second receives an overlap error).
- Payments and billing are entirely out of scope for this MVP.
- Notifications (email, SMS, push) are entirely out of scope for this MVP.
- No-show records are stored but no automated consequence (ban, penalty) is enforced in the MVP.
