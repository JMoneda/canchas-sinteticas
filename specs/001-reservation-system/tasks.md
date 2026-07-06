---
description: "Lista de tareas para el Sistema de Reservas — Arquitectura Limpia, orden de construcción capa por capa"
---

# Tareas: Sistema de Reservas para Canchas de Fútbol Sintético

**Entrada**: Documentos de diseño de `/specs/001-reservation-system/`

**Prerequisitos**: plan.md ✅ · spec.md ✅ · research.md ✅ · data-model.md ✅ · contracts/ ✅

**Orden de Construcción**: Dominio → Aplicación → Infraestructura → API (por historia de usuario) → Frontend (por historia de usuario)

**Regla de Constitución**: Las pruebas para cada regla de dominio DEBEN ser escritas y confirmadas como FALLIDAS antes de implementar la regla.

## Formato: `[ID] [P?] [Historia?] Descripción`

- **[P]**: Puede ejecutarse en paralelo (archivos diferentes, sin dependencia entre tareas)
- **[Historia]**: Mapea la tarea a una historia de usuario (HU1–HU4)
- Incluir rutas de archivo exactas en cada descripción de tarea

---

## Fase 1: Configuración

**Propósito**: Inicialización del proyecto — sin código, sin pruebas aún.

- [x] T001 Crear el árbol completo de directorios según plan.md: `backend/{domain/{entities,value_objects,repositories},application/use_cases,infrastructure/{models,repositories},api/routes,tests/{unit/domain,integration}}/` y `frontend/src/{components,services}/`
- [x] T002 [P] Inicializar proyecto Python backend: crear `backend/requirements.txt` (fastapi, uvicorn[standard], sqlalchemy, pytest, httpx, pytest-cov) y `backend/pyproject.toml` con `[tool.pytest.ini_options] testpaths = ["tests"]`
- [x] T003 [P] Inicializar frontend: crear `frontend/package.json` (react 18, react-dom, vite), `frontend/vite.config.js` (puerto 5173), `frontend/index.html` y `frontend/src/App.jsx` como placeholder vacío

---

## Fase 2: Fundacional — Capa de Dominio

**Propósito**: Objetos de dominio puros, interfaces de repositorio y todas las reglas de negocio con pruebas unitarias. Sin base de datos, sin framework.

**⚠️ CRÍTICO**: Todas las fases a partir de la 3 dependen de que esta fase esté completa y todas las pruebas unitarias pasen.

