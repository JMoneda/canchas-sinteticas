?---
description: "Task list for feature 002-payments-gateway"
---

# Tasks: Pagos reales, pago dividido y comprobantes

**Input**: Design documents from `/specs/002-payments-gateway/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/payments.md](./contracts/payments.md)

**Tests**: INCLUIDOS. La constitución (Principio IV — Test-Driven Domain, NO-NEGOCIABLE) exige tests
unitarios de dominio escritos **antes** de la implementación (Red → Green), y la Regla de Dominio 7
(pago solo aprobado tras confirmación del proveedor) debe estar cubierta por tests.

**Organization**: Tareas agrupadas por historia de usuario para implementación y prueba independiente.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo (archivos distintos, sin dependencias pendientes)
- **[Story]**: US1–US4 (mapea a las historias del spec)
- Rutas relativas a la raíz del repo. Backend en `dotnet-backend/`, frontend en `frontend/`.

## Path Conventions

- Domain: `dotnet-backend/CanchasSinteticas.Domain/`
- Application: `dotnet-backend/CanchasSinteticas.Application/`
- Infrastructure: `dotnet-backend/CanchasSinteticas.Infrastructure/`
- Api: `dotnet-backend/CanchasSinteticas.Api/`
- Tests: `dotnet-backend/CanchasSinteticas.Tests/` (crear si no existe)
- Frontend: `frontend/src/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Dependencias y configuración base para pagos.

- [X] T001 Añadir paquete QuestPDF a `dotnet-backend/CanchasSinteticas.Infrastructure/CanchasSinteticas.Infrastructure.csproj` y registrar `IHttpClientFactory` (`AddHttpClient`) para el gateway en `dotnet-backend/CanchasSinteticas.Api/Program.cs`
- [X] T002 [P] Añadir la sección no sensible `Payments` (Provider, ExpiryMinutes, Wompi.BaseUrl/PublicKey, Notifications.*) a `dotnet-backend/CanchasSinteticas.Api/appsettings.json` según [contracts/payments.md](./contracts/payments.md)
- [X] T003 [P] Crear proyecto de pruebas `dotnet-backend/CanchasSinteticas.Tests/` (xUnit) si no existe y referenciarlo en la solución `CanchasSinteticas.slnx`
- [X] T004 [P] Documentar carga de secretos (user-secrets / variables de entorno) para `Payments:Wompi:PrivateKey`, `EventsSecret`, `IntegritySecret` en `dotnet-backend/README` o [quickstart.md](./quickstart.md) (sin versionar secretos)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Máquina de estados del pago, abstracción del gateway y persistencia de soporte. Todo esto
bloquea las historias de usuario.

**⚠️ CRITICAL**: Ninguna historia puede comenzar hasta completar esta fase.

### Enums y dominio base

- [X] T005 [P] Extender `PaymentStatus` con `Processing, Rejected, Expired, RefundRequested` en `dotnet-backend/CanchasSinteticas.Domain/Enums/PaymentStatus.cs`
- [X] T006 [P] Extender `PaymentMethod` con `Card, Nequi, Pse, BancolombiaTransfer, BancolombiaButton, BancolombiaQr` en `dotnet-backend/CanchasSinteticas.Domain/Enums/PaymentMethod.cs`
- [X] T007 [P] Crear enum `SettlementMode` (Direct, Marketplace) en `dotnet-backend/CanchasSinteticas.Domain/Enums/SettlementMode.cs`
- [X] T008 [P] Crear enum `NotificationChannel` (InApp, Email, WhatsAppSms) en `dotnet-backend/CanchasSinteticas.Domain/Enums/NotificationChannel.cs`

### Máquina de estados del pago (TDD)

- [X] T009 [P] Escribir tests unitarios de transiciones de `Payment` (incluida **Regla 7**: `MarkApproved` solo desde `Processing`/`Pending`; reembolso solo desde `Paid`; idempotencia de estados terminales) en `dotnet-backend/CanchasSinteticas.Tests/Domain/PaymentStateMachineTests.cs` — deben FALLAR primero
- [X] T010 Reescribir la entidad `Payment` con campos nuevos (`MatchId, PayerUserId, GatewayTransactionId, GatewayReference, GatewayStatusRaw, CheckoutUrl, RefundReference, PaidAt, ExpiresAt`) y métodos `StartProcessing, MarkApproved, MarkRejected, MarkExpired, RequestRefund, ConfirmRefund, FailRefund, Fail` en `dotnet-backend/CanchasSinteticas.Domain/Entities/Payment.cs` hasta que T009 pase (Green)

