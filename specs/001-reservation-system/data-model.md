# Modelo de Datos: Sistema de Reservas para Canchas de Fútbol Sintético

**Salida de Fase 1** — entidades, objetos de valor, transiciones de estado y esquema de persistencia.

---

## Entidades de Dominio

### Cancha (Field)

Representa una cancha de fútbol sintético disponible para reserva.

| Atributo | Tipo   | Restricciones |
|----------|--------|---------------|
| id       | int    | Clave primaria, auto-incremento |
| name     | string | No vacío, único |

Las canchas se pre-cargan al inicio. Sin transiciones de ciclo de vida — las canchas
son permanentes dentro del MVP.

---

### Reserva (Reservation)

Representa una reserva de una cancha específica por un usuario para un rango de tiempo definido.

| Atributo     | Tipo     | Restricciones |
|--------------|----------|---------------|
| id           | UUID     | Clave primaria, generado en creación |
| user_id      | string   | No vacío; proporcionado por el usuario |
| field_id     | int      | Clave foránea → Field.id |
| date         | date     | La fecha calendario de la reserva |
| start_time   | time     | Alineado al límite de 30 min; ≥ 06:00 |
| end_time     | time     | Alineado al límite de 30 min; ≤ 23:00; > start_time + 1h |
| status       | enum     | `active` · `cancelled` — almacenado en BD |
| created_at   | datetime | Establecido en creación, UTC |
| cancelled_at | datetime | Nullable; establecido cuando se cancela |

**Estado computado — `completed`**: Una reserva con `status = active` cuyo
`date + end_time` está en el pasado es tratada como `completed` por la aplicación.
Esto NO se almacena en la base de datos; se computa en tiempo de consulta.

**Transiciones de estado**:

```
creado ──► activo ──► cancelado   (acción explícita del usuario; puede generar no-show)
                 └──► [completado]  (computado — end_datetime < now())
```

**Definición de activo** (usada para el límite de 2 reservas):
Una reserva está activa si `status = 'active'` Y `(date, end_time)` está en el futuro.
Las reservas canceladas y pasadas nunca cuentan para el límite.

---

### NoShow

Registra que un usuario canceló una reserva con menos de 2 horas de aviso previo.

| Atributo        | Tipo     | Restricciones |
|-----------------|----------|---------------|
| id              | int      | Clave primaria, auto-incremento |
| reservation_id  | UUID     | Clave foránea → Reservation.id |
| user_id         | string   | Copiado de la reserva cancelada |
| cancelled_at    | datetime | Marca de tiempo de la cancelación, UTC |

Un registro NoShow se crea junto con la cancelación cuando:
`reservation.start_datetime - cancellation_time < 2 hours`

No se aplica ninguna consecuencia automatizada en el MVP (sin baneos ni penalizaciones).

---

## Objeto de Valor: TimeSlot

Encapsula el rango de fecha + hora de una reserva y aplica todas las restricciones
de reserva como lógica pura, independiente del framework.

| Atributo   | Tipo | Descripción |
|------------|------|-------------|
| date       | date | Fecha calendario |
| start_time | time | Inicio de la ventana de reserva |
| end_time   | time | Fin de la ventana de reserva |

**Reglas de validación** (todas aplicadas en construcción):

| Regla | Condición |
|-------|-----------|
| Duración mínima | `end_time - start_time >= 1 hora` |
| Alineación de 30 minutos | `start_time.minute in {0, 30}` Y `end_time.minute in {0, 30}` |
| Horario operativo — inicio | `start_time >= 06:00` |
| Horario operativo — fin | `end_time <= 23:00` |
| Sin cruce de medianoche | `end_time > start_time` (mismo día calendario) |

**Comportamientos**:

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `is_bookable(now: datetime)` | bool | `True` si `date + start_time - now >= 1 hora` |
| `overlaps_with(other: TimeSlot)` | bool | `True` si las dos franjas comparten algún tiempo en la misma fecha y cancha |

---

## Esquema de Persistencia (SQLite)

Los modelos ORM en `infrastructure/models/` mapean a estas tablas. Son clases Python
separadas de las entidades de dominio — sin clase base compartida ni herencia.

```sql
CREATE TABLE fields (
    id   INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT    NOT NULL UNIQUE
);

CREATE TABLE reservations (
    id           TEXT     PRIMARY KEY,          -- UUID como texto
    user_id      TEXT     NOT NULL,
    field_id     INTEGER  NOT NULL REFERENCES fields(id),
    date         TEXT     NOT NULL,             -- Fecha ISO-8601: YYYY-MM-DD
    start_time   TEXT     NOT NULL,             -- HH:MM (24h)
    end_time     TEXT     NOT NULL,             -- HH:MM (24h)
    status       TEXT     NOT NULL DEFAULT 'active',  -- 'active' | 'cancelled'
    created_at   TEXT     NOT NULL,             -- Datetime ISO-8601 UTC
    cancelled_at TEXT     NULL                  -- Datetime ISO-8601 UTC
);

CREATE TABLE no_shows (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    reservation_id  TEXT    NOT NULL REFERENCES reservations(id),
    user_id         TEXT    NOT NULL,
    cancelled_at    TEXT    NOT NULL             -- Datetime ISO-8601 UTC
);
```

**Índice para verificación de superposición** (creado al inicio):
```sql
CREATE INDEX idx_reservations_field_date
    ON reservations(field_id, date, status);
```

**Consulta para conteo de reservas activas** (usada por la regla del límite máximo de 2):
```sql
SELECT COUNT(*) FROM reservations
WHERE user_id = :user_id
  AND status  = 'active'
  AND (date > :today
       OR (date = :today AND end_time > :current_time));
```

---

## Mapper: Entidad de Dominio ↔ Modelo ORM

Cada implementación de repositorio mapea entre modelos ORM y entidades de dominio. El dominio
nunca importa SQLAlchemy. La infraestructura nunca expone modelos SQLAlchemy fuera de
su propio paquete.

| Entidad de Dominio | Modelo ORM | Ubicación del Mapper |
|--------------------|------------|----------------------|
| `Field` | `FieldModel` | `sqlite_field_repository.py` |
| `Reservation` | `ReservationModel` | `sqlite_reservation_repository.py` |
| `NoShow` | `NoShowModel` | `sqlite_reservation_repository.py` |
