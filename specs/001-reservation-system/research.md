# Investigación: Sistema de Reservas para Canchas de Fútbol Sintético

**Salida de Fase 0** — todas las decisiones técnicas resueltas antes de que comience el diseño.

---

## Decisión 1: Patrón de Arquitectura

**Decisión**: Arquitectura Limpia con cuatro capas explícitas (dominio, aplicación,
infraestructura, API).

**Justificación**: Mandatado por los Principios I y II de la Constitución. Mantiene las
reglas de negocio verificables de forma aislada, desacopla la persistencia de la lógica
y permite reemplazar la capa API sin tocar el dominio.

**Alternativas consideradas**: MVC por capas (rechazado — la lógica de negocio se filtra
hacia los controladores); Active Record (rechazado — acopla el dominio a la persistencia,
viola la regla de dependencia hacia adentro).

---

## Decisión 2: Implementación del Estado "Completado"

**Decisión**: Las reservas se almacenan como `active` en la base de datos. En tiempo de
consulta, cualquier reserva cuyo `end_datetime` (date + end_time) está en el pasado es
tratada como `completed`. No se usa ningún job en segundo plano ni tarea cron. La consulta
del repositorio "contar activas para un usuario" filtra por `status = 'active' AND end_datetime > now()`.

**Justificación**: SQLite no tiene un planificador nativo. Agregar una tarea en segundo
plano (APScheduler, Celery) violaría el Principio III de la Constitución (sin colas de
mensajes, simplicidad primero). Computar la finalización en tiempo de consulta es correcto,
no requiere infraestructura adicional y es completamente verificable.

**Alternativas consideradas**: Almacenar `completed` como estado explícito en BD actualizado
por una tarea cron (rechazado — sobreingeniería para MVP, requiere dependencia de planificador);
hooks de eventos de SQLAlchemy para auto-actualizar el estado (rechazado — comportamiento mágico,
difícil de probar, oculta la lógica).

---

## Decisión 3: Concurrencia / Prevención de Reservas Duplicadas

**Decisión**: SQLite abierto en modo WAL con transacciones IMMEDIATE para operaciones de
escritura. El caso de uso `create_reservation` adquiere una transacción, verifica superposiciones
mediante una consulta SELECT e inserta solo si no existen. El modo IMMEDIATE de SQLite serializa
las escrituras concurrentes, evitando que dos reservas simultáneas ambas tengan éxito.

**Justificación**: El modo WAL de SQLite con transacciones IMMEDIATE es la solución fiable
más simple. A escala MVP (pocos usuarios), la serialización de escritura de SQLite es suficiente.

**Alternativas consideradas**: Bloqueo optimista con columnas de versión (rechazado — complejidad
no justificada a escala MVP); bloqueos en memoria a nivel de aplicación (rechazado — no funciona
entre procesos, frágil).

---

## Decisión 4: Implementación del Patrón Repositorio

**Decisión**: Interfaces de repositorio abstractas (`FieldRepository`, `ReservationRepository`)
definidas en `domain/repositories/` como ABCs de Python. Las implementaciones concretas de
SQLAlchemy viven en `infrastructure/repositories/`. Los casos de uso reciben repositorios
mediante inyección por constructor (inversión de dependencias). Las pruebas unitarias inyectan
fakes en memoria; las pruebas de integración usan SQLite real.

**Justificación**: Permite pruebas unitarias de dominio puras sin dependencia de BD. Aplica la
regla de dependencia hacia adentro — el dominio nunca importa desde infraestructura. Sigue el
patrón puerto/adaptador de la Arquitectura Limpia.

**Alternativas consideradas**: Consultas SQLAlchemy directas en casos de uso (rechazado —
acopla la capa de aplicación a la infraestructura, viola el Principio II de la Constitución);
Django ORM / Active Record (rechazado — no está en este stack).

---

## Decisión 5: Estrategia de Excepciones de Dominio

**Decisión**: Una excepción base `DomainError` en `domain/exceptions.py` con subclases tipadas
por violación de regla (`OverlapError`, `OperatingHoursError`, `AdvanceNoticeError`,
`DurationError`, `ActiveLimitError`, `FieldNotFoundError`, `NotAuthorizedError`,
`AlreadyCancelledError`). La capa API captura subclases de `DomainError` y las mapea a los
códigos de estado HTTP apropiados (422 Unprocessable Entity para violaciones de reglas de
negocio, 404/403 para errores de recurso/autorización).

**Justificación**: Las excepciones tipadas hacen imposible capturar accidentalmente una violación
de regla específica. El mapeo de la capa API es explícito y verificable. Cada tipo de error lleva
un `message` legible por humanos que el RF-011 requiere.