### Persistencia de soporte

- [X] T011 [P] Crear entidad `ProcessedWebhookEvent` en `dotnet-backend/CanchasSinteticas.Domain/Entities/ProcessedWebhookEvent.cs`
- [X] T012 [P] Crear interfaz `IProcessedWebhookEventRepository` (`Exists`, `Add`) en `dotnet-backend/CanchasSinteticas.Domain/Repositories/IProcessedWebhookEventRepository.cs`
- [X] T013 Extender `IPaymentRepository` con `GetById, GetByGatewayTransactionId, GetByMatchAndPayer, GetSharesByMatch` en `dotnet-backend/CanchasSinteticas.Domain/Repositories/IPaymentRepository.cs`
- [X] T014 Implementar `InMemoryProcessedWebhookEventRepository` en `dotnet-backend/CanchasSinteticas.Infrastructure/Repositories/InMemoryProcessedWebhookEventRepository.cs` y añadir el diccionario en `dotnet-backend/CanchasSinteticas.Infrastructure/Persistence/InMemoryDatabase.cs`
- [X] T015 Extender `InMemoryPaymentRepository` con los métodos de T013 en `dotnet-backend/CanchasSinteticas.Infrastructure/Repositories/InMemoryPaymentRepository.cs`

### Abstracción del gateway e infraestructura de proveedor

- [X] T016 [P] Definir `IPaymentGateway` (`CreateTransaction`, `GetTransaction`, `Refund`) y sus DTOs de request/response en `dotnet-backend/CanchasSinteticas.Application/Abstractions/IPaymentGateway.cs`
- [X] T017 [P] Definir `IPaymentGatewayCredentialsResolver` (resuelve credenciales por `Venue`/plataforma según `SettlementMode`) en `dotnet-backend/CanchasSinteticas.Application/Abstractions/IPaymentGatewayCredentialsResolver.cs`
- [X] T018 [P] Definir `INotificationSender` (multicanal) en `dotnet-backend/CanchasSinteticas.Application/Abstractions/INotificationSender.cs`
- [X] T019 Implementar `WompiSignatureVerifier` (checksum SHA-256 sobre properties + timestamp + events secret) con tests en `dotnet-backend/CanchasSinteticas.Infrastructure/Payments/WompiSignatureVerifier.cs` y `dotnet-backend/CanchasSinteticas.Tests/Infrastructure/WompiSignatureVerifierTests.cs`
- [X] T020 Implementar `WompiPaymentGateway` (HttpClient a sandbox: crear transacción, consultar estado, reembolso) en `dotnet-backend/CanchasSinteticas.Infrastructure/Payments/WompiPaymentGateway.cs`
- [X] T021 Implementar `PaymentGatewayCredentialsResolver` (lee config `Payments:Wompi` y `Venue.SettlementMode`) en `dotnet-backend/CanchasSinteticas.Infrastructure/Payments/PaymentGatewayCredentialsResolver.cs`
- [X] T022 [P] Implementar `InAppNotifier` (canal garantizado; los demás canales se añaden en Polish) en `dotnet-backend/CanchasSinteticas.Infrastructure/Notifications/InAppNotifier.cs`

### Configuración de recaudo en Venue (base para US1 y config del dueño)

- [X] T023 Añadir `SettlementMode` y `GatewayMerchantRef` a `dotnet-backend/CanchasSinteticas.Domain/Entities/Venue.cs` (default `Marketplace`) y ajustar el seeder `dotnet-backend/CanchasSinteticas.Infrastructure/Seed/DatabaseSeeder.cs`

### Registro DI

- [X] T024 Registrar en `dotnet-backend/CanchasSinteticas.Api/Program.cs`: `IPaymentGateway`→`WompiPaymentGateway`, `IPaymentGatewayCredentialsResolver`, `INotificationSender`→`InAppNotifier`, `IProcessedWebhookEventRepository`, y opciones `Payments`

**Checkpoint**: Fundamentos listos — las historias de usuario pueden comenzar.

---

## Phase 3: User Story 1 - Pago real de reserva (Priority: P1) 🎯 MVP

**Goal**: El cliente paga una reserva con un método real (Nequi/PSE/Bancolombia/tarjeta); la reserva se
confirma solo tras la confirmación aprobada del proveedor vía webhook.

