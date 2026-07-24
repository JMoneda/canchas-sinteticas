# API Contracts: Pagos, pago dividido, comprobantes y webhook

**Feature**: 002-payments-gateway · **Date**: 2026-07-24

Convenciones del proyecto: JSON en **snake_case**, JWT Bearer (salvo el webhook, que es público y se
verifica por firma). Errores de dominio se mapean vía `DomainExceptionMiddleware` a códigos HTTP.
Todas las rutas nuevas o modificadas se listan aquí.

---

## 1. Iniciar pago de una reserva (MODIFICADO)

`POST /api/reservations/{id}/pay` — rol: `Client` (titular de la reserva).

Antes marcaba el pago como pagado (simulado). **Ahora** crea la transacción en el proveedor y devuelve
la información de checkout; el pago queda `processing`.

**Request**
```json
{ "method": "nequi", "return_url": "https://app.example.com/reservas/{id}/resultado" }
```
`method` ∈ `card | nequi | pse | bancolombia_transfer | bancolombia_button | bancolombia_qr`.
`return_url` (opcional) para el retorno del checkout.

**Response 200**
```json
{
  "payment_id": "pay_123",
  "reservation_id": "res_123",
  "status": "processing",
  "amount": 120000,
  "method": "nequi",
  "checkout_url": "https://checkout.wompi.co/l/abc123",
  "expires_at": "2026-07-24T15:15:00"
}
```
El frontend redirige a `checkout_url` (o abre el Widget). **No** se confirma el pago aquí.

**Errores**: 401/403 (no titular), 404 (reserva/pago), 409 (ya pagado / estado inválido),
502 (`Failed` — el proveedor no respondió; la franja no queda bloqueada).

---

## 2. Consultar estado de un pago (NUEVO)

`GET /api/payments/{id}` — rol: `Client` titular o `Owner` de la sede.

Respaldo de *polling* mientras llega el webhook.

**Response 200**
```json
{
  "payment_id": "pay_123",
  "reservation_id": "res_123",
  "status": "paid",
  "amount": 120000,
  "method": "nequi",
  "gateway_reference": "WOMPI-TX-9F2A...",
  "paid_at": "2026-07-24T15:03:11",
  "has_receipt": true
}
```

---

## 3. Pagar la parte de un partido dividido (MODIFICADO)

`POST /api/matches/{id}/pay-share` — rol: `Client` inscrito en el partido.

Antes marcaba `HasPaid` (simulado). **Ahora** crea una transacción por la **parte** del jugador.

**Request**
```json
{ "method": "nequi", "return_url": "https://app.example.com/partidos/{id}/resultado" }
```

**Response 200**
```json
{
  "payment_id": "pay_share_77",
  "match_id": "match_9",
  "payer_user_id": "usr_5",
  "status": "processing",
  "amount": 30000,
  "checkout_url": "https://checkout.wompi.co/l/def456",
  "expires_at": "2026-07-24T15:15:00"
}
```

**Errores**: 403 (no inscrito), 404, 409 (parte ya pagada / split no habilitado).

---

## 4. Webhook del proveedor (NUEVO — público)

`POST /api/payments/webhook` — **sin JWT**. Autenticidad por **firma/checksum** del proveedor.

**Request** (forma Wompi, resumida)
```json
{
  "event": "transaction.updated",
  "data": { "transaction": { "id": "WOMPI-TX-9F2A", "status": "APPROVED", "reference": "res_123", "amount_in_cents": 12000000 } },
  "sent_at": "2026-07-24T15:03:10Z",
  "timestamp": 1753370590,
  "signature": { "checksum": "d3f...", "properties": ["data.transaction.id","data.transaction.status","data.transaction.amount_in_cents"] }
}
```

