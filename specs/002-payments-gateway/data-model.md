# Phase 1 Data Model: Pagos reales, pago dividido y comprobantes

**Feature**: 002-payments-gateway · **Date**: 2026-07-24

Modelo de dominio para la feature. Se **extienden** entidades existentes y se **añaden** nuevas,
todas en `CanchasSinteticas.Domain`. Los montos son `decimal` (COP). Toda transición de estado vive
en el dominio (Principio I) y solo cambia a *aprobado* tras confirmación del proveedor (Regla 7).

---

## Enums

### PaymentStatus (extendido)

Estado actual: `Pending, Paid, Refunded, Failed`. Nuevo conjunto:

| Valor | Significado | Origen de la transición |
|-------|-------------|-------------------------|
| `Pending` | Creado, aún sin intento de cobro confirmado | inicial |
| `Processing` | Transacción creada en el proveedor, esperando confirmación | al crear transacción |
| `Paid` | Aprobado por el proveedor (confirmación verificada) | webhook aprobado (Regla 7) |
| `Rejected` | Rechazado por el proveedor | webhook rechazado |
| `Expired` | Venció el plazo sin aprobación | sweeper de expiración |
| `RefundRequested` | Reembolso solicitado al proveedor, sin confirmar | cancelación válida |
| `Refunded` | Reembolso confirmado por el proveedor | webhook/confirmación de reembolso |
| `Failed` | Error de comunicación/técnico | error al crear transacción |

**Transiciones válidas** (monótonas, sin retroceso salvo la conciliación explícita):
`Pending → Processing → {Paid | Rejected | Expired}`;
`Paid → RefundRequested → {Refunded | Paid}` (si el proveedor rechaza el reembolso vuelve a `Paid`);
`Pending/Processing → Failed`.

**Conciliación de aprobación tardía tras expirar** (edge case): si el proveedor aprueba una
transacción cuyo `Payment` ya está `Expired`, NO se hace `Expired → Paid` automáticamente. El
`PaymentWebhookService` decide:
- si la franja **sigue libre** → `Reactivate()` (reactiva la reserva y transiciona el pago a `Paid`);
- si la franja **ya no está libre** → se solicita **reembolso automático** (`Expired → RefundRequested → Refunded`).
Esto evita doble reserva y dinero cobrado sin cupo.

### PaymentMethod (extendido)

Actual: `OnlineGateway, Cash`. Nuevo (instrumentos reales de Colombia):

`Cash, Card, Nequi, Pse, BancolombiaTransfer, BancolombiaButton, BancolombiaQr`
(`OnlineGateway` se conserva por compatibilidad como genérico; los nuevos valores lo especifican).

### SettlementMode (nuevo)

`Direct` (cuenta del dueño) · `Marketplace` (cuenta central de la plataforma).

### NotificationChannel (nuevo)

`InApp, Email, WhatsAppSms`.

---

## Entidades

### Payment (extendida)

Representa el intento y resultado de cobro de **una reserva completa** o de **una parte de pago
dividido**.

| Campo | Tipo | Notas |
|-------|------|-------|
| `Id` | string | PK |
| `ReservationId` | string | reserva asociada |
| `MatchId` | string? | presente si es el pago de una **parte** de partido dividido |
| `PayerUserId` | string? | jugador que paga la parte (split); null en pago de reserva completa |
| `Amount` | decimal | monto (COP) |
| `Method` | PaymentMethod | instrumento elegido |
| `Status` | PaymentStatus | estado (ver transiciones) |
| `GatewayTransactionId` | string? | id de transacción del proveedor |
| `GatewayReference` | string? | referencia/comprobante del proveedor (reemplaza `SIM-...`) |
| `GatewayStatusRaw` | string? | estado crudo informado por el proveedor (auditoría) |
| `CheckoutUrl` | string? | URL/token de checkout para el frontend |
| `RefundReference` | string? | referencia del reembolso |
| `CreatedAt` | DateTime | |
| `PaidAt` | DateTime? | fecha de aprobación |
| `ExpiresAt` | DateTime? | límite para completar el pago |

**Métodos de dominio** (reemplazan/añaden a `MarkPaid`/`Refund`):
`StartProcessing(txnId, checkoutUrl, expiresAt)`, `MarkApproved(txnId, reference, paidAt)`,
`MarkRejected(raw)`, `MarkExpired()`, `RequestRefund()`, `ConfirmRefund(refundRef)`, `FailRefund()`,
`Fail(raw)`. Cada método valida la transición y lanza `ValidationError`/estado-inválido si no aplica
(idempotente: reaplicar el mismo estado terminal no falla ni duplica efectos).

