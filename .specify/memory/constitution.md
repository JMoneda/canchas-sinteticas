<!--
SYNC IMPACT REPORT
==================
Version change: (template) → 1.0.0 (initial ratification)

Added sections:
  - Core Principles (5 principles)
  - Backend & Frontend Constraints
  - Domain Rules
  - Testing Standards
  - Governance

Modified principles: N/A (first version)
Removed sections: N/A (first version)

Templates checked:
  - .specify/templates/plan-template.md  ✅ Constitution Check section is generic — compatible
  - .specify/templates/spec-template.md  ✅ User story / acceptance criteria format aligns with domain rules
  - .specify/templates/tasks-template.md ✅ Phase structure aligns with TDD and layered architecture
  - .specify/templates/checklist-template.md ⚠️ pending — review once first feature checklist is generated

Follow-up TODOs: None — all fields resolved from provided specification.
-->

# Canchas Sintéticas Constitution

## Core Principles

### I. Domain-First Architecture (NON-NEGOTIABLE)

All business logic MUST reside in the domain layer. Backend and frontend layers MUST
treat the domain as the single source of truth. Any feature that places business rules
in a controller, route handler, or UI component MUST be rejected and refactored before
merging.

**Rationale**: Keeping logic in the domain makes it independently testable, prevents
duplication across layers, and ensures correctness regardless of delivery mechanism.

### II. Clean Architecture + SOLID

The system MUST follow Clean Architecture: domain → application → infrastructure →
delivery. Dependencies MUST point inward only — outer layers may depend on inner layers,
never the reverse. All components MUST follow SOLID principles: single responsibility,
open/closed, Liskov substitution, interface segregation, and dependency inversion.

**Rationale**: Clean Architecture keeps the system understandable and extensible without
requiring a full rewrite when infrastructure details change.

### III. Simplicity Over Engineering

The simplest solution that satisfies current requirements MUST be chosen. No
microservices. No message queues. No external caching layers. Every abstraction MUST be
justified against a concrete simpler alternative that was evaluated and rejected.
The system MUST be fully understandable in a single reading of the codebase.

**Rationale**: This is an MVP. Complexity that cannot be justified by a current
requirement wastes effort and makes the codebase harder to reason about.

### IV. Test-Driven Domain

All domain rules MUST have unit tests written before implementation (TDD). Tests MUST
be confirmed to fail (Red) before implementation begins (Green), then refactored.
Backend API endpoints SHOULD have basic integration tests. No domain rule is considered
complete until its unit tests pass. Test coverage of domain logic MUST be exhaustive —
every rule in Section 3 (Domain Rules) MUST have a corresponding test.

**Rationale**: Domain rules for reservations have subtle edge cases (overlap detection,
advance notice, concurrent limits). Tests are the only reliable way to enforce them.

### V. MVP Scope Discipline

Only features explicitly listed in this constitution MUST be built. Any addition that
goes beyond the MVP scope (Section 2) MUST be explicitly deferred and documented.
Scope creep MUST be rejected at specification and review time.

**Rationale**: Production quality on a small scope delivers more value than partial
quality on a large scope. The MVP boundary is intentional.

## Technical Constraints

### Backend

- **Language**: Python
- **Framework**: FastAPI (allowed, keep minimal) or none
- **Persistence**: SQLite ONLY — no Postgres, no external databases
- **ORM**: SQLAlchemy (allowed but MUST remain simple — no complex relationships,
  no lazy-loading chains)
- **Architecture**: No microservices. No message queues. Single deployable unit.

### Frontend

- **Framework**: React (MUST be used)
- **State management**: `useState` / `useReducer` ONLY — Redux and external state
  libraries are prohibited
- **Required capabilities**:
  - Create a reservation
  - View existing reservations
  - Display and handle validation errors returned from the backend

## Domain Rules (STRICT — NON-NEGOTIABLE)

These rules MUST be enforced in the domain layer and MUST have unit tests:

1. A field CANNOT have two reservations that overlap in the same time slot.
2. Reservations MUST be a minimum of 1 hour and scheduled in 30-minute blocks.
3. Operating hours are 6:00 AM to 11:00 PM — no reservation may start or end
   outside this window.
4. A reservation CANNOT be created with less than 1 hour of advance notice from
   the current time.
5. A user CANNOT hold more than 2 active reservations simultaneously.
6. A cancellation made with less than 2 hours of advance notice MUST be recorded
   as a "no-show" in addition to the cancellation.

## Testing Standards

- Domain rules (Section 3) MUST each have dedicated unit tests — written first,
  confirmed failing, then implemented.
- Backend API endpoints SHOULD have basic integration tests covering happy path
  and key error cases.
- Any business logic not covered by a test is considered incomplete.
- Test files MUST live alongside or co-located with the code they test following
  the structure defined in each feature's `plan.md`.

## Governance

This constitution supersedes all other development practices and conventions.
When any guideline conflicts with this document, this document wins.

**Enforcement rules**:
- Business logic found outside the domain layer MUST be rejected in code review.
- Unnecessary abstractions MUST be flagged and removed before merging.
- Any pattern that introduces overengineering (extra layers, frameworks, or services
  not listed in Technical Constraints) MUST be rejected.

**Amendment procedure**:
1. Propose the amendment with written justification referencing the violated principle
   and the concrete problem it solves.
2. Update this file and bump the version (versioning policy below).
3. Propagate changes to affected templates via `/speckit-constitution`.
4. Include before/after diff in the PR description.

**Versioning policy**:
- MAJOR: Removal or incompatible redefinition of an existing principle or domain rule.
- MINOR: New principle, section, or domain rule added.
- PATCH: Clarifications, wording, or non-semantic refinements.

**Compliance review**: Every `plan.md` MUST include a Constitution Check that verifies
the feature design against all five principles and all domain rules before Phase 0
research begins.

**Version**: 1.0.0 | **Ratified**: 2026-06-25 | **Last Amended**: 2026-06-25