**Independent Test**: Crear reserva → iniciar pago → aprobar en sandbox → webhook confirma → reserva
`confirmed` y pago `paid` con referencia real; rechazo/expiración → franja liberada.

### Tests for User Story 1 ⚠️ (escribir primero, deben FALLAR)

- [X] T025 [P] [US1] Test de integración: `POST /api/reservations/{id}/pay` crea transacción y deja pago en `processing` (con `IPaymentGateway` falso) en `dotnet-backend/CanchasSinteticas.Tests/Api/ReservationPayTests.cs`
- [X] T026 [P] [US1] Test de integración del webhook: evento aprobado → reserva confirmada + pago `paid`; evento repetido → sin duplicado (idempotencia); firma inválida → sin cambios, en `dotnet-backend/CanchasSinteticas.Tests/Api/PaymentWebhookTests.cs`
- [X] T027 [P] [US1] Test del sweeper: pago `processing` vencido → `expired` y franja liberada, en `dotnet-backend/CanchasSinteticas.Tests/Application/PaymentExpiryTests.cs`
- [X] T027a [P] [US1] Test de fallo del proveedor (FR-009/C5): si `IPaymentGateway.CreateTransaction` lanza/da error, el endpoint responde **502**, el pago queda `Failed` y la franja **no** queda bloqueada, en `dotnet-backend/CanchasSinteticas.Tests/Api/ReservationPayTests.cs`

### Implementation for User Story 1

- [X] T028 [US1] Reescribir `PaymentService.Pay` para crear la transacción vía `IPaymentGateway` (resolviendo credenciales por sede), dejar el pago en `Processing` con `CheckoutUrl`/`ExpiresAt` y devolver checkout, en `dotnet-backend/CanchasSinteticas.Application/Services/PaymentService.cs`
- [X] T029 [US1] Crear `PaymentWebhookService` (verifica firma, aplica idempotencia con `IProcessedWebhookEventRepository`, transiciona el `Payment`, confirma la reserva al aprobar) y **notificar vía `INotificationSender` tanto en aprobación como en rechazo** (FR-026), en `dotnet-backend/CanchasSinteticas.Application/Services/PaymentWebhookService.cs`
- [X] T030 [US1] Actualizar DTOs de pago (`PayInput` con `method`/`return_url`, `PaymentInitiationOutput`, `PaymentStatusOutput`) en `dotnet-backend/CanchasSinteticas.Application/DTOs/PaymentDtos.cs`
- [X] T031 [US1] Implementar `PaymentExpirySweeper` (`IHostedService`) que expira pagos vencidos y libera la reserva, en `dotnet-backend/CanchasSinteticas.Infrastructure/BackgroundJobs/PaymentExpirySweeper.cs`; registrarlo en `Program.cs`
- [X] T032 [US1] Crear `PaymentsController` con `POST /api/payments/webhook` (público, sin JWT) y `GET /api/payments/{id}` (autorizado) en `dotnet-backend/CanchasSinteticas.Api/Controllers/PaymentsController.cs`
- [X] T033 [US1] Modificar `POST /api/reservations/{id}/pay` para el nuevo flujo async en `dotnet-backend/CanchasSinteticas.Api/Controllers/ReservationsController.cs`
- [X] T033a [US1] Conciliación de **aprobación tardía tras expirar** (FR/edge case C2): en `PaymentWebhookService`, si llega una aprobación para un pago ya `Expired`/reserva liberada, resolver el conflicto — **auto-reembolsar** vía `IPaymentGateway.Refund` si la franja ya no está libre, o **reconfirmar** (reactivar reserva + `MarkApproved`) si sigue libre; añadir método de dominio `Reactivate()`/transición controlada en `Payment.cs` y test en `dotnet-backend/CanchasSinteticas.Tests/Api/PaymentWebhookTests.cs`
- [X] T034 [P] [US1] Frontend: añadir `reservations.pay` (nuevo contrato), `payments.getStatus` y tipos en `frontend/src/api/client.ts` y `frontend/src/api/types.ts`
- [X] T035 [P] [US1] Frontend: componente selector de método de pago (Nequi/PSE/Bancolombia/tarjeta) en `frontend/src/components/`
- [X] T036 [US1] Frontend: flujo de checkout (redirección a `checkout_url`), página de resultado con **polling** de estado, y actualización de `MyReservationsPage.tsx` en `frontend/src/pages/`

**Checkpoint**: US1 funcional — MVP desplegable. Pago real de reserva con confirmación por webhook.