- [x] T004 Crear `backend/domain/exceptions.py`: definir clase base `DomainError(Exception)` y 9 subclases tipadas — `OverlapError`, `DurationError`, `InvalidBlockError`, `OperatingHoursError`, `AdvanceNoticeError`, `ActiveLimitError`, `FieldNotFoundError`, `NotAuthorizedError`, `AlreadyCancelledError` — cada una con una cadena `message` legible por humanos por defecto
- [x] T005 [P] Crear `backend/domain/entities/field.py`: dataclass `Field` con `id: int` y `name: str`; agregar `backend/domain/entities/__init__.py`
- [x] T006 [P] Crear `backend/domain/entities/reservation.py`: dataclass `Reservation` con `id: str` (UUID), `user_id: str`, `field_id: int`, `date: date`, `start_time: time`, `end_time: time`, `status: str` (literal `'active'`/`'cancelled'`), `created_at: datetime`, `cancelled_at: datetime | None`; agregar propiedades `start_datetime` y `end_datetime` combinando date + time
- [x] T007 [P] Crear `backend/domain/repositories/field_repository.py`: `FieldRepository(ABC)` abstracto con método `get_all() -> list[Field]`; agregar `__init__.py`
- [x] T008 [P] Crear `backend/domain/repositories/reservation_repository.py`: `ReservationRepository(ABC)` abstracto con métodos: `save(r: Reservation) -> Reservation`, `get_by_id(id: str) -> Reservation | None`, `count_active_by_user(user_id: str, now: datetime) -> int`, `get_active_by_field_and_date(field_id: int, date: date) -> list[Reservation]`, `get_active_by_user(user_id: str, now: datetime) -> list[Reservation]`, `cancel(id: str, cancelled_at: datetime) -> None`, `add_no_show(reservation_id: str, user_id: str, cancelled_at: datetime) -> None`
- [x] T009 Crear `backend/domain/value_objects/time_slot.py`: dataclass `TimeSlot` con `date: date`, `start_time: time`, `end_time: time`; implementar `__post_init__` que lanza `DurationError` si duración < 1 hora e `InvalidBlockError` si el minuto de inicio o fin no está en `{0, 30}`; escribir pruebas unitarias en `backend/tests/unit/domain/test_time_slot.py` — **confirmar que las pruebas FALLAN antes de agregar la lógica, luego pasan después** (cubre: franja de 30 min rechazada, franja de 1h aceptada, 1.5h aceptada, tiempos no alineados rechazados)
- [x] T010 Agregar validación de horario operativo a `TimeSlot.__post_init__` en `backend/domain/value_objects/time_slot.py`: lanzar `OperatingHoursError` si `start_time < time(6, 0)` o `end_time > time(23, 0)`; agregar pruebas unitarias a `backend/tests/unit/domain/test_time_slot.py` (cubre: 05:30–06:30 rechazado, 22:00–23:30 rechazado, 06:00–07:00 aceptado, 22:00–23:00 aceptado)
- [x] T011 Agregar `is_bookable(now: datetime) -> bool` a `TimeSlot` en `backend/domain/value_objects/time_slot.py`: retorna `True` si `self.start_datetime - now >= timedelta(hours=1)`; agregar pruebas unitarias que cubran exactamente 60 min (aceptado), 59 min (rechazado) y franjas pasadas (rechazadas)
- [x] T012 Agregar `overlaps_with(other: "TimeSlot") -> bool` a `TimeSlot`: retorna `True` solo si `self.date == other.date` y los rangos de tiempo comparten algún período (no meramente adyacentes); agregar pruebas unitarias que cubran: superposición misma fecha (True), franjas adyacentes misma fecha (False), no superpuestas misma fecha (False), fechas diferentes (False)

**Punto de Control**: Ejecutar `pytest backend/tests/unit/` — todas las pruebas deben pasar antes de la Fase 3.

---

## Fase 3: Fundacional — Capa de Aplicación

**Propósito**: Los casos de uso orquestan las reglas de dominio mediante fakes de repositorio inyectados. Sin SQLite, sin FastAPI.

**⚠️ CRÍTICO**: Todas las fases de API y frontend dependen de que esta fase esté completa.

- [x] T013 [P] Crear `backend/application/dtos.py`: definir dataclasses `CreateReservationInput` (user_id, field_id, date, start_time, end_time), `ReservationOutput` (reservation_id, user_id, field_id, field_name, date, start_time, end_time, status), `SlotOutput` (start_time, end_time), `FieldAvailabilityOutput` (field_id, field_name, available_slots), `CancelOutput` (reservation_id, status, no_show: bool); agregar `backend/application/__init__.py` y `backend/application/use_cases/__init__.py`
- [x] T014 Crear `backend/application/use_cases/list_available_slots.py`: caso de uso `ListAvailableSlots` recibe `FieldRepository` + `ReservationRepository`; método `execute(date: date, now: datetime) -> list[FieldAvailabilityOutput]` genera todas las franjas de 30 min de 06:00–23:00, elimina rangos ocupados (de reservas activas), elimina franjas no reservables (is_bookable falla); escribir pruebas unitarias en `backend/tests/unit/use_cases/test_list_available_slots.py` usando clases de repositorio fake en línea (cubre: sin reservas → lista completa de franjas; franja ocupada → eliminada de la lista; franja dentro de 1h desde ahora → eliminada)
- [x] T015 Crear `backend/application/use_cases/create_reservation.py`: caso de uso `CreateReservation`; `execute(input: CreateReservationInput, now: datetime) -> ReservationOutput`; pasos: (1) obtener cancha o lanzar `FieldNotFoundError`, (2) construir `TimeSlot` (lanza errores de duración/bloque/horario), (3) verificar `is_bookable` o lanzar `AdvanceNoticeError`, (4) `count_active_by_user` — lanzar `ActiveLimitError` si ≥ 2, (5) `get_active_by_field_and_date` + verificación `overlaps_with` — lanzar `OverlapError` si hay superposición, (6) guardar y retornar; escribir pruebas unitarias en `backend/tests/unit/use_cases/test_create_reservation.py` cubriendo las 6 violaciones de regla (una prueba por regla) + camino feliz
- [x] T016 Crear `backend/application/use_cases/list_reservations.py`: caso de uso `ListReservations`; `execute(user_id: str, now: datetime) -> list[ReservationOutput]` llama `get_active_by_user` (solo reservas futuras activas); escribir pruebas unitarias en `backend/tests/unit/use_cases/test_list_reservations.py` (cubre: retorna solo futuras activas, excluye pasadas, retorna lista vacía para usuario desconocido)
- [x] T017 Crear `backend/application/use_cases/cancel_reservation.py`: caso de uso `CancelReservation`; `execute(reservation_id: str, user_id: str, now: datetime) -> CancelOutput`; pasos: (1) obtener por id o lanzar `NotFoundError`, (2) verificar `r.user_id == user_id` o lanzar `NotAuthorizedError`, (3) verificar `r.status != 'cancelled'` o lanzar `AlreadyCancelledError`, (4) determinar no_show = `r.start_datetime - now < timedelta(hours=2)`, (5) cancelar, (6) si no_show agregar registro de no_show; escribir pruebas unitarias en `backend/tests/unit/use_cases/test_cancel_reservation.py` (cubre: cancelación limpia, cancelación tardía → no_show=True, no autorizado, ya cancelado, no encontrado)

