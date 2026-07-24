# Phase 0 Research: Pagos reales, pago dividido y comprobantes

**Feature**: 002-payments-gateway · **Date**: 2026-07-24

Este documento resuelve las incógnitas técnicas del spec antes del diseño. Formato por decisión:
**Decisión / Justificación / Alternativas consideradas**.

---

## 1. Proveedor de pasarela de pago (Colombia)

**Decisión**: Usar **Wompi** (de Bancolombia) como proveedor de referencia, detrás de una
abstracción `IPaymentGateway` en la capa Application.

**Justificación**:
- Cobertura nativa de los métodos más usados en Colombia: **Nequi**, **PSE**, **botón/transferencia
  Bancolombia**, **tarjetas** de crédito/débito, y **Bancolombia QR**. Soporta el ecosistema de
  transferencias inmediatas (llaves Bre-B) en la medida que Bancolombia lo habilite.
- Modelo de integración simple y bien documentado: API REST de *transactions* + **eventos webhook
  firmados** + **Widget/Checkout Web** para el cobro. Tiene ambiente **sandbox**.
- Soporta **cuenta propia por comercio** (modelo cuenta directa) y esquemas de recaudo por
  plataforma, lo que habilita los dos modelos de recaudo del spec (FR-027).

**Alternativas consideradas**:
- **PayU / ePayco / Mercado Pago**: cobertura equivalente; se descartan como implementación inicial
  por preferencia del proveedor Bancolombia, pero la abstracción `IPaymentGateway` permite añadir
  cualquiera de ellos sin tocar Domain/Application (FR-010).
- Integración directa banco por banco: descartada por complejidad y mantenimiento (viola Principio III
  Simplicidad).

---

## 2. Flujo de pago asíncrono y confirmación

**Decisión**: Flujo **crear transacción → checkout del proveedor → confirmación por webhook**.
El estado `Paid`/aprobado del `Payment` se establece **solo** al procesar un evento de webhook
verificado, nunca en la respuesta síncrona del cliente (refuerza la Regla de Dominio 7).

**Justificación**:
- La verdad del pago la fija el proveedor de forma asíncrona (aprobado/rechazado/expirado).
- El endpoint de pago crea la transacción, deja el `Payment` en `Pending`/`Processing` y devuelve
  al frontend la información de checkout (URL de redirección o token de Widget).
- El frontend puede además **consultar el estado** (`GET /payments/{id}`) como respaldo (polling)
  mientras llega el webhook, pero el cambio de estado autoritativo proviene del webhook.

**Alternativas consideradas**:
- Marcar `Paid` con la respuesta síncrona del cliente: **descartada** — viola la Regla 7 y permite
  confirmaciones prematuras (SC-002).
- Solo polling sin webhook: descartada por latencia y por no ser la fuente de verdad recomendada.

---

## 3. Verificación de autenticidad e idempotencia del webhook

**Decisión**: Verificar la **firma/checksum** de cada evento (SHA-256 sobre las propiedades del
evento + timestamp + *events secret* del proveedor) antes de aplicar cambios. Registrar cada evento
procesado (`ProcessedWebhookEvent` por id de evento/transacción) para **idempotencia**: eventos
repetidos no cambian estado dos veces. Las transiciones de estado del `Payment` son además
**monótonas** (no se puede volver de `Refunded`/`Paid` a `Pending`).

**Justificación**: Cubre FR-005 (autenticidad), FR-006 (idempotencia), SC-003 (0 duplicados) y el
edge case de confirmación duplicada. El *events secret* se lee de configuración (FR-011).

**Alternativas consideradas**:
- Confiar en el estado sin verificar firma: descartada (riesgo de fraude).
- Idempotencia por *lock* distribuido: innecesario — la persistencia es un único proceso en memoria
  (Principio III).

---

## 4. Expiración de transacciones y liberación de franja

**Decisión**: La reserva queda `Pending` hasta la confirmación. Un **servicio en segundo plano en
proceso** (`IHostedService` `PaymentExpirySweeper`) barre periódicamente los pagos `Pending`/
`Processing` que superan el **plazo de expiración** (configurable, p. ej. 15 min) y los marca
`Expired`, liberando la franja (cancela la reserva no pagada).

**Justificación**: Cumple FR-008/FR-009 y SC-005 sin introducir colas de mensajes (Principio III):
un `IHostedService` es un temporizador en proceso, no una cola. Al recibir una aprobación tardía sobre
una franja ya liberada, se resuelve el conflicto reembolsando automáticamente (edge case).

**Alternativas consideradas**:
- Cola de mensajes / job scheduler externo (Hangfire, etc.): **descartada** — viola Principio III y
  Technical Constraints (single deployable unit).
- Expiración perezosa (al leer): insuficiente para liberar la franja proactivamente (SC-005).

---

## 5. Modelo de recaudo: cuenta directa vs. marketplace

**Decisión**: Configurable **por sede** (`Venue.SettlementMode` = `Direct` | `Marketplace`).
Un `IPaymentGatewayCredentialsResolver` selecciona las credenciales del proveedor según el modo:
- **Direct**: credenciales del comercio del **dueño** (almacenadas por sede).
- **Marketplace**: credenciales **centrales de la plataforma** (de configuración); se liquida el
  100% al dueño (sin comisión en el MVP, FR-029).

