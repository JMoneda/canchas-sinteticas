# Canchas Sintéticas — API (.NET)

API REST de una **plataforma multi-tenant** para reservar canchas de fútbol sintético: marketplace
para clientes y panel de gestión para dueños. Incluye pagos reales (pasarela de Colombia), partidos
abiertos con pago dividido y comprobantes.

## 🧱 Stack técnico

- **.NET 10** · ASP.NET Core Web API
- **Clean Architecture** en 4 proyectos: `Domain`, `Application`, `Infrastructure`, `Api`
  (solución `CanchasSinteticas.slnx`)
- **Persistencia en memoria** (`InMemoryDatabase` con `ConcurrentDictionary`). Las interfaces
  `IRepository` permiten migrar a EF Core sin tocar `Domain`/`Application`. *No usa SQLite ni una BD
  externa.*
- **JWT Bearer** (HMAC-SHA256), contraseñas con **PBKDF2**
- **JSON en snake_case** · **Swagger/Swashbuckle**
- **Multi-tenant** por `OwnerId` (Owner → Venue → Court)
- Pruebas con **xUnit** (`CanchasSinteticas.Tests`)

## 🚀 Ejecutar

```bash
cd dotnet-backend
dotnet run --project CanchasSinteticas.Api    # http://localhost:8080 · Swagger en /swagger
dotnet test                                   # pruebas de dominio y de aplicación
```

CORS autoriza por defecto el frontend en `http://localhost:5173`.

### Datos de demostración

Al arrancar, si el almacén está vacío, se cargan datos de ejemplo. Contraseña de todas las cuentas:
`password123`.

| Rol | Correo |
|-----|--------|
| Owner | `owner1@canchas.co`, `owner2@canchas.co` |
| Client | `cliente@canchas.co` |
| SuperAdmin | `admin@canchas.co` |

Incluye sedes en Bogotá y Medellín con canchas (tarifas diurno/nocturno), reservas de ejemplo y un
partido abierto con pago dividido.

## 🔐 Autenticación

JWT Bearer. Obtén un token con `POST /api/auth/login` y envíalo como `Authorization: Bearer <token>`.
Roles: `SuperAdmin`, `Owner`, `Client`.

## 📡 Endpoints

### Auth
| Método | Ruta | Acceso | Descripción |
|--------|------|--------|-------------|
| POST | `/api/auth/register` | público | Registra cuenta (Owner/Client) y devuelve token |
| POST | `/api/auth/login` | público | Inicia sesión |
| GET | `/api/auth/me` | autenticado | Perfil del usuario |

### Marketplace (cliente)
| Método | Ruta | Acceso | Descripción |
|--------|------|--------|-------------|
| GET | `/api/venues?city=` | público | Busca sedes activas (opcional por ciudad) |
| GET | `/api/venues/{id}` | público | Detalle de sede con sus canchas |
| GET | `/api/courts/{id}/availability?date=` | público | Slots de una cancha para una fecha |

### Reservas (cliente)
| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/api/reservations` | Crea una reserva (queda `pending` de pago) |
| GET | `/api/reservations` | Lista las reservas del cliente |
| POST | `/api/reservations/{id}/pay` | Inicia el pago (devuelve `checkout_url`) |
| GET | `/api/reservations/{id}/receipt` | Comprobante (PDF; `?format=json` para datos) |
| DELETE | `/api/reservations/{id}` | Cancela y reembolsa según política (`refund_status`) |

### Pagos
| Método | Ruta | Acceso | Descripción |
|--------|------|--------|-------------|
| GET | `/api/payments/{id}` | titular/dueño | Estado del pago (polling) |
| POST | `/api/payments/webhook` | **público** | Eventos del proveedor (firma verificada) |

### Partidos abiertos (matchmaking)
| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/matches?city=` | Lista partidos con cupos |
| GET | `/api/matches/{id}` | Detalle del partido |
| POST | `/api/matches` | Abre un partido (crea la reserva y lo publica) |
| POST | `/api/matches/{id}/join` | Unirse |
| POST | `/api/matches/{id}/leave` | Salir (reembolsa la parte si ya pagó) |
| POST | `/api/matches/{id}/pay-share` | Paga la parte del jugador (pago dividido) |
| GET | `/api/matches/{id}/players/me/receipt` | Comprobante de la parte del jugador |