**Punto de Control**: Ejecutar `pytest backend/tests/unit/` — todas las pruebas de casos de uso deben pasar antes de la Fase 4.

---

## Fase 4: Fundacional — Infraestructura y Bootstrap de API

**Propósito**: Persistencia SQLite + cableado de la aplicación FastAPI. Habilita todas las historias de usuario de API.

**⚠️ CRÍTICO**: Todas las fases de API dependen de esta fase.

- [x] T018 Crear `backend/infrastructure/database.py`: `create_engine` de SQLAlchemy apuntando a `backend/reservations.db` (usar `check_same_thread=False`); fábrica `SessionLocal`; `Base = declarative_base()`; función `create_tables()` que llama `Base.metadata.create_all()`; agregar `backend/infrastructure/__init__.py`
- [x] T019 [P] Crear `backend/infrastructure/models/field_model.py` (`FieldModel`: id, name), `backend/infrastructure/models/reservation_model.py` (`ReservationModel`: todas las columnas del esquema data-model.md), `backend/infrastructure/models/no_show_model.py` (`NoShowModel`); todos heredan de `Base`; agregar `backend/infrastructure/models/__init__.py`
- [x] T020 [P] Crear `backend/infrastructure/seed.py`: función `seed_fields(session)` que inserta `FieldModel(name="Cancha A")`, `Cancha B`, `Cancha C` solo si `session.query(FieldModel).count() == 0`; idempotente
- [x] T021 Crear `backend/infrastructure/repositories/sqlite_field_repository.py`: `SQLiteFieldRepository(FieldRepository)` implementa `get_all()` consultando `FieldModel` y mapeando cada uno a la entidad de dominio `Field`; agregar `backend/infrastructure/repositories/__init__.py`
- [x] T022 Crear `backend/infrastructure/repositories/sqlite_reservation_repository.py`: `SQLiteReservationRepository(ReservationRepository)` implementa todos los métodos abstractos; `count_active_by_user` filtra por `status='active'` Y `(date > today OR (date = today AND end_time > current_time))`; `save` usa UUID v4 para id; `cancel` establece `status='cancelled'` y `cancelled_at`; `add_no_show` inserta `NoShowModel`; incluir métodos mapper ORM↔dominio
- [x] T023 Crear `backend/api/main.py`: aplicación FastAPI con contexto `lifespan` que llama `create_tables()` luego `seed_fields()`; crear `backend/api/dependencies.py` con dependencia `get_db_session()` y funciones de fábrica `get_field_repo()`, `get_reservation_repo()`, `get_create_reservation_uc()`, `get_cancel_reservation_uc()`, `get_list_reservations_uc()`, `get_list_slots_uc()`; agregar `backend/api/__init__.py` y `backend/api/routes/__init__.py`

