# Canchas Sintéticas

Sistema de reservas para canchas de fútbol sintético. Permite ver disponibilidad por hora, reservar turnos y cancelarlos, con validación de reglas de negocio en el dominio.

## Requisitos

| Herramienta | Versión mínima | Uso |
|-------------|----------------|-----|
| .NET SDK    | 10.0+          | Backend (.NET) |
| Node.js     | 18+            | Frontend React |
| Python      | 3.11+          | Backend original (opcional) |

---

## Inicio rápido

### 1. Backend .NET (recomendado)

```bash
cd dotnet-backend

dotnet run --project CanchasSinteticas.Api/CanchasSinteticas.Api.csproj
```

El servidor queda en `http://localhost:8080`.  
Swagger UI: `http://localhost:8080/swagger`  
OpenAPI spec: `http://localhost:8080/swagger/v1/swagger.json`

> La base de datos SQLite se crea automáticamente en `dotnet-backend/CanchasSinteticas.Api/reservations.db`.  
> Las 3 canchas (A, B, C) se siembran al arrancar si no existen.

### 1b. Backend Python (alternativo)

```bash
cd backend

python -m venv .venv
.venv\Scripts\activate          # Windows PowerShell
source .venv/bin/activate       # macOS / Linux

pip install -r requirements.txt
uvicorn api.main:app --reload --port 8000
```

Docs interactivos: `http://localhost:8000/docs`

### 2. Frontend

```bash
cd frontend
npm install
npm run dev
```

La app queda en `http://localhost:5173`.

---

## Ejecutar tests

```bash
cd backend
.venv\Scripts\activate

# Todos los tests
pytest tests/ -v

# Solo unitarios (sin base de datos)
pytest tests/unit/ -v

# Con cobertura
pytest tests/ -v --cov=. --cov-report=term-missing
```

**55 tests · 0 fallos** — unitarios de dominio, use cases e integración con SQLite en memoria.

---

## Reglas de negocio

| Regla | Detalle |
|-------|---------|
| Duración mínima | 1 hora por reserva |
| Horario operativo | 06:00 – 23:00 |
| Anticipación mínima | La reserva debe crearse con al menos 1 hora de antelación |
| Límite activo | Máximo 2 reservas activas por usuario |
| Sin solapamiento | No se puede reservar una cancha ya ocupada en ese rango |
| No-show | Cancelar con menos de 2 horas de anticipación queda registrado como no-show |

---

## API

Base URL: `http://localhost:8000/api`

### Disponibilidad

```
GET /fields/availability?date=YYYY-MM-DD
```

Devuelve las 3 canchas con sus turnos disponibles (bloques de 1 hora libres).  
Retorna `400` si la fecha es pasada.

### Reservas

```
POST   /reservations              → 201  Crea una reserva
GET    /reservations?user_id=X   → 200  Lista reservas activas del usuario
DELETE /reservations/{id}        → 200  Cancela una reserva
```

**Errores de dominio** (HTTP 422 salvo indicación):

| `error_type`       | Motivo                                        |
|--------------------|-----------------------------------------------|
| `OVERLAP`          | El turno ya está ocupado en esa cancha        |
| `DURATION_INVALID` | Duración menor a 1 hora                       |
| `INVALID_BLOCK`    | Hora no alineada a bloques de 30 min          |
| `OPERATING_HOURS`  | Fuera del horario 06:00–23:00                 |
| `ADVANCE_NOTICE`   | Menos de 1 hora de anticipación               |
| `ACTIVE_LIMIT`     | Ya tenés 2 reservas activas                   |
| `FIELD_NOT_FOUND`  | La cancha no existe                           |
| `NOT_FOUND`        | Reserva inexistente (404)                     |
| `NOT_AUTHORIZED`   | La reserva no pertenece al usuario (403)      |
| `ALREADY_CANCELLED`| Ya fue cancelada (400)                        |

---

## Copilot Studio — Custom Connector

Para consumir la API desde un agente de Copilot Studio:

1. Levantá el backend .NET en un host accesible públicamente (Azure App Service, ngrok, etc.).
2. En Copilot Studio → **Conectores** → **Nuevo conector personalizado**.
3. Importá desde URL: `https://<tu-host>/swagger/v1/swagger.json`
4. En **Seguridad**: sin autenticación.
5. Probá la acción **GetAvailability** con un `date` en formato `YYYY-MM-DD`.

El endpoint clave para el agente es:

```
GET /api/fields/availability?date=YYYY-MM-DD
```

Respuesta: lista de canchas con sus turnos libres del día.

---

## Estructura del proyecto

```
canchas-sinteticas/
├── dotnet-backend/
│   ├── CanchasSinteticas.Domain/        # Entidades, ValueObjects, interfaces repos, excepciones
│   ├── CanchasSinteticas.Application/   # Use cases + DTOs
│   ├── CanchasSinteticas.Infrastructure/ # EF Core + SQLite
│   └── CanchasSinteticas.Api/           # Controllers, Middleware, Program.cs
│
├── backend/         # Backend original Python/FastAPI (referencia)
│   ├── domain/                  # Reglas de negocio puras (sin frameworks)
│   │   ├── entities/            #   Field, Reservation
│   │   ├── value_objects/       #   TimeSlot (valida duración, horario, solapamiento)
│   │   ├── repositories/        #   Interfaces abstractas (ABCs)
│   │   └── exceptions.py        #   DomainError y subclases tipadas
│   ├── application/             # Casos de uso (orquestan dominio)
│   │   ├── dtos.py
│   │   └── use_cases/           #   ListAvailableSlots, CreateReservation,
│   │                            #   ListReservations, CancelReservation
│   ├── infrastructure/          # Persistencia SQLite + SQLAlchemy
│   │   ├── database.py
│   │   ├── models/              #   ORM models (separados de las entidades)
│   │   ├── repositories/        #   Implementaciones concretas de los ABCs
│   │   └── seed.py              #   Siembra Cancha A/B/C al iniciar
│   ├── api/                     # HTTP (FastAPI) — solo in/out, sin lógica
│   │   ├── main.py
│   │   ├── dependencies.py
│   │   ├── error_handling.py
│   │   └── routes/              #   fields.py, reservations.py
│   ├── tests/
│   │   ├── unit/
│   │   │   ├── domain/          #   test_time_slot.py
│   │   │   └── use_cases/       #   test_create/cancel/list_*.py
│   │   └── integration/         #   test_api.py (SQLite en memoria)
│   ├── requirements.txt
│   └── pyproject.toml
│
└── frontend/
    └── src/
        ├── components/
        │   ├── IdentifierGate.jsx    # Pantalla de entrada (nombre de usuario)
        │   ├── FieldAvailability.jsx # Grilla de turnos disponibles por cancha
        │   ├── ReservationForm.jsx   # Confirmación de reserva
        │   ├── ReservationList.jsx   # Mis reservas activas + cancelación
        │   └── ErrorMessage.jsx      # Banner de error reutilizable
        ├── services/api.js           # Llamadas HTTP al backend
        ├── App.jsx                   # useReducer: estado de sesión y navegación
        └── main.jsx
```

---

## Flujo de uso

1. Abrís `http://localhost:5173` e ingresás tu nombre (ej: `"maria"`)
2. Seleccionás fecha → ves los turnos disponibles por cancha
3. Hacés click en un turno → confirmás la reserva
4. En "Mis reservas" ves tus reservas activas y podés cancelarlas
