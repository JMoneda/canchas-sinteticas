# Plan de Implementación: Sistema de Reservas para Canchas de Fútbol Sintético

**Rama**: `001-reservation-system` | **Fecha**: 2026-06-25 | **Spec**: [spec.md](spec.md)

**Entrada**: Especificación de funcionalidad en `/specs/001-reservation-system/spec.md`

## Resumen

Construir un sistema de reservas MVP full-stack para canchas de fútbol sintético. El backend
sigue la Arquitectura Limpia (dominio → aplicación → infraestructura → API) con
Python/FastAPI/SQLite. El frontend es una SPA React mínima usando useState/useReducer.
La pureza de la capa de dominio es innegociable: las 6 reglas de negocio viven exclusivamente
en el dominio, cada una respaldada por pruebas unitarias escritas primero (TDD). La
persistencia usa SQLAlchemy con modelos ORM físicamente separados de las entidades de dominio.
La "finalización" de reservas se computa en tiempo de consulta (end_datetime < now) — no se
necesita ningún job en segundo plano para el MVP.

## Contexto Técnico

**Lenguaje/Versión**: Python 3.11+ (backend) · Node.js 20+ / React 18 (frontend)

**Dependencias Principales**:
- Backend: `fastapi`, `uvicorn`, `sqlalchemy`, `pytest`, `httpx`, `pytest-cov`
- Frontend: `react`, `react-dom`, `vite`

**Almacenamiento**: SQLite — archivo único (`backend/reservations.db`)

**Pruebas**: pytest + httpx TestClient (unitarias e integración)

**Plataforma Objetivo**: Servidor de desarrollo local, una sola máquina

**Tipo de Proyecto**: Aplicación web — backend REST API + frontend React SPA

**Objetivos de Rendimiento**: Nivel MVP; sin objetivos de rendimiento explícitos. La
serialización de SQLite maneja la carga de escritura concurrente esperada mediante
transacciones IMMEDIATE.

**Restricciones**: Solo SQLite · Sin microservicios · Sin colas de mensajes · Solo React
useState/useReducer · Sin Redux ni librerías de estado externas.

**Escala/Alcance**: MVP — 2–3 canchas pre-cargadas, pocos usuarios concurrentes, una sola zona horaria.

## Verificación de Constitución

*CONTROL: Debe pasar antes de la investigación de Fase 0. Verificar nuevamente después del diseño de Fase 1.*

| Principio | Estado | Notas |
|-----------|--------|-------|
| I. Arquitectura Dominio-Primero | ✅ APROBADO | Las 6 reglas en dominio; las rutas API manejan solo HTTP |
| II. Arquitectura Limpia + SOLID | ✅ APROBADO | Dirección de dependencias: dominio ← aplicación ← infraestructura/API |
| III. Simplicidad sobre Ingeniería | ✅ APROBADO | Proceso único por nivel; sin capas adicionales más allá de lo requerido |
| IV. Dominio Dirigido por Pruebas | ✅ APROBADO | Pruebas unitarias de dominio escritas primero (Rojo→Verde→Refactor) |
| V. Disciplina de Alcance MVP | ✅ APROBADO | Sin pagos, autenticación, administración, notificaciones |

**Cobertura de Reglas de Dominio**:

| Regla | Aplicada En | Prueba |
|-------|-------------|--------|
| Sin superposición de canchas | `TimeSlot.overlaps_with()` + verificación en repositorio del caso de uso | Requerida |
| Mín 1h / bloques de 30 min | Validación de `TimeSlot` en construcción | Requerida |
| Horario operativo 6 AM–11 PM | Validación de `TimeSlot` en construcción | Requerida |
| Aviso previo de 1h | `TimeSlot.is_bookable(now)` | Requerida |
| Máx 2 reservas activas | Caso de uso `CreateReservation` vía conteo en repositorio | Requerida |
| No-show en cancelación tardía | Caso de uso `CancelReservation` | Requerida |

**Re-verificación post-Fase 1**: ✅ APROBADO — los artefactos de diseño (modelo de datos, contratos)
no introducen nuevas capas, dependencias ni abstracciones más allá de lo que permite la constitución.

