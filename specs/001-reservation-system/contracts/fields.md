# Contrato: API de Canchas

Ruta base: `/api/fields`

---

## GET /api/fields/availability

Retorna todas las canchas con sus franjas horarias de 30 minutos disponibles para la fecha solicitada.
Las franjas disponibles excluyen los rangos de tiempo ya ocupados por reservas activas y los rangos
de tiempo que no pueden reservarse debido a la regla de aviso previo de 1 hora.

### Solicitud

| Parámetro | Ubicación | Tipo   | Requerido | Descripción |
|-----------|-----------|--------|-----------|-------------|
| `date`    | query     | string | Sí        | Fecha objetivo en formato `YYYY-MM-DD` |

### Respuestas

**200 OK** — Disponibilidad retornada exitosamente.

```json
[
  {
    "field_id": 1,
    "field_name": "Cancha A",
    "available_slots": [
      { "start_time": "06:00", "end_time": "06:30" },
      { "start_time": "06:30", "end_time": "07:00" },
      { "start_time": "07:00", "end_time": "07:30" }
    ]
  },
  {
    "field_id": 2,
    "field_name": "Cancha B",
    "available_slots": []
  }
]
```

- `available_slots` es un array de bloques consecutivos de 30 minutos que están libres.
- Un array `available_slots` vacío significa que la cancha está completamente reservada (o que todas
  las franjas restantes caen dentro de la ventana de aviso previo de 1 hora).

**400 Bad Request** — La fecha está en el pasado o tiene formato incorrecto.

```json
{
  "error_type": "INVALID_DATE",
  "message": "La fecha solicitada está en el pasado. Por favor elige hoy o una fecha futura."
}
```

### Notas

- Las cadenas de tiempo usan formato de 24 horas: `"HH:MM"` (p. ej., `"06:00"`, `"23:00"`).
- La última franja posible comienza a las `22:30` (termina a las `23:00`).
- Las franjas se retornan en orden ascendente.
- Las franjas ya iniciadas o dentro de la ventana de aviso previo de 1 hora están excluidas.
