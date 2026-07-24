# Implementation Plan: Pagos reales, pago dividido y comprobantes

**Branch**: `002-payments-gateway` | **Date**: 2026-07-24 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/002-payments-gateway/spec.md`

## Summary

Reemplazar el pago simulado (`PaymentService` genera `SIM-...`; `MatchService` genera `SPLIT-...`) por
una integración real con una pasarela colombiana (**Wompi** de referencia), detrás de una abstracción
`IPaymentGateway`. El cobro es **asíncrono**: el endpoint de pago crea la transacción y devuelve un
checkout; el estado `Paid` solo se establece al procesar un **webhook verificado** (Regla de Dominio 7).
Se añade **pago dividido real** por jugador (un `Payment` por parte), **comprobantes PDF** descargables,
**reembolsos reales** integrados con la política de cancelación existente, **notificaciones** (app +
email + WhatsApp/SMS) y **recaudo configurable por sede** (cuenta directa del dueño o marketplace de la
plataforma). Persistencia se mantiene en memoria (con repositorios nuevos para comprobantes y eventos
de webhook); migración a EF Core queda como seguimiento recomendado.

## Technical Context

**Language/Version**: C# / .NET (backend); TypeScript + React (frontend, Vite)

**Primary Dependencies**: ASP.NET Core Web API; `HttpClient`/`IHttpClientFactory` (llamadas a Wompi);
QuestPDF (comprobantes PDF); adaptadores de email + WhatsApp/SMS por configuración; `IHostedService`
(sweeper de expiración). Frontend: React + Tailwind, cliente API existente.

**Storage**: En memoria (`InMemoryDatabase` + `ConcurrentDictionary`), vía interfaces `IRepository`.
Nuevos repos en memoria: `IReceiptRepository`, `IProcessedWebhookEventRepository`. Migración a EF Core:
diferida (habilitada por las interfaces, sin tocar Domain/Application).

**Testing**: xUnit para Domain (unit, TDD) y Application; pruebas de integración de API para los flujos
de pago/webhook/comprobante. Frontend: pruebas de componente básicas del flujo de checkout.

**Target Platform**: Servicio web (backend único desplegable) + SPA. Webhook requiere URL pública
(túnel HTTPS en dev).

**Project Type**: Web application (backend .NET + frontend React) — se reutiliza la estructura actual.

**Performance Goals**: Pago completable por el usuario en < 3 min (SC-004); liberación de franja
rechazada/expirada automática dentro del plazo de expiración (SC-005). Sin metas de alta concurrencia
para el MVP.

**Constraints**: Sin colas de mensajes, sin caché externa, un solo desplegable (Principio III y
Technical Constraints). Secretos por configuración, nunca en código (FR-011). Confirmación de pago solo
por el proveedor (Regla 7). JSON snake_case, multi-tenant por `OwnerId`.

**Scale/Scope**: MVP: 4 historias, ~29 FR. Sin comisión de marketplace (diferida). Bre-B según soporte
del proveedor.

## Constitution Check

*GATE: Debe pasar antes de Phase 0. Re-evaluado tras Phase 1.*

| Principio / Regla | Cumplimiento en este diseño |
|-------------------|-----------------------------|
| **I. Domain-First** | La máquina de estados del pago vive en `Payment` (métodos de dominio); la agregación de recaudo y el ajuste de redondeo viven en `Match`. Application solo orquesta. ✅ |
| **II. Clean Architecture + SOLID** | `IPaymentGateway`, `IReceiptGenerator`, `INotificationSender`, `IPaymentGatewayCredentialsResolver` en Application; implementaciones (Wompi, QuestPDF, email/WhatsApp) en Infrastructure. Dependencias hacia adentro; Domain no cambia sus dependencias. ✅ |
| **III. Simplicidad** | Un solo proveedor detrás de abstracción; sweeper en proceso (no cola); una sola lib de PDF; persistencia en memoria. Cada dependencia nueva justificada en research.md. ✅ |
| **IV. TDD del dominio** | Reglas nuevas (transiciones de `Payment`, Regla 7, recaudo, redondeo, elegibilidad de reembolso) con tests unitarios escritos primero. ✅ |
| **V. Disciplina MVP** | Comisión de plataforma y migración a BD explícitamente diferidas y documentadas. ✅ |
| **Regla de Dominio 7** | `MarkApproved` solo desde `Processing`/`Pending` y solo invocado por el manejador de webhook verificado; nunca en la respuesta síncrona. ✅ |

**Resultado**: PASA. Sin violaciones que requieran justificación en Complexity Tracking.

**Notas de dependencias nuevas** (todas detrás de abstracciones, justificadas en research.md):
QuestPDF y adaptadores de notificación son integraciones requeridas por FR-019..FR-021 y FR-026; no
introducen capas ni servicios prohibidos (sin microservicios ni colas).

## Project Structure

### Documentation (this feature)

```text
specs/002-payments-gateway/
├── plan.md              # Este archivo
├── spec.md              # Especificación
├── research.md          # Fase 0 — decisiones técnicas
├── data-model.md        # Fase 1 — entidades y transiciones
├── quickstart.md        # Fase 1 — guía de validación
├── contracts/
│   └── payments.md      # Fase 1 — contratos de API
└── checklists/
    └── requirements.md  # Calidad del spec
