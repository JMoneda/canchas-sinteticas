# Canchas Sintéticas

Plataforma **multi-tenant** de reserva de canchas de fútbol sintético. Funciona como un
**marketplace** para clientes (buscar sedes, ver disponibilidad en tiempo real, reservar y pagar)
y como un **panel SaaS** para dueños (registrar sedes, dar de alta canchas, configurar tarifas por
franja horaria, bloqueos, agenda y reportes). Todo sobre una misma API.

> **Nota de arquitectura:** la persistencia es **en memoria** (sin base de datos por ahora). Los
> repositorios viven detrás de interfaces (`IVenueRepository`, `IReservationRepository`, …), así que
> enchufar EF Core / SQL Server más adelante no requiere tocar el dominio ni la capa de aplicación.

## Requisitos

| Herramienta | Versión mínima | Uso |
|-------------|----------------|-----|
| .NET SDK    | 10.0+          | Backend |
| Node.js     | 18+            | Frontend |

---

## Inicio rápido

### 1. Backend (.NET)

```bash
cd dotnet-backend
dotnet run --project CanchasSinteticas.Api/CanchasSinteticas.Api.csproj
```

- API: `http://localhost:8080`
- Swagger UI (con botón *Authorize* para el JWT): `http://localhost:8080/swagger`

Al arrancar se cargan datos de demostración en memoria (2 dueños, un cliente, 3 sedes en Bogotá y
Medellín, canchas con tarifas diurno/nocturno y reservas de ejemplo).

### 2. Frontend (React + TypeScript + Tailwind)

```bash
cd frontend
npm install
npm run dev
```

App: `http://localhost:5173`

La URL de la API se configura en `frontend/.env` (`VITE_API_URL`).

---

## Cuentas de demostración

Contraseña para todas: **`password123`**

| Rol | Correo | Qué puede hacer |
|-----|--------|-----------------|
| Cliente | `cliente@canchas.co` | Buscar sedes, reservar, pagar, cancelar |
| Dueño | `owner1@canchas.co` | Gestionar sus sedes/canchas/tarifas, agenda, reportes |
| Dueño | `owner2@canchas.co` | (otra organización — no ve las sedes del dueño 1) |

---

## Arquitectura

Clean Architecture / hexagonal en 4 proyectos:

```
dotnet-backend/
├── CanchasSinteticas.Domain/          # Entidades, enums, value objects, servicios e interfaces de repos
│   ├── Entities/                      #   User, Venue, Court, PriceRule, Blackout, Reservation, Payment
│   ├── Enums/                         #   UserRole, CourtType, ReservationStatus, ...
│   ├── ValueObjects/                  #   TimeSlot, GeoLocation
│   ├── Services/                      #   PricingCalculator (precio por franja)
│   ├── Repositories/                  #   Interfaces (I*Repository)
│   └── Exceptions/                    #   DomainException + subclases tipadas
├── CanchasSinteticas.Application/     # Casos de uso (servicios), DTOs, abstracciones
│   ├── Abstractions/                  #   IClock, IPasswordHasher, ITokenService
│   ├── Services/                      #   AuthService, VenueService, CourtService, AvailabilityService,
│   │                                  #   ReservationService, BlackoutService, PaymentService, ReportService
│   ├── DTOs/
│   └── Common/                        #   Parsing, Mappers, Ownership (guard multi-tenant)
├── CanchasSinteticas.Infrastructure/  # Persistencia en memoria, hashing, reloj, seed
│   ├── Persistence/                   #   InMemoryDatabase (ConcurrentDictionary)
│   ├── Repositories/                  #   InMemory*Repository
│   ├── Security/                      #   Pbkdf2PasswordHasher
│   ├── Time/                          #   SystemClock
│   └── Seed/                          #   DatabaseSeeder
└── CanchasSinteticas.Api/             # Controllers, middleware, JWT, Program.cs
    ├── Auth/                          #   JwtTokenService
    ├── Controllers/
    └── Middleware/                    #   DomainExceptionMiddleware
```

```
frontend/
└── src/
    ├── api/            # client.ts (fetch + JWT), types.ts (DTOs)
    ├── auth/           # AuthContext, ProtectedRoute
    ├── components/     # Layout, ui (Button, Card, Field, ...)
    ├── lib/            # useAsync, format (COP, etiquetas)
    └── pages/
        ├── MarketplacePage / VenueDetailPage        # cara cliente (marketplace + wizard de reserva)
        ├── LoginPage / RegisterPage / MyReservationsPage
        └── OwnerDashboardPage / OwnerVenuesPage /
            OwnerVenueDetailPage / OwnerAgendaPage    # cara dueño (panel)
```