**Justificación**: Cumple FR-027/FR-028/FR-029 y la decisión del usuario (soportar ambos). La
resolución de credenciales queda aislada en Infrastructure; Domain/Application no conocen al proveedor.

**Alternativas consideradas**:
- Solo cuenta directa o solo marketplace: descartadas — el usuario pidió ambos.
- Comisión de plataforma en el MVP: **diferida** (documentada como extensión futura, Principio V).

---

## 6. Generación de comprobantes descargables

**Decisión**: Generar un **PDF** con **QuestPDF** (licencia MIT) mediante una abstracción
`IReceiptGenerator` en Application; la implementación vive en Infrastructure. El comprobante se
genera al confirmarse el pago y se puede consultar/descargar por endpoint autorizado.

**Justificación**: FR-019/FR-020/FR-021 piden un comprobante descargable con datos de la transacción.
QuestPDF es una única dependencia, simple y declarativa (Principio III). El acceso se restringe al
titular y al dueño de la sede (FR-022, SC-008).

**Alternativas consideradas**:
- HTML imprimible (sin PDF): válido y más simple, pero “descargar comprobante” se cumple mejor con un
  artefacto PDF estable. Se mantiene como fallback si se quiere evitar la dependencia.
- Servicios de PDF externos: descartados (Principio III, dependencia de red innecesaria).

---

## 7. Notificaciones (app + email + WhatsApp/SMS)

**Decisión**: Abstracción `INotificationSender` con **múltiples canales** (`InApp`, `Email`,
`WhatsAppSms`). En dev se usan implementaciones **stub/log**; en producción, adaptadores reales
detrás de la misma interfaz. Configuración (remitente, credenciales de proveedor de mensajería) en
`appsettings`/secretos (FR-011).

**Justificación**: FR-026/FR-026a piden notificar por tres canales. Mantener la abstracción evita
acoplar Domain/Application a SMTP/Twilio/WhatsApp Cloud API y respeta Clean Architecture. En el MVP
basta con app + stubs verificables; los adaptadores reales se activan por configuración.

**Alternativas consideradas**:
- Integrar directamente SMTP/Twilio en los servicios de aplicación: descartada (viola Clean
  Architecture y dificulta pruebas).
- Email/WhatsApp como requisito bloqueante del MVP: se mantienen como canales, pero la app es el canal
  garantizado; email/mensajería se habilitan por configuración (Principio V, sin bloquear el MVP).

---

## 8. Persistencia de estados de pago (en memoria)

**Decisión**: Mantener la **persistencia en memoria** actual (`InMemoryDatabase`) para el MVP,
añadiendo repositorios en memoria para `Receipt` y `ProcessedWebhookEvent`. Documentar como **riesgo
conocido** que un reinicio pierde transacciones `Pending`; recomendar migración a **EF Core** como
seguimiento (ya habilitada por las interfaces `IRepository`, sin tocar Domain/Application).

**Justificación**: Respeta Technical Constraints (persistencia en memoria) y Principio III. La
integridad del cobro real la garantiza el proveedor (fuente de verdad); ante pérdida de estado local,
el webhook/consulta al proveedor permite reconciliar.

**Alternativas consideradas**:
- Forzar EF Core + BD en esta feature: fuera de alcance del MVP de pagos; se deja como dependencia
  recomendada (Principio V).

---

## 9. Exposición del webhook en desarrollo

**Decisión**: El webhook (`POST /api/payments/webhook`) es **público** (sin JWT) y se protege por
verificación de firma. En desarrollo, exponer la API local con un túnel (p. ej. herramienta de
tunneling HTTPS) para recibir eventos del sandbox del proveedor.

**Justificación**: Los proveedores requieren una URL pública para notificar (Dependencies del spec).
La seguridad no depende del JWT sino de la firma del evento (FR-005).

**Alternativas consideradas**:
- Webhook autenticado con JWT: inviable — el proveedor no envía JWT.
- Solo polling en dev: aceptable como respaldo pero no reemplaza el webhook como fuente de verdad.

---

## Resumen de dependencias nuevas

| Dependencia | Capa | Propósito | Justificación constitucional |
|-------------|------|-----------|-------------------------------|
| SDK/HTTP a Wompi (vía `HttpClient`/`IHttpClientFactory`) | Infrastructure | Crear transacciones, consultar estado, reembolsos | Integración externa requerida (FR-001..FR-009) |
| QuestPDF | Infrastructure | Generar PDF de comprobantes | FR-019..FR-021; una sola lib simple |
| Adaptadores de email + WhatsApp/SMS (por configuración) | Infrastructure | Canales de notificación | FR-026; detrás de `INotificationSender` |
| `IHostedService` sweeper (in-process) | Api/Infrastructure | Expirar transacciones y liberar franjas | FR-008; no es cola de mensajes |

Todas las integraciones externas quedan detrás de abstracciones en Application; Domain no cambia sus
dependencias (Principios I y II).