**Reglas de validación**:
- `Amount > 0`.
- `MarkApproved` solo desde `Processing` (o `Pending`) — nunca de forma optimista (Regla 7).
- Reembolso solo desde `Paid`.
- Si `MatchId`/`PayerUserId` están presentes, ambos deben estarlo (pago de parte).

### Reservation (sin cambios estructurales)

Su estado depende del resultado del pago; la confirmación la orquesta Application al recibir el
webhook. Se mantiene `Cancel(isLate)`.

### Match / MatchPlayer (extendidos)

- **MatchPlayer**: se conserva `HasPaid`/`MarkPaid()`, pero ahora el pago de la parte se respalda con
  un `Payment` (con `MatchId` + `PayerUserId`). Se añade `PaymentId` (string?) para enlazar la parte
  con su `Payment`/comprobante.
- **Match**: `AmountCollected` se recalcula a partir de las partes con `Payment` en estado `Paid`
  (no solo el booleano). Se añade `SettlementDeadline` (DateTime) para la política de expiración del
  recaudo (FR-017). Método `RefundAllShares()` (marca las partes para reembolso al expirar).
- Regla: `PricePerPlayer` se ajusta para que `PricePerPlayer * MaxPlayers == TotalPrice` exacto
  (el redondeo se corrige en la última parte) — FR-013.

### Receipt (nueva)

Comprobante inmutable generado al aprobarse un pago (FR-019/FR-020).

| Campo | Tipo | Notas |
|-------|------|-------|
| `Id` | string | PK |
| `Number` | string | consecutivo legible del comprobante |
| `PaymentId` | string | pago asociado |
| `ReservationId` | string | reserva |
| `MatchId` | string? | si es parte de partido dividido |
| `PayerUserId` | string? | jugador (split) |
| `PayerName` | string | nombre en el comprobante |
| `Amount` | decimal | monto |
| `Method` | string | método usado |
| `GatewayReference` | string | referencia del proveedor |
| `VenueName` | string | sede (snapshot) |
| `CourtName` | string | cancha (snapshot) |
| `IssuedAt` | DateTime | fecha de emisión |

Los datos son un **snapshot** al momento de emisión (no se recalculan después). Acceso restringido al
titular (`PayerUserId`/cliente de la reserva) y al dueño de la sede (FR-022).

### ProcessedWebhookEvent (nueva)

Soporte de idempotencia (FR-006).

| Campo | Tipo | Notas |
|-------|------|-------|
| `EventId` | string | id del evento del proveedor (PK) |
| `GatewayTransactionId` | string | transacción referida |
| `ReceivedAt` | DateTime | primera vez procesado |

Antes de aplicar un evento se consulta este registro; si ya existe, se ignora (idempotente).

### Venue (extendida — configuración de recaudo)

| Campo | Tipo | Notas |
|-------|------|-------|
| `SettlementMode` | SettlementMode | `Direct` \| `Marketplace` (default `Marketplace`) |
| `GatewayMerchantRef` | string? | identificador del comercio del dueño (modo `Direct`) |

Las **credenciales/secretos** del comercio NO se guardan en la entidad de dominio: se resuelven en
Infrastructure vía `IPaymentGatewayCredentialsResolver` desde configuración segura por `Venue`/plataforma
(FR-011). La entidad solo guarda una referencia no sensible.

---

## Repositorios (interfaces en Domain)

- `IPaymentRepository` (existente): añadir `GetById`, `GetByGatewayTransactionId`,
  `GetByMatchAndPayer(matchId, userId)`, `GetSharesByMatch(matchId)`.
- `IReceiptRepository` (nuevo): `Add`, `GetById`, `GetByPayment`, `GetByReservation`.
- `IProcessedWebhookEventRepository` (nuevo): `Exists(eventId)`, `Add`.

Implementaciones en memoria en Infrastructure (Technical Constraints).

---

## Diagrama de relaciones (resumen)

```text
Venue (SettlementMode) 1───* Court 1───* Reservation 1───1 Payment ──*── Receipt
                                              │                 ▲
                                              │                 │ (parte)
                                          Match 1───* MatchPlayer ─┘
                                              (cada parte pagada = un Payment con MatchId+PayerUserId)
Payment 1───* (via GatewayTransactionId) ──> ProcessedWebhookEvent (idempotencia)
```

Notas:
- Pago de reserva completa: `Payment` con `MatchId=null`.
- Pago dividido: un `Payment` por parte (`MatchId` + `PayerUserId`); la reserva se confirma cuando la
  suma de partes `Paid` cubre `Reservation.TotalPrice`.
- Cada `Payment` aprobado genera exactamente un `Receipt`.