**Punto de Control**: Iniciar backend con `uvicorn api.main:app --reload` desde `backend/` — el servidor inicia, tablas creadas, 3 canchas cargadas, sin errores.

---

## Fase 5: Historia de Usuario 1 — Ver Disponibilidad de Canchas (Prioridad: P1) 🎯

**Objetivo**: Los usuarios pueden ver las franjas horarias de 30 minutos disponibles por cancha para cualquier fecha futura.

**Prueba Independiente**: `GET /api/fields/availability?date=<mañana>` retorna 3 canchas con arrays de franjas; `GET` para fecha pasada retorna 400; la UI muestra la grilla de franjas.

- [x] T024 [HU1] Crear `backend/api/routes/fields.py`: `GET /api/fields/availability` — parsear parámetro de consulta `date`, llamar `ListAvailableSlots.execute()`, retornar lista `FieldAvailabilityOutput`; agregar helper `_domain_error_to_http()` en `backend/api/routes/fields.py` que mapea subclases de `DomainError` a códigos de estado HTTP correctos y cuerpo `{error_type, message}`; agregar pruebas de integración en `backend/tests/integration/test_api.py` (cubre: fecha futura válida → 200 + 3 canchas; fecha pasada → 400)
- [x] T025 [HU1] Crear `frontend/src/services/api.js`: exportar `fetchAvailability(date)` — `GET /api/fields/availability?date={date}`; retorna JSON parseado; lanza `{error_type, message}` en no-2xx
- [x] T026 [HU1] Crear `frontend/src/components/FieldAvailability.jsx`: `<input type="date">` por defecto en mañana; al cambiar llama `fetchAvailability`; renderiza una tarjeta por cancha mostrando nombre de cancha y grilla de botones de franja disponible (`HH:MM–HH:MM`); emite prop `onSlotSelect(field, slot)`; muestra "Sin franjas disponibles" cuando el array está vacío; renderiza cadena de error en línea al fallar la API

**Punto de Control**: Iniciar backend + frontend; seleccionar una fecha; verificar que aparecen 3 tarjetas de cancha con franjas disponibles.

---

## Fase 6: Historia de Usuario 2 — Crear una Reserva (Prioridad: P2) 🎯

**Objetivo**: El usuario de la sesión puede seleccionar una franja y enviar una reserva; las 6 violaciones de regla de dominio producen mensajes de error distintos.

**Prueba Independiente**: Enviar reserva válida → 201 + confirmación; enviar cada caso inválido → 422 con `error_type` correcto.

- [x] T027 [HU2] Agregar `POST /api/reservations` a `backend/api/routes/reservations.py`: parsear cuerpo de solicitud en `CreateReservationInput`, llamar `CreateReservation.execute(input, now=datetime.now())`; capturar subclases de `DomainError` y retornar 422 con `{error_type, message}` (usar nombre del tipo de error como cadena); capturar `FieldNotFoundError` como 422; agregar pruebas de integración en `backend/tests/integration/test_api.py` cubriendo: solicitud válida → 201; cada una de las 6 violaciones de regla → 422 con `error_type` correcto; registrar router en `api/main.py`
- [x] T028 [HU2] Agregar `createReservation(data)` a `frontend/src/services/api.js`: `POST /api/reservations` con cuerpo JSON; retorna respuesta parseada en 201; lanza `{error_type, message}` en 422
- [x] T029 [HU2] Crear `frontend/src/components/ErrorMessage.jsx`: recibe prop `error` `{error_type, message}`; renderiza un banner de error estilizado mostrando `message`; no renderiza nada cuando `error` es null
- [x] T030 [HU2] Crear `frontend/src/components/ReservationForm.jsx`: recibe props `field` y `slot` pre-poblados desde la selección de franja; muestra identificador de usuario (solo lectura, de la sesión); al enviar llama `createReservation`; en éxito muestra confirmación con ID de reserva y limpia el formulario; en error pasa `{error_type, message}` a `ErrorMessage`
- [x] T031 [HU2] Crear `frontend/src/components/IdentifierGate.jsx`: input de texto para identificador de usuario + botón enviar; despacha `{type: 'SET_USER_ID', payload: id}` al reducer de App al enviar; actualizar `frontend/src/App.jsx` con `useReducer(reducer, {userId: null, view: 'gate'})` — el reducer maneja `SET_USER_ID` (establece userId, cambia vista a 'main'); la vista principal renderiza `<FieldAvailability onSlotSelect={...} />` y renderiza condicionalmente `<ReservationForm />` cuando se selecciona una franja

