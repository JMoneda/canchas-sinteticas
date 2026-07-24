# Quickstart / Validation Guide: Pagos reales, pago dividido y comprobantes

**Feature**: 002-payments-gateway · **Date**: 2026-07-24

Guía para validar la feature de extremo a extremo. No contiene implementación; ver
[data-model.md](./data-model.md) y [contracts/payments.md](./contracts/payments.md) para detalles.

## Prerrequisitos

- Backend .NET en `dotnet-backend/` y frontend en `frontend/`.
- Cuenta **sandbox** del proveedor (Wompi) con `PublicKey`, `PrivateKey`, `EventsSecret`,
  `IntegritySecret`.
- Secretos cargados fuera del código:
  ```bash
  # desde dotnet-backend/CanchasSinteticas.Api
  dotnet user-secrets set "Payments:Wompi:PrivateKey" "prv_test_xxx"
  dotnet user-secrets set "Payments:Wompi:EventsSecret" "events_test_xxx"
  dotnet user-secrets set "Payments:Wompi:IntegritySecret" "integrity_test_xxx"
  ```
- Túnel HTTPS público apuntando al backend local, con la URL del webhook registrada en el panel del
  proveedor: `https://<tunnel>/api/payments/webhook`.

## Arranque

```bash
# Backend
cd dotnet-backend
dotnet run --project CanchasSinteticas.Api      # http://localhost:8080, Swagger en /swagger

# Frontend
cd frontend
npm install && npm run dev                      # http://localhost:5173
```

Cuentas demo cargadas por el seeder (Owner y Client). Iniciar sesión como Client para reservar.

## Escenarios de validación

### Escenario A — Pago real de reserva aprobado (User Story 1)
1. Crear una reserva (Client) sobre una franja libre.
2. `POST /api/reservations/{id}/pay` con `method: "nequi"` → respuesta `status: processing` + `checkout_url`.
3. Completar el pago en el checkout **sandbox** (aprobado).
4. El proveedor envía el webhook → `POST /api/payments/webhook`.
5. **Esperado**: `GET /api/payments/{id}` → `status: paid`, `gateway_reference` presente; la reserva
   queda confirmada; se generó comprobante (`has_receipt: true`). (SC-001, SC-002)

### Escenario B — Pago rechazado / expirado libera la franja (User Story 1 / edge cases)
1. Iniciar el pago y **rechazarlo** en sandbox (o dejar expirar el plazo).
2. **Esperado**: el pago queda `rejected`/`expired`, la reserva no se confirma y la franja vuelve a
   estar disponible sin intervención manual. (SC-005)

### Escenario C — Idempotencia del webhook (edge case / SC-003)
1. Reenviar el mismo evento aprobado dos veces al webhook.
2. **Esperado**: un único cambio a `paid`, un único comprobante; el segundo evento se ignora.

### Escenario D — Webhook no auténtico (FR-005)
1. Enviar un evento con firma/checksum inválido.
2. **Esperado**: no cambia ningún estado.

### Escenario E — Pago dividido entre jugadores (User Story 2)
1. Abrir un partido con `split` habilitado y `max_players` (p. ej. 4).
2. Cada jugador se une y hace `POST /api/matches/{id}/pay-share` → paga su parte en sandbox.
3. **Esperado**: `amount_collected` sube por cada parte aprobada; al cubrir el total, la reserva del
   partido queda confirmada. La suma de partes iguala el total exacto. (SC-006, FR-013)
4. Dejar una parte sin pagar hasta la fecha límite → **Esperado**: se reembolsan las partes pagadas y
   se libera la reserva. (FR-017)

### Escenario F — Comprobantes (User Story 3)
1. Tras un pago aprobado: `GET /api/reservations/{id}/receipt` → PDF descargable con referencia, monto,
   método, fecha, sede, cancha. (SC-007)
2. En split: `GET /api/matches/{id}/players/me/receipt` → PDF de la parte del jugador.
3. Intentar acceder al comprobante con otro usuario → **403**. (SC-008)

### Escenario G — Reembolso al cancelar (User Story 4)
1. Cancelar una reserva pagada **dentro** de la ventana permitida → `refund_status: refund_requested`
   → tras confirmación del proveedor, `refunded`. (SC-009)
2. Cancelar **tarde** → sin reembolso (`refund_status: none`).

### Escenario H — Modelo de recaudo por sede (FR-027)
1. `PUT /api/owner/venues/{id}/payment-config` con `settlement_mode: direct` + `gateway_merchant_ref`.
2. **Esperado**: los pagos de esa sede se crean con las credenciales del comercio del dueño; en
   `marketplace`, con las de la plataforma.

## Pruebas automatizadas (referencia)

- **Unit (Domain)**: transiciones de `Payment` (Regla 7: solo `Paid` tras confirmación), agregación de
  recaudo del `Match`, elegibilidad de reembolso, ajuste de redondeo de `PricePerPlayer`.
- **Application**: webhook idempotente con `IPaymentGateway` falso, resolución de credenciales por
  `SettlementMode`, expiración por el sweeper.
- **Api (integración)**: pagar → webhook aprobado → reserva confirmada; rechazo → franja liberada;
  control de acceso a comprobantes.

Criterio de aceptación global: todos los escenarios A–H pasan y las pruebas de dominio (Regla 7) están
en verde antes de considerar la feature completa.
