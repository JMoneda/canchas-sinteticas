# Implementation Plan: Reservation System for Synthetic Football Fields

**Branch**: `001-reservation-system` | **Date**: 2026-06-25 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-reservation-system/spec.md`

## Summary

Build a full-stack MVP reservation system for synthetic football fields. The backend
follows Clean Architecture (domain → application → infrastructure → API) with
Python/FastAPI/SQLite. The frontend is a minimal React SPA using useState/useReducer.
Domain-layer purity is non-negotiable: all 6 business rules live exclusively in the
domain, each backed by unit tests written first (TDD). Persistence uses SQLAlchemy
with ORM models physically separated from domain entities. Reservation "completion"
is computed at query time (end_datetime < now) — no background job needed for MVP.

## Technical Context

**Language/Version**: Python 3.11+ (backend) · Node.js 20+ / React 18 (frontend)

**Primary Dependencies**:
- Backend: `fastapi`, `uvicorn`, `sqlalchemy`, `pytest`, `httpx`, `pytest-cov`
- Frontend: `react`, `react-dom`, `vite`

**Storage**: SQLite — single file (`backend/reservations.db`)

**Testing**: pytest + httpx TestClient (unit & integration)

**Target Platform**: Local development server, single machine

**Project Type**: Web application — REST API backend + React SPA frontend

**Performance Goals**: MVP-level; no explicit throughput targets. SQLite serialization
handles expected concurrent write load via IMMEDIATE transactions.

**Constraints**: SQLite only · No microservices · No message queues · React
useState/useReducer only · No Redux or external state libraries.

**Scale/Scope**: MVP — 2–3 pre-seeded fields, handful of concurrent users, single timezone.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Domain-First Architecture | ✅ PASS | All 6 rules in domain; API routes handle HTTP only |
| II. Clean Architecture + SOLID | ✅ PASS | Dependency direction: domain ← application ← infrastructure/API |
| III. Simplicity Over Engineering | ✅ PASS | Single process per tier; no extra layers beyond required |
| IV. Test-Driven Domain | ✅ PASS | Domain unit tests written first (Red→Green→Refactor) |
| V. MVP Scope Discipline | ✅ PASS | No payments, auth, admin, notifications |

**Domain Rules Coverage**:

| Rule | Enforced In | Test |
|------|-------------|------|
| No field overlap | `TimeSlot.overlaps_with()` + repository check in use case | Required |
| Min 1h / 30-min blocks | `TimeSlot` validation on construction | Required |
| Operating hours 6 AM–11 PM | `TimeSlot` validation on construction | Required |
| 1h advance notice | `TimeSlot.is_bookable(now)` | Required |
| Max 2 active reservations | `CreateReservation` use case via repository count | Required |
| No-show on late cancellation | `CancelReservation` use case | Required |

**Post-Phase 1 Re-check**: ✅ PASS — design artifacts (data model, contracts) introduce no
new layers, dependencies, or abstractions beyond what the constitution permits.

**GATE: PASSED. No violations.**

## Project Structure

### Documentation (this feature)

```text
specs/001-reservation-system/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   ├── fields.md        # GET /api/fields/availability
│   └── reservations.md  # POST/GET/DELETE /api/reservations
└── tasks.md             # Phase 2 output (/speckit-tasks)
```

### Source Code (repository root)

```text
backend/
├── domain/
│   ├── entities/
│   │   ├── __init__.py
│   │   ├── field.py              # Field entity (id, name)
│   │   └── reservation.py        # Reservation entity + status logic
│   ├── value_objects/
│   │   ├── __init__.py
│   │   └── time_slot.py          # TimeSlot: all booking validations
│   ├── repositories/
│   │   ├── __init__.py
│   │   ├── field_repository.py   # Abstract port
│   │   └── reservation_repository.py  # Abstract port
│   └── exceptions.py             # DomainError and subclasses
├── application/
│   ├── use_cases/
│   │   ├── __init__.py
│   │   ├── create_reservation.py
│   │   ├── cancel_reservation.py
│   │   ├── list_reservations.py
│   │   └── list_available_slots.py
│   └── dtos.py                   # Input/output data classes
├── infrastructure/
│   ├── database.py               # SQLAlchemy engine + session factory
│   ├── models/
│   │   ├── __init__.py
│   │   ├── field_model.py        # ORM model (separate from domain entity)
│   │   ├── reservation_model.py  # ORM model
│   │   └── no_show_model.py      # ORM model
│   ├── repositories/
│   │   ├── __init__.py
│   │   ├── sqlite_field_repository.py
│   │   └── sqlite_reservation_repository.py
│   └── seed.py                   # Pre-seeds fields on startup
├── api/
│   ├── main.py                   # FastAPI app + lifespan
│   ├── dependencies.py           # Dependency injection wiring
│   └── routes/
│       ├── __init__.py
│       ├── fields.py             # GET /api/fields/availability
│       └── reservations.py       # POST/GET/DELETE /api/reservations
├── tests/
│   ├── unit/
│   │   └── domain/
│   │       ├── test_time_slot.py          # TimeSlot validation rules
│   │       ├── test_reservation_rules.py  # Overlap, limit, advance notice
│   │       └── test_cancel_rules.py       # No-show threshold rule
│   └── integration/
│       └── test_api.py           # Full-stack endpoint tests via TestClient
├── requirements.txt
└── pyproject.toml

frontend/
├── src/
│   ├── components/
│   │   ├── IdentifierGate.jsx    # Entry screen — captures user identifier
│   │   ├── FieldAvailability.jsx # Slot grid per field
│   │   ├── ReservationForm.jsx   # Booking form
│   │   ├── ReservationList.jsx   # Active reservations + cancel button
│   │   └── ErrorMessage.jsx      # Renders domain error messages
│   ├── services/
│   │   └── api.js                # All fetch calls to backend
│   └── App.jsx                   # Root: useReducer session state + routing
├── index.html
├── package.json
└── vite.config.js
```

**Structure Decision**: Web application (backend + frontend as separate root-level
directories). Backend uses Clean Architecture layer directories as the primary
organizational unit. ORM models (`infrastructure/models/`) and domain entities
(`domain/entities/`) are in separate directories — no shared base class, no import
from infrastructure into domain.

## Complexity Tracking

> No violations detected in Constitution Check. No entries required.