**Alternativas consideradas**: Devolver objetos resultado / uniones discriminadas (rechazado —
más complejo que excepciones a esta escala); `ValueError` genérico (rechazado — pierde información
de tipo, más difícil de mapear a códigos HTTP).

---

## Decisión 6: Arquitectura de Estado del Frontend

**Decisión**: `App.jsx` mantiene un único almacén `useReducer` con forma
`{ userId: string | null, view: 'gate' | 'main' }`. Cada componente hijo maneja su propio
estado local con `useState` (p. ej., fecha seleccionada en `FieldAvailability`, campos del
formulario en `ReservationForm`). `ReservationList` obtiene datos al montar y después de
una cancelación. Todas las llamadas HTTP están centralizadas en `services/api.js`.

**Justificación**: Cumple la restricción de la constitución (solo useState/useReducer). `useReducer`
a nivel de App para el estado de sesión es un patrón React bien establecido para estado
"casi global" sin Redux. `useState` local para estado de UI efímero mantiene los componentes
autocontenidos.

**Alternativas consideradas**: React Context para propagación de userId (rechazado — agrega
abstracción no necesaria a esta escala; el prop drilling a 2 niveles es aceptable); Redux Toolkit
(prohibido por la constitución).

---

## Decisión 7: Forma de Respuesta de Error de la API

**Decisión**: Todas las violaciones de reglas de dominio devuelven HTTP 422 con cuerpo:
```json
{
  "error_type": "OVERLAP | DURATION_INVALID | OPERATING_HOURS | ADVANCE_NOTICE | ACTIVE_LIMIT | FIELD_NOT_FOUND | NOT_AUTHORIZED | ALREADY_CANCELLED | INVALID_BLOCK",
  "message": "Explicación legible por humanos de la violación de regla."
}
```
Los errores de recurso (no encontrado, no autorizado) devuelven 404/403 con la misma forma.
Los errores de validación de FastAPI (solicitud malformada) devuelven 422 con la forma por
defecto de FastAPI.

**Justificación**: Un `error_type` legible por máquina permite al frontend mostrar mensajes
específicos por regla (RF-011). La forma consistente en todas las respuestas de error simplifica
el manejo de errores en el frontend.

**Alternativas consideradas**: Cadena `detail` única (rechazado — el frontend no puede distinguir
superposición de aviso previo sin analizar texto); HTTP 400 para todos los errores de dominio
(rechazado — 422 es semánticamente correcto para fallas de validación, 400 para solicitudes malformadas).

---

## Decisión 8: Estrategia de Pruebas

**Decisión**:
- **Pruebas unitarias** (`tests/unit/domain/`): Prueban `TimeSlot` y entidades de dominio sin
  base de datos. Inyectan repositorios fake/stub. Cubren las 6 reglas de dominio, condiciones
  límite y la lógica de transición al estado `completed`.
- **Pruebas de integración** (`tests/integration/test_api.py`): Usan `TestClient` de FastAPI
  (respaldado por httpx) con una base de datos SQLite en memoria. Prueban el camino feliz de
  cada endpoint y los principales caminos de error. No duplican las pruebas límite a nivel unitario.

**Justificación**: Las pruebas unitarias son rápidas y aisladas — ideales para la cobertura
exhaustiva de reglas de dominio que requiere TDD. Las pruebas de integración verifican el
cableado (FastAPI → casos de uso → repositorio → SQLite) sin duplicar las pruebas de lógica
de dominio.

**Alternativas consideradas**: Solo pruebas de integración (rechazado — demasiado lentas para
el ciclo TDD, la configuración de BD hace verbose las pruebas límite); mockear SQLAlchemy en
pruebas unitarias (rechazado — usamos repositorios fake en su lugar, que es más limpio y
agnóstico a la BD).

---

## Decisión 9: Carga Inicial de Canchas

**Decisión**: `infrastructure/seed.py` contiene una función `seed_fields()` llamada durante el
evento de inicio `lifespan` de FastAPI. Carga 3 canchas ("Cancha A", "Cancha B", "Cancha C")
si la tabla de canchas está vacía. Idempotente — seguro de llamar en cada reinicio.

**Justificación**: El panel de administración está fuera del alcance. Las canchas deben existir
antes de que puedan hacerse reservas. Cargar al inicio es el enfoque más simple sin overhead
de herramientas de migración.

**Alternativas consideradas**: Scripts de migración (rechazado — sobreingeniería para 3 registros
estáticos); fixtures en pruebas (aún necesarios, pero por separado de la carga de producción).