**Punto de Control**: Ingresar identificador → ver disponibilidad → hacer clic en franja → completar formulario → enviar → ver confirmación; enviar cada caso inválido → ver mensaje de error específico.

---

## Fase 7: Historia de Usuario 3 — Ver Mis Reservas (Prioridad: P3)

**Objetivo**: El usuario de la sesión puede ver sus próximas reservas activas.

**Prueba Independiente**: `GET /api/reservations?user_id=maria` retorna reservas futuras activas; array vacío para usuario desconocido; la lista de UI se renderiza correctamente.

- [x] T032 [HU3] Agregar `GET /api/reservations` a `backend/api/routes/reservations.py`: parsear parámetro de consulta `user_id`, llamar `ListReservations.execute(user_id, now=datetime.now())`; retornar lista de `ReservationOutput`; agregar pruebas de integración (cubre: usuario con 2 reservas → retorna ambas; usuario desconocido → 200 array vacío; reserva pasada → excluida)
- [x] T033 [HU3] Agregar `fetchReservations(userId)` a `frontend/src/services/api.js`: `GET /api/reservations?user_id={userId}`; retorna array JSON parseado
- [x] T034 [HU3] Crear `frontend/src/components/ReservationList.jsx`: llama `fetchReservations(userId)` al montar; renderiza cada reserva como tarjeta (nombre de cancha, fecha, hora inicio–fin); muestra estado vacío "Sin próximas reservas" cuando el array está vacío; integrar en la vista principal de `App.jsx` junto con `FieldAvailability`

**Punto de Control**: Crear una reserva → navegar a la lista → verificar que aparece; comprobar que las reservas pasadas o canceladas están ausentes.

---

## Fase 8: Historia de Usuario 4 — Cancelar una Reserva (Prioridad: P4)

**Objetivo**: El usuario de la sesión puede cancelar una de sus reservas activas; la cancelación tardía muestra aviso de no-show.

**Prueba Independiente**: `DELETE /api/reservations/{id}` con usuario correcto → 200 + `{no_show: bool}`; usuario incorrecto → 403; ya cancelada → 400.

- [x] T035 [HU4] Agregar `DELETE /api/reservations/{reservation_id}` a `backend/api/routes/reservations.py`: parsear cuerpo `{user_id}`, llamar `CancelReservation.execute(reservation_id, user_id, now=datetime.now())`; retornar `CancelOutput`; mapear `NotAuthorizedError` → 403, `NotFoundError` → 404, `AlreadyCancelledError` → 400, todos con cuerpo `{error_type, message}`; agregar pruebas de integración (cubre: cancelación limpia, cancelación tardía → no_show=true, no autorizado → 403, ya cancelado → 400)
- [x] T036 [HU4] Agregar `cancelReservation(reservationId, userId)` a `frontend/src/services/api.js`: `DELETE /api/reservations/{reservationId}` con cuerpo JSON `{user_id: userId}`; retorna respuesta parseada; lanza `{error_type, message}` en error
- [x] T037 [HU4] Agregar botón cancelar a cada tarjeta de reserva en `frontend/src/components/ReservationList.jsx`: al hacer clic llama `cancelReservation`; en éxito elimina la reserva de la lista local y muestra banner de no-show si `no_show === true`; en error renderiza `ErrorMessage` (403: "no autorizado", 400: "ya cancelada"); actualizar lista después de cancelación exitosa

**Punto de Control**: Las 4 historias de usuario funcionan de forma independiente. Ejecutar escenarios 1–12 de quickstart.md.

---

## Fase 9: Pulido y Consideraciones Transversales

