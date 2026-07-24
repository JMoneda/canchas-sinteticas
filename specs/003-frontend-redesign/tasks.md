---
description: "Task list — Rediseño del frontend + validación de formularios"
---

# Tasks: Rediseño del frontend + validación de formularios

**Input**: Design documents from `specs/003-frontend-redesign/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: No se solicitaron pruebas automatizadas y el proyecto no tiene runner de frontend.
La verificación es por `tsc -b`, `vite build` y los escenarios manuales de `quickstart.md`. Los
validadores se implementan como funciones puras para permitir tests futuros sin refactor.

**Organization**: Tareas agrupadas por historia de usuario. Todas las rutas son relativas a la
raíz del repo. El frontend vive en `frontend/`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede correr en paralelo (archivos distintos, sin dependencias pendientes)
- **[Story]**: US1 / US2 / US3
- Cada tarea incluye la ruta exacta del archivo

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Línea base y estructura para el trabajo del rediseño (sin dependencias nuevas).

- [ ] T001 Verificar línea base: ejecutar `npm run type-check` y `npm run build` en `frontend/` y anotar que pasan antes de empezar
- [ ] T002 Crear carpeta `frontend/src/hooks/` para el hook de validación (nuevo módulo del plan)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Sistema de diseño y utilidades de validación compartidas. Bloquean TODAS las historias.

**⚠️ CRITICAL**: Ninguna historia puede completarse hasta terminar esta fase.

### Tokens del sistema de diseño

- [ ] T003 Definir tokens `@theme` premium en `frontend/src/index.css`: paleta brand refinada + `accent`, superficies (`surface`, `surface-muted`), tinta (`ink`, `ink-muted`), estados (danger/warning/info), `--font-display`/`--font-sans`, radios (`radius-card`, `radius-control`), sombras suaves y estilos base de `body`/foco global (`focus-visible`) respetando `prefers-reduced-motion`

### Validación (funciones puras + hook) — según contracts/validation.md

- [ ] T004 [P] Crear `frontend/src/lib/validation.ts` con `validateRequired`, `validateEmail`, `validatePhone`, `validatePasswordPolicy` (≥8, letra+número), `validateMatch`, `passwordStrength` (0–4) y `passwordStrengthLabel`
- [ ] T005 [P] Crear `frontend/src/hooks/useFormValidation.ts` (values, errors, touched, isValid, setValue, setTouched, validateAll, reset) según contracts/validation.md

### Componentes compartidos — según contracts/design-system.md

- [ ] T006 Restilar componentes existentes en `frontend/src/components/ui.tsx` (Button con tamaño `lg` + estado `loading`, `buttonClasses`, Card, Badge, Spinner, ErrorBanner con `role="alert"`, EmptyState con `icon?`/`action?`, `inputClasses` con variante de error, ModalShell con foco atrapado + cierre con Escape) usando los nuevos tokens
- [ ] T007 Ampliar `Field` en `frontend/src/components/ui.tsx` para aceptar `error?` y `required?` con asociación accesible (`aria-describedby`, `aria-invalid`) y añadir `FieldError` (`role="alert"`)
- [ ] T008 Añadir `TextInput` y `PasswordField` (con toggle mostrar/ocultar accesible) en `frontend/src/components/ui.tsx` (mismo archivo que T006/T007/T009 → ejecutar en secuencia, no en paralelo)
- [ ] T009 Añadir `PasswordStrengthMeter`, `SectionHeading` y `Skeleton` en `frontend/src/components/ui.tsx` (mismo archivo que T006/T007/T008 → ejecutar en secuencia, no en paralelo)
- [ ] T010 Refinar clases de estado en `frontend/src/lib/format.ts` (`reservationStatusClasses`, `slotStatusClasses`) para alinearlas a los nuevos tokens
- [ ] T011 Rediseñar `frontend/src/components/Layout.tsx` (header sticky premium, nav con foco visible y estado activo, footer) usando tokens y componentes nuevos

**Checkpoint**: Design system y validación listos — las historias pueden comenzar.

---

## Phase 3: User Story 1 - Registro seguro con validación en vivo (Priority: P1) 🎯 MVP

**Goal**: El registro rechaza contraseñas débiles (`123456`), valida todos los campos en vivo,
incluye confirmación y medidor de fortaleza, y bloquea el envío hasta ser válido.

**Independent Test**: Abrir `/registro`, intentar enviar con datos inválidos (contraseña `123456`,
correo mal formado, confirmación distinta) y verificar bloqueo con errores por campo; con datos
válidos, la cuenta se crea y redirige por rol. (quickstart.md escenarios 1–7)

### Implementation for User Story 1

- [ ] T012 [US1] Reescribir `frontend/src/pages/RegisterPage.tsx` usando `useFormValidation` y los validadores para name/email/phone/password/confirmPassword, con `FieldError` por campo y estilos premium (FR-014, FR-015, FR-016, FR-017)
- [ ] T013 [US1] Integrar política de contraseña en el registro: `validatePasswordPolicy` + `PasswordStrengthMeter` en vivo; rechazar `123456` (FR-011, FR-012)
- [ ] T014 [US1] Añadir campo "Confirmar contraseña" con `validateMatch` y error si no coincide (FR-013)
- [ ] T015 [US1] Deshabilitar el botón "Crear cuenta" mientras `!isValid` y mostrar estado `loading` al enviar (FR-018)
- [ ] T016 [US1] Manejar error del backend (p. ej. correo duplicado) en `ErrorBanner` sin borrar los campos ya ingresados (FR-021)
- [ ] T017 [US1] Verificar `npm run type-check` y recorrer quickstart escenarios 1–7

### Backend hardening (misma política, autoridad final) — FR-025, FR-026

- [ ] T040 [US1] Endurecer `AuthService.Register` en `dotnet-backend/CanchasSinteticas.Application/Services/AuthService.cs`: exigir contraseña ≥8 con al menos una letra y un número (rechazar `123456`) y validar formato de correo, lanzando `ValidationError` con mensaje claro (FR-025, FR-026)
- [ ] T041 [P] [US1] Añadir pruebas en `dotnet-backend/CanchasSinteticas.Tests/Application/` que verifiquen: registro rechaza `123456`, rechaza contraseña sin número, rechaza sin letra, rechaza correo inválido, y acepta una contraseña válida (`Futbol2026`)
- [ ] T042 [US1] Ejecutar `dotnet test` en `dotnet-backend/` y confirmar que pasan (incluidas las nuevas y las existentes)

**Checkpoint**: Registro seguro y validado en cliente y backend, funcional de forma independiente.

---

## Phase 4: User Story 2 - Experiencia visual deportiva, premium y elegante (Priority: P1)

**Goal**: Identidad visual coherente y de alta calidad en todas las páginas, con estados de
carga/vacío/error consistentes y responsive.

**Independent Test**: Recorrer todas las páginas y verificar que comparten componentes y tokens,
con hero deportivo, cards atractivas y estados consistentes. (quickstart.md 10–11, 14)

### Implementation for User Story 2

- [ ] T018 [P] [US2] Rediseñar `frontend/src/pages/MarketplacePage.tsx`: hero deportivo premium con buscador y grilla de cards de sede (usar `Card`, `SectionHeading`, estados `Spinner`/`EmptyState`/`ErrorBanner`) (FR-001, FR-004)
- [ ] T019 [P] [US2] Rediseñar `frontend/src/pages/VenueDetailPage.tsx` (cabecera de sede, selector de cancha, grilla de horarios, modal de reserva) con tokens y componentes nuevos
- [ ] T020 [P] [US2] Rediseñar `frontend/src/pages/MyReservationsPage.tsx` con cards/badges y estados consistentes
- [ ] T021 [P] [US2] Rediseñar `frontend/src/pages/OpenMatchesPage.tsx` con cards y estados consistentes
- [ ] T022 [P] [US2] Rediseñar `frontend/src/pages/NotFoundPage.tsx` con `EmptyState`/estilo premium
- [ ] T023 [P] [US2] Rediseñar `frontend/src/components/PaymentMethodDialog.tsx` alineado al design system
- [ ] T024 [P] [US2] Rediseñar `frontend/src/pages/OwnerDashboardPage.tsx` con tokens y componentes compartidos
- [ ] T025 [P] [US2] Rediseñar `frontend/src/pages/OwnerVenuesPage.tsx` con tokens y componentes compartidos
- [ ] T026 [P] [US2] Rediseñar `frontend/src/pages/OwnerVenueDetailPage.tsx` (layout y presentación; la validación de sus formularios es US3)
- [ ] T027 [P] [US2] Rediseñar `frontend/src/pages/OwnerAgendaPage.tsx` con tokens y componentes compartidos
- [ ] T028 [US2] Pasada de responsive (360/768/1024/1440) sobre todas las páginas: sin desbordes ni solapamientos (FR-005, SC-005)
- [ ] T029 [US2] Verificar `npm run build` y recorrer quickstart 10–11 y 14 (consistencia, estados, responsive)

**Checkpoint**: Todas las páginas comparten la identidad visual premium.

---

## Phase 5: User Story 3 - Accesibilidad y feedback consistente en formularios (Priority: P2)

**Goal**: Foco visible en toda la app, contraste AA, navegación por teclado, y validación por
campo con el mismo patrón del registro en login y formularios del panel.

**Independent Test**: Navegar solo con teclado (foco visible), y validar login y formularios del
panel con datos inválidos. (quickstart.md 8–9, 12–13)

### Implementation for User Story 3

- [ ] T030 [US3] Aplicar validación por campo en `frontend/src/pages/LoginPage.tsx` con `useFormValidation` (email formato válido, password requerida) y botón deshabilitado si `!isValid`; conservar botones de demo (FR-019)
- [ ] T031 [US3] Aplicar validación por campo y bloqueo de guardado a los formularios de `frontend/src/pages/OwnerVenueDetailPage.tsx` (campos requeridos, numéricos/rango) con el mismo patrón (FR-020)
- [ ] T032 [P] [US3] Inspeccionar `frontend/src/pages/OwnerVenuesPage.tsx` y `frontend/src/pages/OwnerAgendaPage.tsx`, enumerar los formularios/campos de entrada que contengan (p. ej. crear/editar sede, filtros de agenda, bloqueos) y aplicarles validación por campo con el mismo patrón; si alguno no tiene entrada de datos, anotarlo explícitamente como "sin formulario" (FR-020)
- [ ] T033 [US3] Pasada de accesibilidad: foco visible en todos los controles, navegación por teclado en nav/menús/diálogos, `aria-*` en inputs y errores, cierre de diálogos con Escape (FR-007, FR-009, FR-010, SC-003)
- [ ] T034 [US3] Verificar contraste AA de texto y controles con los tokens definidos; ajustar tokens si algún par no cumple (FR-008, SC-004)
- [ ] T035 [US3] Verificar `npm run type-check` y recorrer quickstart 8–9, 12–13

**Checkpoint**: Formularios y accesibilidad consistentes en toda la app.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Cierre de calidad y no-regresión.

- [ ] T036 No-regresión funcional: recorrer quickstart escenario 15 (buscar→detalle→reservar→pagar sandbox, abrir partido, gestión de dueño y agenda) (SC-008, FR-024)
- [ ] T037 [P] Revisar consistencia final de tokens: eliminar clases hardcodeadas sobrantes fuera del sistema en páginas y componentes (SC-002)
- [ ] T038 Ejecutar `npm run type-check` y `npm run build` finales; corregir cualquier warning/typo introducido
- [ ] T039 [P] Actualizar `frontend/README.md` (si aplica) con una nota breve del sistema de diseño y la política de validación

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sin dependencias.
- **Foundational (Phase 2)**: depende de Setup. BLOQUEA todas las historias.
- **US1 (Phase 3)**: depende de Foundational (validación T004/T005 + componentes T007/T008/T009).
- **US2 (Phase 4)**: depende de Foundational (tokens T003 + componentes T006/T010/T011).
- **US3 (Phase 5)**: depende de Foundational; se beneficia de US1 (reusa el patrón de validación) y US2 (páginas ya restiladas).
- **Polish (Phase 6)**: depende de las historias deseadas completas.

### User Story Dependencies

- **US1 (P1)**: independiente tras Foundational. Es el MVP recomendado.
- **US2 (P1)**: independiente tras Foundational (puede correr en paralelo con US1; distinta superficie de archivos).
- **US3 (P2)**: tras Foundational; conviene después de US1 (patrón de validación) y US2 (páginas restiladas) para evitar retrabajo.

### Within Each User Story

- Componentes/tokens (Foundational) antes de aplicar en páginas.
- En US1: validadores/hook (Foundational) antes de reescribir el registro.

### Parallel Opportunities

- T004 y T005 en paralelo (Foundational).
- T008 y T009 en paralelo (componentes en `ui.tsx`, coordinar por ser el mismo archivo — hacer secuencial si genera conflicto de edición).
- En US2, T018–T027 son páginas distintas → altamente paralelizables.
- US1 y US2 pueden avanzar en paralelo tras el checkpoint de Foundational.

---

## Parallel Example: User Story 2

```bash
# Páginas independientes (archivos distintos) tras el checkpoint de Foundational:
Task: "Rediseñar MarketplacePage.tsx"      # T018
Task: "Rediseñar MyReservationsPage.tsx"   # T020
Task: "Rediseñar OpenMatchesPage.tsx"      # T021
Task: "Rediseñar OwnerDashboardPage.tsx"   # T024
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1: Setup
2. Phase 2: Foundational (crítico — bloquea todo)
3. Phase 3: US1 (registro seguro)
4. **STOP y VALIDAR**: probar el registro de forma independiente (quickstart 1–7)
5. Demo del MVP

### Incremental Delivery

1. Setup + Foundational → base lista (ya se ve el salto visual en nav/componentes)
2. US1 → registro seguro → demo
3. US2 → identidad visual en todas las páginas → demo
4. US3 → accesibilidad + validación en login/panel → demo
5. Polish → no-regresión y cierre

---

## Notes

- [P] = archivos distintos, sin dependencias pendientes. Nota: varias tareas de Foundational
  editan `ui.tsx` (mismo archivo) → no marcarlas como paralelas entre sí en ejecución real.
- No se cambian contratos de API ni lógica de backend (FR-023).
- Commit tras cada tarea o grupo lógico; validar cada historia en su checkpoint.