---

## Phase 4: User Story 2 - Pago dividido entre jugadores (Priority: P2)

**Goal**: Cada jugador de un partido con split paga su parte con un método real; se rastrea el recaudo y
se aplica la política de expiración/reembolso.

**Independent Test**: Abrir partido con split → cada jugador paga su parte (sandbox) → recaudo sube; al
cubrir el total la reserva se confirma; si expira sin completar, se reembolsan las partes pagadas.

### Tests for User Story 2 ⚠️ (escribir primero, deben FALLAR)

- [X] T037 [P] [US2] Tests de dominio: `AmountCollected` desde partes `Paid`, ajuste de redondeo `PricePerPlayer * MaxPlayers == TotalPrice`, `RefundAllShares`, en `dotnet-backend/CanchasSinteticas.Tests/Domain/MatchSplitTests.cs`
- [X] T038 [P] [US2] Test de integración: pagar todas las partes → reserva del partido confirmada; parte repetida bloqueada, en `dotnet-backend/CanchasSinteticas.Tests/Api/MatchPayShareTests.cs`

### Implementation for User Story 2

- [X] T039 [US2] Extender `Match`/`MatchPlayer`: `PaymentId` por jugador, `AmountCollected` desde pagos `Paid`, `SettlementDeadline`, `RefundAllShares()`, ajuste de redondeo, en `dotnet-backend/CanchasSinteticas.Domain/Entities/Match.cs` y `MatchPlayer.cs`
- [X] T040 [US2] Reescribir `MatchService.PayShare` para crear un `Payment` por parte (con `MatchId`+`PayerUserId`) vía `IPaymentGateway`, en `dotnet-backend/CanchasSinteticas.Application/Services/MatchService.cs`
- [X] T041 [US2] Extender `PaymentWebhookService` para confirmar partes de split y liquidar la reserva del partido cuando el recaudo cubre el total, en `dotnet-backend/CanchasSinteticas.Application/Services/PaymentWebhookService.cs`
- [X] T042 [US2] Extender `PaymentExpirySweeper` para aplicar la política de expiración del recaudo (reembolsar partes pagadas + liberar reserva) en `dotnet-backend/CanchasSinteticas.Infrastructure/BackgroundJobs/PaymentExpirySweeper.cs`
- [X] T043 [US2] Crear `POST /api/matches/{id}/pay-share` en `dotnet-backend/CanchasSinteticas.Api/Controllers/MatchesController.cs` (o el controller de matches existente) con DTOs en `dotnet-backend/CanchasSinteticas.Application/DTOs/`
- [X] T043a [US2] Manejar el **abandono de un jugador que ya pagó** (FR-018/US2 AC5/C1): al salir del partido antes del cierre, aplicar la política de reembolso a su parte (`IPaymentGateway.Refund` → `Payment.RequestRefund`), desvincular `MatchPlayer.PaymentId` y recalcular `AmountCollected`, en `dotnet-backend/CanchasSinteticas.Application/Services/MatchService.cs` (método `Leave`) con test en `dotnet-backend/CanchasSinteticas.Tests/Application/MatchLeaveRefundTests.cs`
- [X] T044 [P] [US2] Frontend: pago de parte vía gateway + estado de recaudo (pagado por jugador, total, faltante) en `frontend/src/pages/OpenMatchesPage.tsx` y `frontend/src/api/client.ts`

**Checkpoint**: US1 + US2 funcionan independientemente.

---

## Phase 5: User Story 3 - Comprobantes descargables (Priority: P2)

**Goal**: Tras un pago aprobado, el cliente ve y descarga un comprobante (reserva o parte de split) con
los datos de la transacción; acceso restringido.

**Independent Test**: Pago aprobado → descargar PDF con referencia/monto/método/fecha/sede/cancha; otro
usuario recibe 403.

### Tests for User Story 3 ⚠️ (escribir primero, deben FALLAR)

- [X] T045 [P] [US3] Test de integración: control de acceso al comprobante (titular y dueño OK; tercero 403; sin pago aprobado 404), en `dotnet-backend/CanchasSinteticas.Tests/Api/ReceiptAccessTests.cs`

### Implementation for User Story 3

