<!--
SYNC IMPACT REPORT
==================
Version change: 1.0.0 → 1.1.0 (MINOR)

Rationale: Technical Constraints rewritten to reflect the ACTUAL stack (.NET Clean
Architecture + in-memory persistence + React/TS/Tailwind/Vite) instead of the previously
documented Python/FastAPI/SQLite. One new domain rule added (Rule 7 — payment confirmation
must come from the provider). Core Principles are unchanged. Per the versioning policy, a
new domain rule and materially expanded/changed constraints constitute a MINOR bump.

Modified sections:
  - Technical Constraints → Backend: Python/FastAPI/SQLite/SQLAlchemy → C#/.NET Clean
    Architecture, ASP.NET Core Web API, in-memory persistence (IRepository → EF Core later),
    JWT, PBKDF2, multi-tenant by OwnerId.
  - Technical Constraints → Frontend: React + TypeScript + Vite + Tailwind; state via
    useState/useReducer + React Context for auth/cross-cutting state (Redux still prohibited).

Added:
  - Domain Rule 7: reservation "paid" status may only be set after payment-provider
    confirmation (never optimistically).

Removed sections: N/A

Templates checked:
  - .specify/templates/plan-template.md   ✅ Constitution Check section is generic — compatible
  - .specify/templates/spec-template.md   ✅ User story / acceptance format aligns — no change
  - .specify/templates/tasks-template.md  ✅ Phase structure aligns with TDD + layered arch
  - .specify/templates/checklist-template.md ✅ generic — compatible

Follow-up TODOs: None.
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

**Rationale**: Domain rules for reservations and payments have subtle edge cases (overlap
detection, advance notice, concurrent limits, asynchronous payment confirmation). Tests are
the only reliable way to enforce them.

### V. MVP Scope Discipline

Only features explicitly listed in this constitution or in an approved feature
specification MUST be built. Any addition that goes beyond the agreed MVP scope MUST be
explicitly deferred and documented. Scope creep MUST be rejected at specification and
review time.

**Rationale**: Production quality on a small scope delivers more value than partial
quality on a large scope. The MVP boundary is intentional.

## Technical Constraints

### Backend

- **Language**: C# / .NET
- **Architecture**: Clean Architecture across four projects —
  `CanchasSinteticas.Domain`, `CanchasSinteticas.Application`,
  `CanchasSinteticas.Infrastructure`, `CanchasSinteticas.Api`
  (solution `CanchasSinteticas.slnx`). Dependencies point inward only.
- **API**: ASP.NET Core Web API. JSON serialized in **snake_case**. Swagger for
  documentation. No microservices, no message queues — single deployable unit.
- **Persistence**: **In-memory** (`InMemoryDatabase` backed by `ConcurrentDictionary`,
  registered as a singleton). All data access MUST go through `IRepository`
  abstractions in the Domain layer so a real database (EF Core) can be plugged in later
  **without changing Domain or Application code**. No persistence detail may leak inward.
- **Authentication**: JWT Bearer (HMAC-SHA256). Password hashing with PBKDF2. Secrets and
  signing keys MUST come from configuration, never hardcoded in source.
- **Authorization / Multi-tenancy**: Role-based (`SuperAdmin`, `Owner`, `Client`).
  Tenant isolation is logical, rooted at `OwnerId` (Owner → Venue → Court). Ownership
  checks MUST be centralized and applied on every owner-scoped resource.

### Frontend

- **Framework**: React + TypeScript, built with Vite. Styling with Tailwind CSS. SPA.
- **State management**: `useState` / `useReducer` for local state and **React Context**
  for cross-cutting concerns such as authentication. Redux and other external global
  state libraries are prohibited (unjustified complexity for this MVP).
- **Required capabilities**:
  - Create and view reservations
  - Complete a payment and view its result/receipt
  - Display and handle validation and error responses returned from the backend

## Domain Rules (STRICT — NON-NEGOTIABLE)

These rules MUST be enforced in the domain layer and MUST have unit tests:

1. A court CANNOT have two reservations that overlap in the same time slot.
2. Reservations MUST be a minimum of 1 hour and scheduled in 30-minute blocks.
3. Operating hours are defined per venue — no reservation may start or end outside the
   venue's opening/closing window.
4. A reservation CANNOT be created with less than 1 hour of advance notice from the
   current time.
5. A user CANNOT hold more than 2 active reservations simultaneously.
6. A cancellation made with less than the venue's cancellation-window notice MUST be
   treated as a late cancellation (no refund) in addition to the cancellation.
7. A reservation (or a split-payment share) MUST only be marked as **paid** after the
   payment provider confirms the transaction as approved — never optimistically and never
   before receiving a verified provider confirmation.

## Testing Standards

- Domain rules (Section "Domain Rules") MUST each have dedicated unit tests — written
  first, confirmed failing, then implemented.
- Backend API endpoints SHOULD have basic integration tests covering happy path and key
  error cases.
- Any business logic not covered by a test is considered incomplete.
- Test files MUST follow the structure defined in each feature's `plan.md`.

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
- MINOR: New principle, section, or domain rule added, or a material change to constraints.
- PATCH: Clarifications, wording, or non-semantic refinements.

**Compliance review**: Every `plan.md` MUST include a Constitution Check that verifies
the feature design against all five principles and all domain rules before Phase 0
research begins.

**Version**: 1.1.0 | **Ratified**: 2026-06-25 | **Last Amended**: 2026-07-24
