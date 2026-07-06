# Inicio Rápido: Guía de Validación del Sistema de Reservas

Usa esta guía para validar que el sistema funciona de extremo a extremo después de la implementación.
Cubre los prerequisitos, el inicio y la verificación escenario por escenario.

---

## Prerequisitos

- Python 3.11+ instalado
- Node.js 20+ instalado
- Raíz del repositorio: `c:\Users\jmgonzaleh\canchas-sinteticas\`

---

## Configuración e Inicio del Backend

```bash
cd backend
python -m venv .venv
.venv\Scripts\activate          # Windows PowerShell
pip install -r requirements.txt
uvicorn api.main:app --reload --port 8000
```

Al iniciar, el backend:
1. Crea `reservations.db` (archivo SQLite) si no existe.
2. Ejecuta la creación de tablas.
3. Carga 3 canchas: **Cancha A**, **Cancha B**, **Cancha C** (idempotente).

Verificar inicio: `GET http://localhost:8000/api/fields/availability?date=2026-07-01`
debe retornar 3 canchas con franjas, HTTP 200.

---

## Ejecutar Pruebas del Backend

```bash
cd backend
pytest tests/ -v --cov=. --cov-report=term-missing
```

Todas las pruebas deben pasar antes de proceder a la validación del frontend.
Las pruebas unitarias de dominio DEBEN ejecutarse sin ninguna conexión a base de datos.

---

## Configuración e Inicio del Frontend

```bash
cd frontend
npm install
npm run dev
```

Abrir `http://localhost:5173` en un navegador.

---

## Validación de Escenarios

### Escenario 1 — Ingreso de Identificador (prerequisito US1)

1. Abrir `http://localhost:5173`.
2. La aplicación muestra una pantalla de ingreso de identificador.
3. Ingresar `"maria"` y confirmar.
4. Se abre la vista principal. El identificador no se vuelve a solicitar durante la sesión.

**Esperado**: La puerta de identificador se muestra una vez; la vista principal es accesible después del ingreso.

---

### Escenario 2 — Ver Disponibilidad de Canchas (Historia de Usuario 1)

1. En la vista principal, seleccionar la fecha de hoy (o mañana).
2. El sistema muestra Cancha A, Cancha B, Cancha C con sus franjas de 30 min disponibles.

**Esperado**: Cada cancha muestra franjas de 06:00 a 23:00 (menos las ya reservadas,
menos las franjas dentro de 1 hora desde ahora).

Equivalente API:
```
GET /api/fields/availability?date=<hoy>
→ 200, 3 canchas cada una con array available_slots
```

---

### Escenario 3 — Crear una Reserva Válida (Historia de Usuario 2)

1. Seleccionar **Cancha A**, una fecha al menos 2 horas en el futuro, 10:00–12:00.
2. Enviar el formulario de reserva.
3. Se muestra la confirmación con un ID de reserva.

**Esperado**: HTTP 201, la reserva aparece en la lista "Mis Reservas".

Equivalente API:
```
POST /api/reservations
Body: {"user_id":"maria","field_id":1,"date":"<futuro>","start_time":"10:00","end_time":"12:00"}
→ 201, el cuerpo contiene reservation_id y status: "active"
```

---

### Escenario 4 — Regla de Dominio: Superposición (Historia de Usuario 2, camino de error)

1. Intentar reservar **Cancha A** de 10:00 a 11:00 en la misma fecha que el Escenario 3.

**Esperado**: Mensaje de error "Esta franja horaria se superpone con una reserva existente."
API → 422, `error_type: "OVERLAP"`.

---

### Escenario 5 — Regla de Dominio: Aviso Previo (Historia de Usuario 2, camino de error)

1. Intentar reservar cualquier cancha para una franja que comienza en menos de 60 minutos.

**Esperado**: Mensaje de error sobre el requisito de aviso previo de 1 hora.
API → 422, `error_type: "ADVANCE_NOTICE"`.

---

### Escenario 6 — Regla de Dominio: Duración / Alineación de Bloques (Historia de Usuario 2, camino de error)

1. Intentar reservar de 10:00 a 10:30 (solo 30 minutos, por debajo del mínimo de 1 hora).

**Esperado**: Mensaje de error sobre duración mínima.
API → 422, `error_type: "DURATION_INVALID"`.

---

### Escenario 7 — Regla de Dominio: Límite de Reservas Activas (Historia de Usuario 2, camino de error)

1. Como `"maria"`, crear una segunda reserva válida en otra cancha u horario.
2. Intentar crear una tercera reserva.

**Esperado**: Mensaje de error sobre el límite activo de 2 reservas.
API → 422, `error_type: "ACTIVE_LIMIT"`.

---

### Escenario 8 — Ver Mis Reservas (Historia de Usuario 3)

1. Después de crear reservas en el Escenario 3, navegar a "Mis Reservas".

**Esperado**: La lista muestra las reservas futuras activas de `"maria"`.
API → `GET /api/reservations?user_id=maria` → 200, array con elementos de reserva.

---

### Escenario 9 — Cancelar con Suficiente Aviso (Historia de Usuario 4)

1. Cancelar la reserva del Escenario 3 (la reserva está horas en el futuro).

**Esperado**: El estado cambia a `cancelled`. `no_show: false`.
API → `DELETE /api/reservations/<id>` con `{"user_id":"maria"}` → 200.

---

### Escenario 10 — Cancelar con Poco Aviso / No-Show (Historia de Usuario 4)

Requiere una reserva con hora de inicio a menos de 2 horas.

1. Crear una reserva que empiece ~90 minutos desde ahora.
2. Cancelarla inmediatamente.

**Esperado**: Estado `cancelled`, `no_show: true`.
API → 200, `{"no_show": true}`.

---

### Escenario 11 — Cancelación No Autorizada (Historia de Usuario 4, camino de error)

1. Como `"pedro"`, intentar cancelar una reserva que pertenece a `"maria"`.

**Esperado**: 403, `error_type: "NOT_AUTHORIZED"`.

---

### Escenario 12 — Estado Vacío (Historia de Usuario 3, caso límite)

1. Usar un identificador de usuario que nunca ha hecho una reserva (p. ej., `"nuevousuario"`).
2. Ver "Mis Reservas".

**Esperado**: Se muestra una lista vacía con un mensaje claro. Sin error.
API → `GET /api/reservations?user_id=nuevousuario` → 200, `[]`.

---

## Criterios de Finalización

Los 12 escenarios deben pasar antes de que la funcionalidad se considere completa:

- [ ] Escenario 1: La puerta de identificador funciona
- [ ] Escenario 2: La vista de disponibilidad retorna las franjas correctas
- [ ] Escenario 3: Reserva válida creada
- [ ] Escenario 4: Superposición rechazada
- [ ] Escenario 5: Aviso previo rechazado
- [ ] Escenario 6: Duración/bloque rechazado
- [ ] Escenario 7: Límite activo rechazado
- [ ] Escenario 8: La lista de mis reservas funciona
- [ ] Escenario 9: Cancelación limpia funciona
- [ ] Escenario 10: Cancelación tardía crea no-show
- [ ] Escenario 11: Cancelación no autorizada rechazada
- [ ] Escenario 12: Estado vacío manejado