- [X] T046 [P] [US3] Crear entidad `Receipt` en `dotnet-backend/CanchasSinteticas.Domain/Entities/Receipt.cs`
- [X] T047 [P] [US3] Crear `IReceiptRepository` (`Add, GetById, GetByPayment, GetByReservation`) en `dotnet-backend/CanchasSinteticas.Domain/Repositories/IReceiptRepository.cs` e `InMemoryReceiptRepository` en `dotnet-backend/CanchasSinteticas.Infrastructure/Repositories/InMemoryReceiptRepository.cs` (+ diccionario en `InMemoryDatabase.cs`)
- [X] T048 [P] [US3] Definir `IReceiptGenerator` en `dotnet-backend/CanchasSinteticas.Application/Abstractions/IReceiptGenerator.cs` e implementar `QuestPdfReceiptGenerator` en `dotnet-backend/CanchasSinteticas.Infrastructure/Receipts/QuestPdfReceiptGenerator.cs`
- [X] T049 [US3] Crear `ReceiptService` (genera y persiste el comprobante; aplica control de acceso titular/dueño) en `dotnet-backend/CanchasSinteticas.Application/Services/ReceiptService.cs`
- [X] T050 [US3] Enganchar la generación del comprobante en `PaymentWebhookService` al aprobar un pago (reserva y parte de split) en `dotnet-backend/CanchasSinteticas.Application/Services/PaymentWebhookService.cs`
- [X] T051 [US3] Endpoints `GET /api/reservations/{id}/receipt` (pdf/json) y `GET /api/matches/{id}/players/me/receipt` en los controllers correspondientes; registrar `IReceiptRepository`/`IReceiptGenerator` en `Program.cs`
- [X] T052 [P] [US3] Frontend: botón "Descargar comprobante" en `frontend/src/pages/MyReservationsPage.tsx` y en el detalle de partido; cliente API en `frontend/src/api/client.ts`

**Checkpoint**: US1 + US2 + US3 funcionan independientemente.

---

## Phase 6: User Story 4 - Reembolso al cancelar (Priority: P3)

**Goal**: Cancelar una reserva pagada dentro de la ventana permitida ejecuta un reembolso real; tarde no
hay reembolso. Se refleja el estado real del reembolso.

**Independent Test**: Cancelar dentro de ventana → `refund_requested` → `refunded` al confirmar; cancelar
tarde → sin reembolso.

### Tests for User Story 4 ⚠️ (escribir primero, deben FALLAR)

- [X] T053 [P] [US4] Tests: cancelación no tardía sobre pago `Paid` → solicita reembolso al gateway y pasa a `RefundRequested`; tardía → sin reembolso, en `dotnet-backend/CanchasSinteticas.Tests/Application/RefundTests.cs`

### Implementation for User Story 4

- [X] T054 [US4] Modificar `ReservationService.Cancel` para invocar `IPaymentGateway.Refund` cuando procede y transicionar el pago a `RefundRequested`, en `dotnet-backend/CanchasSinteticas.Application/Services/ReservationService.cs`
- [X] T055 [US4] Extender `PaymentWebhookService` para procesar la confirmación de reembolso (`RefundRequested → Refunded`, o volver a `Paid` si el proveedor lo rechaza) y **notificar al cliente el reembolso confirmado vía `INotificationSender`** (FR-026), en `dotnet-backend/CanchasSinteticas.Application/Services/PaymentWebhookService.cs`
- [X] T056 [US4] Actualizar `CancelOutput`/DTO con `refund_status` y el endpoint `POST /api/reservations/{id}/cancel` en `dotnet-backend/CanchasSinteticas.Api/Controllers/ReservationsController.cs`
- [X] T057 [P] [US4] Frontend: mostrar `refund_status` al cancelar en `frontend/src/pages/MyReservationsPage.tsx`

**Checkpoint**: Todas las historias funcionan independientemente.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Configuración del dueño, canales de notificación adicionales, endurecimiento y validación.