**CONTROL: APROBADO. Sin violaciones.**

## Estructura del Proyecto

### Documentación (esta funcionalidad)

```text
specs/001-reservation-system/
├── plan.md              # Este archivo
├── research.md          # Salida de Fase 0
├── data-model.md        # Salida de Fase 1
├── quickstart.md        # Salida de Fase 1
├── contracts/
│   ├── fields.md        # GET /api/fields/availability
│   └── reservations.md  # POST/GET/DELETE /api/reservations
└── tasks.md             # Salida de Fase 2 (/speckit-tasks)
```

### Código Fuente (raíz del repositorio)

```text
backend/
├── domain/
│   ├── entities/
│   │   ├── __init__.py
│   │   ├── field.py              # Entidad Cancha (id, name)
│   │   └── reservation.py        # Entidad Reserva + lógica de estado
│   ├── value_objects/
│   │   ├── __init__.py
│   │   └── time_slot.py          # TimeSlot: todas las validaciones de reserva
│   ├── repositories/
│   │   ├── __init__.py
│   │   ├── field_repository.py   # Puerto abstracto
│   │   └── reservation_repository.py  # Puerto abstracto
│   └── exceptions.py             # DomainError y subclases
├── application/
│   ├── use_cases/
│   │   ├── __init__.py
│   │   ├── create_reservation.py
│   │   ├── cancel_reservation.py
│   │   ├── list_reservations.py
│   │   └── list_available_slots.py
│   └── dtos.py                   # Clases de datos entrada/salida
├── infrastructure/
│   ├── database.py               # Motor SQLAlchemy + fábrica de sesiones
│   ├── models/
│   │   ├── __init__.py
│   │   ├── field_model.py        # Modelo ORM (separado de la entidad de dominio)
│   │   ├── reservation_model.py  # Modelo ORM
│   │   └── no_show_model.py      # Modelo ORM
│   ├── repositories/
│   │   ├── __init__.py
│   │   ├── sqlite_field_repository.py
│   │   └── sqlite_reservation_repository.py
│   └── seed.py                   # Pre-carga canchas al inicio
├── api/
│   ├── main.py                   # Aplicación FastAPI + lifespan
│   ├── dependencies.py           # Cableado de inyección de dependencias
│   └── routes/
│       ├── __init__.py
│       ├── fields.py             # GET /api/fields/availability
│       └── reservations.py       # POST/GET/DELETE /api/reservations
├── tests/
│   ├── unit/
│   │   └── domain/
│   │       ├── test_time_slot.py          # Reglas de validación de TimeSlot
│   │       ├── test_reservation_rules.py  # Superposición, límite, aviso previo
│   │       └── test_cancel_rules.py       # Regla de umbral de no-show
│   └── integration/
│       └── test_api.py           # Pruebas de endpoints full-stack vía TestClient
├── requirements.txt
└── pyproject.toml

frontend/
├── src/
│   ├── components/
│   │   ├── IdentifierGate.jsx    # Pantalla de entrada — captura identificador de usuario
│   │   ├── FieldAvailability.jsx # Grilla de franjas por cancha
│   │   ├── ReservationForm.jsx   # Formulario de reserva
│   │   ├── ReservationList.jsx   # Reservas activas + botón cancelar
│   │   └── ErrorMessage.jsx      # Renderiza mensajes de error de dominio
│   ├── services/
│   │   └── api.js                # Todas las llamadas fetch al backend
│   └── App.jsx                   # Raíz: estado de sesión useReducer + enrutamiento
├── index.html
├── package.json
└── vite.config.js
```

**Decisión de Estructura**: Aplicación web (backend + frontend como directorios raíz separados).
El backend usa directorios de capas de Arquitectura Limpia como unidad organizativa principal.
Los modelos ORM (`infrastructure/models/`) y las entidades de dominio (`domain/entities/`) están
en directorios separados — sin clase base compartida, sin importación desde infraestructura hacia dominio.

## Seguimiento de Complejidad

> No se detectaron violaciones en la Verificación de Constitución. No se requieren entradas.