---

## Modelo de dominio

```
User (rol: SuperAdmin | Owner | Client)
Owner 1───N Venue (sede) 1───N Court (cancha) 1───N PriceRule (tarifa por franja)
                                              1───N Blackout (bloqueo)
                                              1───N Reservation N───1 Client
Reservation 1───1 Payment
```

---

## Autenticación

JWT (HMAC-SHA256). Registro/login devuelven un token; el rol viaja como claim y protege los
endpoints (`[Authorize(Roles = "Owner")]`). Contraseñas con PBKDF2. Config en `appsettings.json` → `Jwt`.

```
POST /api/auth/register   { name, email, phone?, password, role }  → token
POST /api/auth/login      { email, password }                       → token
GET  /api/auth/me                                                   → perfil
```

---

## API

### Público / cliente

```
GET    /api/venues?city=Bogotá                       Buscar sedes (marketplace)
GET    /api/venues/{venueId}                          Detalle de sede + canchas
GET    /api/courts/{courtId}/availability?date=       Disponibilidad (slots con precio y estado)
POST   /api/reservations                              Crear reserva            (cliente)
GET    /api/reservations                              Mis reservas             (cliente)
DELETE /api/reservations/{id}                         Cancelar reserva         (cliente)
POST   /api/reservations/{id}/pay                     Pagar (simulado)         (cliente)
```

### Partidos abiertos (matchmaking)

```
GET    /api/matches?city=                             Listar partidos con cupos (público)
GET    /api/matches/{id}                               Detalle de un partido     (público)
POST   /api/matches                                    Abrir partido (crea reserva + publica cupos) (cliente)
POST   /api/matches/{id}/join                          Unirse a un partido       (cliente)
POST   /api/matches/{id}/leave                         Salir de un partido       (cliente)
```

### Panel del dueño (`[Authorize(Roles = "Owner")]`)

```
GET/POST        /api/owner/venues                     Listar / crear sedes
PUT/DELETE      /api/owner/venues/{id}                Editar / eliminar sede
GET/POST        /api/owner/venues/{id}/courts         Listar / crear canchas
PUT/DELETE      /api/owner/courts/{id}                Editar / eliminar cancha
PUT             /api/owner/courts/{id}/prices         Configurar tarifas por franja
GET/POST        /api/owner/courts/{id}/blackouts      Listar / crear bloqueos
DELETE          /api/owner/blackouts/{id}             Eliminar bloqueo
GET/POST        /api/owner/reservations               Agenda / reserva manual
GET             /api/owner/reports                     Reporte de ocupación e ingresos
```

**Errores de dominio** — cuerpo `{ error_type, message }`:

| `error_type` | HTTP | Motivo |
|--------------|------|--------|
| `OVERLAP` | 422 | El turno ya está ocupado |
| `DURATION_INVALID` | 422 | La duración no coincide con el bloque de la cancha |
| `OPERATING_HOURS` | 422 | Fuera del horario de la sede |
| `ADVANCE_NOTICE` | 422 | Menos de 1 hora de anticipación |
| `ACTIVE_LIMIT` | 422 | Límite de reservas activas alcanzado |
| `BLACKOUT_CONFLICT` | 422 | La cancha está bloqueada en esa franja |
| `NO_PRICE` | 422 | No hay tarifa configurada para la franja |
| `VALIDATION` | 422 | Datos de entrada inválidos |
| `VENUE_NOT_FOUND` / `COURT_NOT_FOUND` / `NOT_FOUND` | 404 | Recurso inexistente |
| `NOT_AUTHORIZED` | 403 | El recurso no pertenece al dueño |
| `EMAIL_EXISTS` | 409 | Correo ya registrado |
| `INVALID_CREDENTIALS` | 401 | Correo o contraseña incorrectos |

---

## Reglas de negocio

| Regla | Detalle |
|-------|---------|
| Duración de reserva | Múltiplo de la duración de bloque de la cancha (`slot_duration_minutes`) |
| Horario operativo | Definido por sede (`opening_time` / `closing_time`) |
| Anticipación mínima | 1 hora |
| Límite activo | 3 reservas activas por cliente |
| Precio por franja | Tarifas configurables por día y hora (diurno/nocturno/festivo) |
| Cancelación | Ventana configurable por sede; cancelar fuera de plazo cuenta como no-show |
| Aislamiento multi-tenant | Cada dueño solo ve y gestiona sus propias sedes y canchas |
