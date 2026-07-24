# Implementation Plan: Rediseño del frontend + validación de formularios

**Branch**: `003-frontend-redesign` | **Date**: 2026-07-24 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/003-frontend-redesign/spec.md`

## Summary

Rediseño visual completo de la SPA ("deportivo, premium y elegante") y endurecimiento de la
validación de formularios, **sin tocar backend ni contratos de API**. El enfoque es centralizar
el sistema de diseño en el módulo de componentes propio (`frontend/src/components/ui.tsx`) —
ampliado con tokens (color, tipografía, espaciado, radios, sombras, motion) definidos vía
`@theme` de Tailwind v4 en `index.css` — de modo que el restyling se propague a todas las
páginas, que ya consumen esos componentes. La validación se centraliza en un hook y un conjunto
de validadores puros reutilizables, aplicados primero al registro (contraseña ≥ 8 con letra +
número, confirmación, medidor de fortaleza, errores por campo, envío bloqueado) y luego a login
y formularios del panel de dueño.

## Technical Context

**Language/Version**: TypeScript 5.6, React 18.3

**Primary Dependencies**: React Router 6.26, Tailwind CSS v4.1 (`@tailwindcss/vite`), Vite 5.4.
**Sin nuevas dependencias** (decisión del usuario + principio de Simplicidad).

**Storage**: N/A (frontend). El estado de autenticación vive en React Context; datos vía API REST.

**Testing**: No hay framework de test de frontend instalado. Verificación por `tsc -b` (type-check),
`vite build`, y validación manual guiada por `quickstart.md`. La lógica de validación se extrae a
funciones puras (`lib/validation.ts`) para que sea testeable de forma aislada si más adelante se
añade Vitest (fuera de alcance ahora).

**Target Platform**: Navegadores modernos (SPA). Responsive 360px–1440px+.

**Project Type**: Web app (frontend existente + backend .NET ya presente; esta feature es solo frontend).

**Performance Goals**: Interfaz fluida; sin regresiones de bundle relevantes (cero libs nuevas).
Micro-interacciones a 60fps usando solo transición/transform de CSS.

**Constraints**: Contraste AA, foco visible, navegación completa por teclado, sin desbordes
horizontales. No alterar contratos de API (JSON snake_case) ni la lógica de negocio.

**Scale/Scope**: ~12 páginas + ~2 componentes compartidos (`ui.tsx`, `Layout.tsx`) +
`PaymentMethodDialog`. Idioma español.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Evaluación |
|-----------|------------|
| **I. Domain-First** | ✅ Sin lógica de negocio nueva en frontend. La validación de cliente es de *entrada/UX* y complementa (no reemplaza) la validación del backend, que sigue siendo la autoridad (FR-021, Assumptions). |
| **II. Clean Architecture + SOLID** | ✅ Se respeta la separación existente. La lógica de validación se extrae a funciones puras reutilizables (SRP), los componentes solo presentan. |
| **III. Simplicity Over Engineering** | ✅ Cero dependencias nuevas. Se amplían componentes propios y tokens de Tailwind en vez de introducir shadcn/Radix u otra librería. Sin state manager global nuevo. |
| **IV. Test-Driven Domain** | ✅ No se añade ni modifica lógica de dominio, por lo que no aplican tests de dominio. La validación se aísla en funciones puras para permitir pruebas futuras. |
| **V. MVP Scope Discipline** | ✅ Alcance acotado a rediseño + validación de formularios existentes. No se agregan features de producto. Modo oscuro explícitamente fuera de alcance. |

**Constraint de constitución (Frontend)**: React + TS + Vite + Tailwind, estado con
useState/useReducer + Context, sin Redux. ✅ Cumplido.

**Resultado del gate**: PASA. Sin violaciones → sección de Complexity Tracking no aplica.

## Project Structure

### Documentation (this feature)

```text
specs/003-frontend-redesign/
├── plan.md              # Este archivo
├── research.md          # Fase 0: decisiones de diseño y validación
├── data-model.md        # Fase 1: tokens del design system + reglas de validación
├── quickstart.md        # Fase 1: guía de verificación manual
├── contracts/
│   ├── design-system.md # Contrato de componentes UI y tokens
│   └── validation.md    # Contrato de reglas de validación por formulario
└── tasks.md             # Fase 2 (/speckit-tasks — NO lo crea /speckit-plan)
```

### Source Code (repository root)

```text
frontend/
├── index.html
├── src/
│   ├── index.css                 # [MOD] Tokens @theme: paleta premium, tipografía, sombras, radios
│   ├── main.tsx
│   ├── App.tsx                   # (sin cambios de rutas)
│   ├── api/                      # [SIN CAMBIOS] client.ts, types.ts
│   ├── auth/                     # [SIN CAMBIOS] AuthContext, ProtectedRoute
│   ├── lib/
│   │   ├── format.ts             # [MOD] refinar clases de estado (badges/slots) a nuevos tokens
│   │   ├── useAsync.ts           # [SIN CAMBIOS]
│   │   └── validation.ts         # [NUEVO] validadores puros: email, phone, password policy, strength
│   ├── hooks/
│   │   └── useFormValidation.ts  # [NUEVO] hook genérico: valores, errores, touched, isValid
│   ├── components/
│   │   ├── ui.tsx                # [MOD FUERTE] Button, Card, Field, Input, Badge, Spinner,
│   │   │                         #   EmptyState, ErrorBanner + NUEVOS: TextInput, PasswordField,
│   │   │                         #   PasswordStrengthMeter, FieldError, SectionHeading, Skeleton
│   │   ├── Layout.tsx            # [MOD] header/nav/footer premium, foco accesible
│   │   └── PaymentMethodDialog.tsx # [MOD] estilo consistente
│   └── pages/                    # [MOD] todas: aplicar nuevos componentes/tokens
│       ├── MarketplacePage.tsx       #   hero deportivo + cards de sede premium
│       ├── VenueDetailPage.tsx
│       ├── MyReservationsPage.tsx
│       ├── OpenMatchesPage.tsx
│       ├── LoginPage.tsx             #   validación por campo
│       ├── RegisterPage.tsx          #   política de contraseña + medidor + confirmación
│       ├── NotFoundPage.tsx
│       ├── OwnerDashboardPage.tsx
│       ├── OwnerVenuesPage.tsx
│       ├── OwnerVenueDetailPage.tsx  #   validación de formularios del panel
│       └── OwnerAgendaPage.tsx
```

**Structure Decision**: Web app con frontend existente. Toda la feature vive bajo `frontend/src`.
El eje del rediseño es doble: (1) **tokens** en `index.css` (`@theme`) + (2) **componentes**
en `components/ui.tsx`. Como todas las páginas ya importan de `components/ui`, restilar el núcleo
propaga el cambio; cada página solo ajusta composición/espaciado. La validación añade dos módulos
nuevos (`lib/validation.ts`, `hooks/useFormValidation.ts`) consumidos por los formularios.

## Complexity Tracking

No aplica — Constitution Check pasa sin violaciones.