```

### Source Code (repository root)

```text
dotnet-backend/
├── CanchasSinteticas.Domain/
│   ├── Entities/         Payment (extender), Match/MatchPlayer (extender), Receipt (nuevo),
│   │                     ProcessedWebhookEvent (nuevo), Venue (SettlementMode)
│   ├── Enums/            PaymentStatus (extender), PaymentMethod (extender),
│   │                     SettlementMode (nuevo), NotificationChannel (nuevo)
│   └── Repositories/     IPaymentRepository (extender), IReceiptRepository (nuevo),
│                         IProcessedWebhookEventRepository (nuevo)
├── CanchasSinteticas.Application/
│   ├── Abstractions/     IPaymentGateway, IReceiptGenerator, INotificationSender,
│   │                     IPaymentGatewayCredentialsResolver (nuevos)
│   ├── Services/         PaymentService (reescribir), MatchService (reescribir pay-share),
│   │                     ReservationService (reembolso real), ReceiptService (nuevo),
│   │                     PaymentWebhookService (nuevo), VenuePaymentConfigService (nuevo)
│   └── DTOs/             PaymentDtos (extender), WebhookDtos, ReceiptDtos (nuevos)
├── CanchasSinteticas.Infrastructure/
│   ├── Payments/         WompiPaymentGateway, WompiSignatureVerifier,
│   │                     PaymentGatewayCredentialsResolver
│   ├── Receipts/         QuestPdfReceiptGenerator
│   ├── Notifications/    InAppNotifier, EmailNotifier (stub/real), WhatsAppSmsNotifier (stub/real)
│   ├── Repositories/     InMemoryReceiptRepository, InMemoryProcessedWebhookEventRepository
│   └── BackgroundJobs/   PaymentExpirySweeper (IHostedService)
└── CanchasSinteticas.Api/
    ├── Controllers/      PaymentsController (webhook + estado), ReservationsController (pay/receipt),
    │                     MatchesController (pay-share/receipt), OwnerVenuesController (payment-config)
    └── Program.cs        Registrar HttpClient, gateway, PDF, notificadores, sweeper, repos nuevos

frontend/
├── src/api/             client.ts (endpoints pay/estado/receipt/config), types.ts (nuevos tipos)
├── src/pages/           Checkout/Resultado de pago, MyReservationsPage (descargar comprobante),
│                        OpenMatchesPage (pagar parte + comprobante), Owner (config de recaudo)
└── src/components/      Selector de método de pago, estado de pago (polling), botón de comprobante
```

**Structure Decision**: Web application. Se reutiliza la Clean Architecture existente de
`dotnet-backend/` (4 proyectos) y la SPA de `frontend/`. Toda la lógica de negocio de pagos se añade en
Domain/Application; las integraciones externas (Wompi, PDF, notificaciones) en Infrastructure; los
endpoints en Api. No se crean proyectos nuevos (Principio III).

## Complexity Tracking

> Sin violaciones de la constitución. Tabla no aplica.

## Phasing sugerido para tasks (orientativo, lo detalla `/speckit-tasks`)

1. **Dominio (TDD)**: enums y transiciones de `Payment`, `Receipt`, `ProcessedWebhookEvent`, ajuste de
   recaudo/redondeo en `Match`, `SettlementMode` en `Venue`. Tests primero.
2. **Abstracciones + Infra**: `IPaymentGateway` + `WompiPaymentGateway` (sandbox), verificación de
   firma, resolver de credenciales, repos en memoria nuevos.
3. **Flujo de pago de reserva**: reescribir `PaymentService`, `PaymentWebhookService`, endpoints de pago
   y estado, sweeper de expiración.
4. **Pago dividido**: reescribir `MatchService.PayShare` con `Payment` por parte + confirmación.
5. **Comprobantes**: `ReceiptService` + `QuestPdfReceiptGenerator` + endpoints de descarga + control de
   acceso.
6. **Reembolsos**: reembolso real en `ReservationService.Cancel` + seguimiento de estado.
7. **Notificaciones**: `INotificationSender` + canales (app garantizado; email/WhatsApp por config).
8. **Config de recaudo**: `VenuePaymentConfigService` + endpoint owner.
9. **Frontend**: flujo de checkout, polling de estado, pago de parte, descarga de comprobantes, config
   de recaudo del dueño.
10. **Integración**: pruebas de API de los escenarios A–H del quickstart.
```