**Comportamiento**:
1. Verificar checksum (SHA-256 sobre las `properties` + `timestamp` + *events secret*). Si falla → **200/400 sin cambiar estado** (FR-005).
2. Idempotencia: si el evento ya está en `ProcessedWebhookEvent` → responder 200 sin reprocesar (FR-006).
3. Aplicar transición al `Payment` (`APPROVED→Paid`, `DECLINED→Rejected`, `VOIDED/refund→Refunded`, etc.).
4. Si aprueba una reserva → confirmar reserva y **generar comprobante** + notificar (app/email/WhatsApp).
5. Si aprueba una parte de split → recalcular recaudo; si cubre el total, confirmar la reserva del partido.

**Response**: `200 OK` siempre que el evento se reciba y procese (o se ignore por idempotencia). Nunca
expone detalles internos.

---

## 5. Descargar comprobante de reserva (NUEVO)

`GET /api/reservations/{id}/receipt` — rol: `Client` titular o `Owner` de la sede.

**Response 200**: `application/pdf` (adjunto). 404 si no hay comprobante (pago no aprobado).
403 si el solicitante no es el titular ni el dueño (SC-008).

Variante de datos (para render en la app): `GET /api/reservations/{id}/receipt?format=json` →
```json
{
  "number": "REC-000123",
  "amount": 120000,
  "method": "nequi",
  "gateway_reference": "WOMPI-TX-9F2A",
  "issued_at": "2026-07-24T15:03:12",
  "venue_name": "Complejo La 80",
  "court_name": "Cancha 1",
  "payer_name": "Juan Pérez"
}
```

---

## 6. Descargar comprobante de una parte de split (NUEVO)

`GET /api/matches/{id}/players/me/receipt` — rol: `Client` inscrito (su propia parte).

**Response 200**: `application/pdf` de la parte del jugador (nombre + monto de su parte). 404 si su
parte no está pagada; 403 si no está inscrito.

---

## 7. Cancelar reserva con reembolso real (MODIFICADO)

`POST /api/reservations/{id}/cancel` — rol: `Client` titular.

Se mantiene la política existente (tardía = sin reembolso). **Ahora**, si procede reembolso, se
solicita al proveedor y el pago pasa a `refund_requested` → `refunded` al confirmarse.

**Response 200**
```json
{ "reservation_id": "res_123", "status": "cancelled", "is_late": false, "refund_status": "refund_requested" }
```
`refund_status` ∈ `none | refund_requested | refunded`.

---

## 8. Configurar recaudo de la sede (NUEVO)

`PUT /api/owner/venues/{id}/payment-config` — rol: `Owner` dueño de la sede.

**Request**
```json
{ "settlement_mode": "direct", "gateway_merchant_ref": "MERCHANT-OWNER-42" }
```
`settlement_mode` ∈ `direct | marketplace`. En `direct`, `gateway_merchant_ref` identifica el comercio
del dueño (las credenciales/secretos se cargan de configuración segura, no por este endpoint).

**Response 200**
```json
{ "venue_id": "ven_1", "settlement_mode": "direct", "gateway_merchant_ref": "MERCHANT-OWNER-42" }
```

**Errores**: 403 (no dueño), 404, 422 (config inválida, p. ej. `direct` sin `gateway_merchant_ref`).

---

## Configuración (appsettings) — nueva sección `Payments`

```json
{
  "Payments": {
    "Provider": "Wompi",
    "ExpiryMinutes": 15,
    "Wompi": {
      "BaseUrl": "https://sandbox.wompi.co/v1",
      "PublicKey": "pub_test_xxx",
      "PrivateKey": "prv_test_xxx",
      "EventsSecret": "events_test_xxx",
      "IntegritySecret": "integrity_test_xxx"
    },
    "Notifications": {
      "Email": { "Enabled": false },
      "WhatsAppSms": { "Enabled": false }
    }
  }
}
```
Los secretos NO se versionan: se inyectan por *user-secrets*/variables de entorno en dev y por el
gestor de secretos del entorno en producción (FR-011).