- [X] T058 Implementar `VenuePaymentConfigService` + `PUT /api/owner/venues/{id}/payment-config` (settlement mode + merchant ref) en `dotnet-backend/CanchasSinteticas.Application/Services/VenuePaymentConfigService.cs` y `dotnet-backend/CanchasSinteticas.Api/Controllers/OwnerVenuesController.cs`
- [ ] T059 [P] Frontend: página del dueño para configurar el modelo de recaudo de la sede en `frontend/src/pages/`
- [X] T060 [P] Implementar `EmailNotifier` y `WhatsAppSmsNotifier` (activados por config `Payments:Notifications`, con adjunto/enlace al comprobante) en `dotnet-backend/CanchasSinteticas.Infrastructure/Notifications/` y componer los canales en `INotificationSender`
- [X] T061 [P] Actualizar el README de la API con los nuevos endpoints de pago/webhook/comprobantes
- [X] T062 Endurecimiento de seguridad: revisar que ningún secreto quede en código/logs, rate-limit básico del webhook, y que el webhook nunca exponga detalles internos
- [X] T062a Verificar/ajustar que `ReportService` (`total_revenue` por sede/dueño) contabilice solo pagos en estado `Paid` reales y no incluya `Refunded`/`Expired` (FR-028/C4), con test en `dotnet-backend/CanchasSinteticas.Tests/Application/RevenueAttributionTests.cs`
- [ ] T063 Ejecutar la validación completa de [quickstart.md](./quickstart.md) (escenarios A–H) y corregir desviaciones

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sin dependencias.
- **Foundational (Phase 2)**: depende de Setup — **BLOQUEA** todas las historias.
- **User Stories (Phase 3–6)**: dependen de Foundational.
  - US1 (P1) es la base del cobro; US2/US3/US4 reutilizan `PaymentWebhookService` y la máquina de estados.
- **Polish (Phase 7)**: depende de las historias deseadas.

### User Story Dependencies

- **US1 (P1)**: solo requiere Foundational. MVP.
- **US2 (P2)**: requiere Foundational; extiende el webhook de US1 (integración, pero probable independiente con gateway falso).
- **US3 (P2)**: requiere Foundational; se engancha al webhook para generar comprobantes (US1/US2 producen los pagos aprobados).
- **US4 (P3)**: requiere Foundational + un pago `Paid` (US1).

### Within Each User Story

- Los tests se escriben y **fallan** antes de implementar (TDD, Principio IV).
- Dominio → repositorios → servicios → endpoints → frontend.

### Parallel Opportunities

- Setup: T002, T003, T004 en paralelo.
- Foundational: enums T005–T008 en paralelo; T009 (test) en paralelo con enums; abstracciones T016–T018 y T022 en paralelo.
- Dentro de cada historia, las tareas [P] (frontend vs backend, entidades distintas) en paralelo.
- Con equipo: tras Foundational, US1–US4 pueden repartirse (respetando que US3/US4 integran el webhook de US1).

---

## Parallel Example: User Story 1

```text
# Tests de US1 juntos (deben fallar primero):
T025 ReservationPayTests.cs
T026 PaymentWebhookTests.cs
T027 PaymentExpiryTests.cs

# Frontend de US1 en paralelo con backend:
T034 client.ts/types.ts
T035 componente selector de método
```

---

## Implementation Strategy

### MVP First (solo US1)

1. Phase 1 (Setup) → 2. Phase 2 (Foundational, CRÍTICA) → 3. Phase 3 (US1).
4. **PARAR y VALIDAR**: escenarios A–D del quickstart (pago aprobado, rechazo/expiración, idempotencia, firma inválida).
5. Desplegar/demostrar MVP: cobro real de reservas.

### Incremental Delivery

1. Setup + Foundational → base lista.
2. US1 → validar → demo (MVP).
3. US2 (split) → validar → demo.
4. US3 (comprobantes) → validar → demo.
5. US4 (reembolsos) → validar → demo.
6. Polish (config de recaudo del dueño, email/WhatsApp, seguridad, validación completa).

---

## Notes

- [P] = archivos distintos, sin dependencias pendientes.
- Mantener toda la lógica de negocio en el dominio (Principio I): transiciones en `Payment`, recaudo en `Match`.
- **Regla 7**: `MarkApproved` solo se invoca desde `PaymentWebhookService` tras verificar la firma — nunca en la respuesta síncrona del pago.
- Persistencia en memoria: un reinicio pierde pagos `Pending`/`Processing` (riesgo conocido; reconciliar con el proveedor). Migración a EF Core = seguimiento.
- Commit tras cada tarea o grupo lógico; validar cada historia en su checkpoint.
- **FR-029 (marketplace, liquidar 100% al dueño)**: en el MVP con sandbox es un **no-op** (no hay movimiento real de fondos entre cuentas); la liquidación efectiva y la comisión de plataforma quedan **diferidas** (C6/Principio V). Solo se registra la atribución (T062a).
- **Canal "app" (I1)**: se considera cubierto por la página de resultado con polling (T036) más la notificación persistida por `InAppNotifier` (T022). Si más adelante se requiere un centro de notificaciones, añadir endpoint de lectura — fuera de alcance del MVP.
