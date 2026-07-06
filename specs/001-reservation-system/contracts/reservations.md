# Contrato: API de Reservas

Ruta base: `/api/reservations`

---

## POST /api/reservations

Crea una nueva reserva. Se aplican todas las reglas de dominio; se retorna un error específico
para cada violación.

### Cuerpo de la Solicitud

```json
{
  "user_id":    "string (requerido, no vacío)",
  "field_id":   1,
  "date":       "YYYY-MM-DD",
  "start_time": "HH:MM",
  "end_time":   "HH:MM"
}
```

### Respuestas

**201 Created** — Reserva confirmada.

```json
{
  "reservation_id": "550e8400-e29b-41d4-a716-446655440000",
  "user_id":        "maria",
  "field_id":       1,
  "field_name":     "Cancha A",
  "date":           "2026-07-01",
  "start_time":     "10:00",
  "end_time":       "12:00",
  "status":         "active"
}
```

**422 Unprocessable Entity** — Regla de dominio violada.

```json
{
  "error_type": "<TIPO_DE_ERROR>",
  "message":    "Explicación legible por humanos de la violación de regla."
}
```

| `error_type` | Se activa cuando |
|--------------|-----------------|
| `OVERLAP` | El rango de tiempo solicitado se superpone con una reserva activa existente en la misma cancha |
| `DURATION_INVALID` | La duración es menor a 1 hora |
| `INVALID_BLOCK` | La hora de inicio o fin no está alineada a un límite de 30 minutos |
| `OPERATING_HOURS` | La hora de inicio es antes de las 06:00 o la hora de fin es después de las 23:00 |
| `ADVANCE_NOTICE` | La hora de inicio es menos de 1 hora desde el momento actual |
| `ACTIVE_LIMIT` | El usuario ya tiene 2 reservas activas (futuras, no canceladas) |
| `FIELD_NOT_FOUND` | El `field_id` proporcionado no existe |

---

## GET /api/reservations

Retorna todas las próximas reservas activas del usuario de la sesión. Solo se retornan
las reservas cuya hora de fin está en el futuro y que no han sido canceladas.

### Solicitud

| Parámetro | Ubicación | Tipo   | Requerido | Descripción |
|-----------|-----------|--------|-----------|-------------|
| `user_id` | query     | string | Sí        | El identificador del usuario de la sesión |

### Respuestas

**200 OK** — Lista retornada (puede estar vacía).

```json
[
  {
    "reservation_id": "550e8400-e29b-41d4-a716-446655440000",
    "field_name":     "Cancha A",
    "date":           "2026-07-01",
    "start_time":     "10:00",
    "end_time":       "12:00",
    "status":         "active"
  }
]
```

- Retorna `[]` si el usuario no tiene próximas reservas activas (no es un error).
- Las reservas completadas (pasadas) y canceladas están excluidas.

---

## DELETE /api/reservations/{reservation_id}

Cancela una reserva activa. Si la cancelación se envía con menos de 2 horas de aviso
previo antes de la hora de inicio de la reserva, también se registra un no-show.

### Solicitud

| Parámetro        | Ubicación | Tipo   | Requerido | Descripción |
|------------------|-----------|--------|-----------|-------------|
| `reservation_id` | path      | string | Sí        | UUID de la reserva |

**Cuerpo de la solicitud**:

```json
{
  "user_id": "string (requerido)"
}
```

### Respuestas

**200 OK** — Reserva cancelada.

```json
{
  "reservation_id": "550e8400-e29b-41d4-a716-446655440000",
  "status":         "cancelled",
  "no_show":        false
}
```

- `no_show: true` cuando la cancelación fue registrada con menos de 2 horas de aviso previo.

**403 Forbidden** — La reserva no pertenece al usuario solicitante.

```json
{
  "error_type": "NOT_AUTHORIZED",
  "message":    "Esta reserva no pertenece al identificador de usuario proporcionado."
}
```

**404 Not Found** — La reserva no existe.

```json
{
  "error_type": "NOT_FOUND",
  "message":    "No se encontró ninguna reserva con el identificador proporcionado."
}
```

**400 Bad Request** — La reserva ya está cancelada.

```json
{
  "error_type": "ALREADY_CANCELLED",
  "message":    "Esta reserva ya ha sido cancelada."
}
```

---

## Convenciones Comunes

- Todos los tiempos usan formato de 24 horas: `"HH:MM"` (p. ej., `"06:00"`, `"22:30"`).
- All dates use ISO-8601: `"YYYY-MM-DD"`.
- `reservation_id` is a UUID v4 string.
- The backend operates in a single fixed timezone (local server time). No timezone
  conversion is performed.