- [x] T038 [P] Configurar CORS en `backend/api/main.py`: agregar `CORSMiddleware` permitiendo `http://localhost:5173`; ejecutar suite completa de pruebas del backend `pytest backend/tests/ -v --cov=backend --cov-report=term-missing` — todas las pruebas deben pasar
- [x] T039 [P] Ejecutar escenarios de validación 1–12 de quickstart.md de extremo a extremo con ambos servidores corriendo; confirmar que todos los checkboxes pasan
- [x] T040 Revisión de cumplimiento de constitución: verificar (a) sin lógica de negocio en `api/routes/` ni en ningún archivo de `frontend/src/`, (b) sin `import` desde `infrastructure/` dentro de `domain/` o `application/`, (c) las 6 reglas de dominio tienen pruebas unitarias, (d) `TimeSlot` es el único lugar donde vive la lógica de validación

---

## Dependencias y Orden de Ejecución

### Dependencias entre Fases

- **Configuración (Fase 1)**: Sin dependencias — comenzar inmediatamente
- **Capa de Dominio (Fase 2)**: Depende de Configuración — **BLOQUEA todas las fases siguientes**
- **Capa de Aplicación (Fase 3)**: Depende de Capa de Dominio — BLOQUEA API y Frontend
- **Infraestructura y Bootstrap de API (Fase 4)**: Depende de Capa de Aplicación — BLOQUEA todas las fases de API
- **HU1 (Fase 5)**: Depende de la finalización de Fase 4
- **HU2 (Fase 6)**: Depende de Fase 5 (FieldAvailability renderiza franjas que ReservationForm lee)
- **HU3 (Fase 7)**: Depende de Fase 4; puede comenzar después de Fase 4 independientemente de HU2
- **HU4 (Fase 8)**: Depende de Fase 7 (el botón cancelar vive en ReservationList)
- **Pulido (Fase 9)**: Depende de todas las fases de historias de usuario

### Oportunidades de Paralelismo Dentro de las Fases

```bash
# Fase 2 — ejecutar en paralelo (archivos diferentes):
T005  # field.py
T006  # reservation.py
T007  # field_repository.py
T008  # reservation_repository.py

# Fase 4 — ejecutar en paralelo después de T018:
T019  # Modelos ORM
T020  # seed.py

# Fase 9 — ejecutar en paralelo:
T038  # CORS + suite de pruebas
T039  # Validación de quickstart
```

---

## Estrategia de Implementación

### MVP Primero (Dominio + HU1 + HU2 únicamente)

1. Completar Fase 1: Configuración
2. Completar Fase 2: Capa de Dominio + todas las pruebas unitarias en verde
3. Completar Fase 3: Capa de Aplicación + todas las pruebas unitarias en verde
4. Completar Fase 4: Infraestructura + Bootstrap de API
5. Completar Fase 5: HU1 (Ver Disponibilidad)
6. Completar Fase 6: HU2 (Crear Reserva)
7. **PARAR y VALIDAR**: El flujo completo de reserva funciona de extremo a extremo (escenarios quickstart 1–7)
8. Desplegar / demostrar si está listo

### Entrega Incremental

1. Fases 1–4 completas → Dominio + Aplicación + Infraestructura listas
2. Fase 5 (HU1) → Vista de disponibilidad en vivo, probar de forma independiente
3. Fase 6 (HU2) → Flujo de reserva en vivo, probar de forma independiente
4. Fase 7 (HU3) → Lista de reservas en vivo, probar de forma independiente
5. Fase 8 (HU4) → Flujo de cancelación en vivo, probar de forma independiente
6. Fase 9 → Pulido + pasada completa de regresión

---

## Notas

- `[P]` = archivos diferentes, sin dependencia de una tarea hermana en la misma fase
- `[HU?]` = mapea a historia de usuario para trazabilidad
- Las pruebas para reglas de dominio se escriben PRIMERO y deben FALLAR antes de implementar la lógica de regla (TDD Rojo→Verde)
- Cada fase termina con un punto de control ejecutable — parar y verificar antes de avanzar
- Sin lógica de negocio en `api/routes/` — solo HTTP entrada/salida y mapeo de `DomainError`
- Sin importaciones de SQLAlchemy o infraestructura dentro de `domain/` o `application/`