### Panel del dueño (`Owner`)
| Método | Ruta | Descripción |
|--------|------|-------------|
| GET/POST | `/api/owner/venues` | Lista / crea sedes |
| PUT/DELETE | `/api/owner/venues/{id}` | Actualiza / elimina sede |
| GET/PUT | `/api/owner/venues/{id}/payment-config` | Modelo de recaudo (`marketplace`/`direct`) |
| GET/POST | `/api/owner/venues/{id}/courts` | Lista / crea canchas |
| PUT/DELETE | `/api/owner/courts/{id}` | Actualiza / elimina cancha |
| PUT | `/api/owner/courts/{id}/prices` | Define reglas de precio |
| GET/POST | `/api/owner/courts/{id}/blackouts` | Bloqueos de agenda |
| DELETE | `/api/owner/blackouts/{id}` | Elimina un bloqueo |
| GET | `/api/owner/reservations?date=` | Agenda de reservas del dueño |
| POST | `/api/owner/reservations` | Reserva manual (walk-in / teléfono) |
| GET | `/api/owner/reports?from=&to=` | Reporte de ocupación e ingresos |

La documentación interactiva completa está en **Swagger** (`/swagger`).

## 📐 Reglas de dominio (con pruebas)

1. Una cancha no puede tener dos reservas que se solapen en la misma franja.
2. Reservas de mínimo 1 hora, en bloques de la duración de la cancha.
3. Horario de operación **por sede** (opening/closing de cada Venue).
4. Anticipación mínima de 1 hora para reservar.
5. Máximo 2 reservas activas por cliente.
6. Cancelación fuera de la ventana de la sede = tardía (no-show, sin reembolso).
7. Una reserva (o parte de pago dividido) solo se marca **pagada** tras la confirmación verificada del
   proveedor por webhook — nunca de forma optimista.

## 🗂️ Estructura

```text
dotnet-backend/
├── CanchasSinteticas.Domain/          # Entidades, enums, value objects, políticas, interfaces repo
├── CanchasSinteticas.Application/     # Casos de uso, DTOs, abstracciones (IPaymentGateway, etc.)
├── CanchasSinteticas.Infrastructure/  # Persistencia en memoria, Wompi, QuestPDF, notificadores, seed
├── CanchasSinteticas.Api/             # Controllers, JWT, middleware, background jobs, Program.cs
└── CanchasSinteticas.Tests/           # xUnit
```

---

## 💳 Pagos (feature 002-payments-gateway)

Integración de pasarela real (**Wompi**, detrás de `IPaymentGateway`), pago dividido entre jugadores,
comprobantes en PDF y reembolsos. Cumple la **Regla de Dominio 7**: el pago solo se aprueba tras la
confirmación verificada del proveedor por webhook, nunca de forma optimista.

**Flujo:** crear reserva (`pending`) → `POST /pay` crea la transacción y devuelve `checkout_url`
(pago `processing`) → el cliente paga en el checkout del proveedor → el **webhook** confirma → la
reserva pasa a `confirmed`, se genera el comprobante y se notifica. Rechazo/expiración libera la
franja (barrido en segundo plano cada 30 s).

Métodos soportados (Colombia): `nequi`, `pse`, `bancolombia_transfer`, `bancolombia_button`,
`bancolombia_qr`, `card`.

### Configuración

Sección `Payments` de `appsettings.json` (valores no sensibles): `Provider`, `ExpiryMinutes`,
`Wompi.BaseUrl/PublicKey`, `Notifications.Email/WhatsAppSms.Enabled`.

Los secretos se cargan **fuera del código**:

```bash
cd CanchasSinteticas.Api
dotnet user-secrets set "Payments:Wompi:PrivateKey"      "prv_test_xxx"
dotnet user-secrets set "Payments:Wompi:EventsSecret"    "events_test_xxx"
dotnet user-secrets set "Payments:Wompi:IntegritySecret" "integrity_test_xxx"
```

En producción, por variables de entorno / gestor de secretos. Para recibir webhooks en desarrollo,
exponer la API con un túnel HTTPS y registrar `https://<túnel>/api/payments/webhook`.

### Modelo de recaudo (por sede)

- **`marketplace`** (por defecto): la plataforma recauda y liquida el 100% al dueño (sin comisión en
  el MVP).
- **`direct`**: el dueño recauda con su propia cuenta del proveedor (`gateway_merchant_ref`).

### Seguridad

- El webhook valida la **firma (checksum SHA-256 + events secret)**; los eventos no auténticos no
  cambian ningún estado.
- Procesamiento **idempotente** (`ProcessedWebhookEvent`): reenvíos no duplican cobros ni
  confirmaciones.
- Comprobantes accesibles solo por el titular del pago y el dueño de la sede.
- Ningún secreto en código ni en logs; el webhook no expone detalles internos.

### Notas de MVP

- Persistencia en memoria: un reinicio pierde pagos `pending`/`processing` (reconciliables con el
  proveedor). Migración a EF Core habilitada por las interfaces `IRepository`.
- Correo y WhatsApp/SMS: activables por `Payments:Notifications` (adaptador de envío real por
  conectar).
- Comisión de plataforma en marketplace: fuera del alcance del MVP.

## 📄 Licencia

Proyecto de uso privado. Todos los derechos reservados.
