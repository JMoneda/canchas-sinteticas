# Canchas Sintéticas API

API REST para la gestión de reservas de canchas de fútbol sintético. Permite consultar disponibilidad, crear y cancelar reservas con validaciones automáticas.

## 🚀 Características

- **Consultar disponibilidad** de canchas por fecha
- **Crear reservas** con validaciones automáticas:
  - Evita solapamiento de reservas
  - Requiere mínimo 24 horas de anticipación
  - Máximo 3 reservas activas por usuario
- **Listar tus reservas** activas y próximas
- **Cancelar reservas** con registro de no-shows si es tardío
- **Swagger UI** para probar endpoints interactivamente

## 📋 Stack Técnico

- **.NET 10** con ASP.NET Core
- **SQLite** como base de datos
- **Clean Architecture** (Domain, Application, Infrastructure, API)
- **Dependency Injection** nativo de ASP.NET Core
- **Swagger/Swashbuckle** para documentación
- **CORS** configurado para frontend local

## 🛠️ Instalación y Configuración

### Requisitos previos
- .NET 10 SDK o superior
- PowerShell (recomendado) o terminal compatible

### Pasos de instalación

1. **Clonar el repositorio**
```bash
git clone https://github.com/JMoneda/canchas-sinteticas.git
cd canchas-sinteticas
```

2. **Restaurar dependencias**
```bash
cd dotnet-backend
dotnet restore
```

3. **Levantar la aplicación**
```bash
cd CanchasSinteticas.Api
dotnet run
```

La API estará disponible en `https://localhost:7001` (o el puerto asignado)

### Base de datos

La base de datos SQLite se crea automáticamente en la primera ejecución con datos de ejemplo:
- **3 canchas** (Cancha A, Cancha B, Cancha C)
- **Turnos de 1 hora** de 09:00 a 22:00 (todos los días)

Para reimportar datos:
```bash
dotnet run --reset-db
```

## 📡 Endpoints

### 1. Consultar Disponibilidad

```http
GET /api/fields/availability?date=2025-02-15
```

**Parámetros:**
- `date` (string, required): Fecha en formato `YYYY-MM-DD`

**Respuesta (200):**
```json
[
  {
    "field_id": "field-1",
    "field_name": "Cancha A",
    "available_slots": [
      { "time": "09:00", "is_available": true },
      { "time": "10:00", "is_available": false },
      { "time": "11:00", "is_available": true }
    ]
  }
]
```

**Errores:**
- `400`: Fecha inválida o en el pasado

---

### 2. Crear Reserva

```http
POST /api/reservations
Content-Type: application/json

{
  "user_id": "user123",
  "field_id": "field-1",
  "date": "2025-02-15",
  "start_time": "14:00",
  "end_time": "15:00"
}
```

**Parámetros (body):**
- `user_id` (string, required): ID del usuario
- `field_id` (string, required): ID de la cancha
- `date` (string, required): Fecha en formato `YYYY-MM-DD`
- `start_time` (string, required): Hora inicio en formato `HH:MM`
- `end_time` (string, required): Hora fin en formato `HH:MM`

**Respuesta (201):**
```json
{
  "reservation_id": "res-abc123",
  "user_id": "user123",
  "field_id": "field-1",
  "field_name": "Cancha A",
  "date": "2025-02-15",
  "start_time": "14:00",
  "end_time": "15:00",
  "status": "active",
  "created_at": "2025-02-10T10:30:00Z"
}
```

**Errores:**
- `400`: Campos faltantes o inválidos
- `422`: Violación de reglas de negocio:
  - Turno ya reservado
  - Menos de 24h de anticipación
  - Usuario con 3 reservas activas

---

### 3. Listar Reservas del Usuario

```http
GET /api/reservations?user_id=user123
```

**Parámetros:**
- `user_id` (string, required): ID del usuario

**Respuesta (200):**
```json
[
  {
    "reservation_id": "res-abc123",
    "field_name": "Cancha A",
    "date": "2025-02-15",
    "start_time": "14:00",
    "end_time": "15:00",
    "status": "active",
    "created_at": "2025-02-10T10:30:00Z"
  }
]
```

**Errores:**
- `400`: `user_id` no proporcionado

---

### 4. Cancelar Reserva

```http
DELETE /api/reservations/{reservationId}
Content-Type: application/json

{
  "user_id": "user123"
}
```

**Parámetros:**
- `reservationId` (path, required): ID de la reserva
- `user_id` (body, required): ID del usuario (validación)

**Respuesta (200):**
```json
{
  "reservation_id": "res-abc123",
  "status": "cancelled",
  "is_no_show": false,
  "cancelled_at": "2025-02-10T11:00:00Z"
}
```

**Valores de `is_no_show`:**
- `false`: Cancelación con ≥2 horas de anticipación
- `true`: Cancelación con <2 horas de anticipación (registrado como no-show)

**Errores:**
- `400`: Datos inválidos
- `403`: Usuario no autorizado para cancelar esa reserva
- `404`: Reserva no encontrada

---

## 🔍 Documentación Interactiva

Una vez que levantes la aplicación, accede a Swagger UI:

```
https://localhost:7001/swagger/index.html
```

Desde allí puedes probar todos los endpoints directamente.

## 📁 Estructura del Proyecto

```
dotnet-backend/
├── CanchasSinteticas.Api/           # Capa de presentación (controllers)
│   ├── Controllers/
│   │   ├── FieldsController.cs
│   │   └── ReservationsController.cs
│   └── Program.cs
├── CanchasSinteticas.Application/   # Lógica de negocio (use cases)
├── CanchasSinteticas.Domain/        # Modelos de dominio
├── CanchasSinteticas.Infrastructure/ # Acceso a datos, base de datos
└── CanchasSinteticas.slnx          # Archivo de solución
```

## 🔧 Desarrollo

### Compilar
```bash
dotnet build
```

### Ejecutar tests
```bash
dotnet test
```

### Publicar (release)
```bash
dotnet publish -c Release -o ./dist
```

## ⚙️ Configuración

### Variables de entorno

Crea un archivo `appsettings.Development.json` en `CanchasSinteticas.Api/`:

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=reservations.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

### CORS

Por defecto, el frontend en `http://localhost:5173` está autorizado. Para agregar otros orígenes, edita `Program.cs`.

## 🐛 Troubleshooting

### Error: "SQLite database is locked"
- Asegúrate que no hay otra instancia ejecutándose
- Reinicia la aplicación

### Error: "Port already in use"
```bash
# Cambiar puerto en launchSettings.json o variable ASPNETCORE_URLS
set ASPNETCORE_URLS=https://localhost:7002
dotnet run
```

### Swagger no carga
- Verifica que `GenerateDocumentationFile` está habilitado en el .csproj
- Limpia y reconstruye: `dotnet clean && dotnet build`

## 📝 Validaciones de Negocio

| Regla | Validación |
|-------|-----------|
| Anticipación mínima | 24 horas antes del turno |
| Máximo de reservas activas | 3 por usuario |
| Cobertura horaria | 09:00 - 22:00 |
| Duración mínima | 1 hora |
| Solapamiento | No permitido |

---

## 💳 Pagos (feature 002-payments-gateway)

Integración de pasarela real (Wompi, detrás de `IPaymentGateway`), pago dividido entre jugadores,
comprobantes en PDF y reembolsos. **Regla de Dominio 7**: el estado del pago sólo pasa a *aprobado*
tras la confirmación verificada del proveedor por webhook; nunca de forma optimista.

### Endpoints

| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/api/reservations/{id}/pay` | Inicia el pago (devuelve `checkout_url`; pago en `processing`). |
| GET | `/api/payments/{id}` | Consulta el estado del pago (polling). |
| POST | `/api/payments/webhook` | **Público**; eventos del proveedor (firma verificada + idempotencia). |
| POST | `/api/matches/{id}/pay-share` | Paga la parte del jugador en un partido con pago dividido. |
| GET | `/api/reservations/{id}/receipt` | Comprobante (PDF; `?format=json` para datos). |
| GET | `/api/matches/{id}/players/me/receipt` | Comprobante de la parte del jugador. |
| DELETE | `/api/reservations/{id}` | Cancela y reembolsa según la política de la sede (`refund_status`). |
| GET/PUT | `/api/owner/venues/{id}/payment-config` | Modelo de recaudo: `marketplace` o `direct`. |

Métodos soportados (Colombia): `nequi`, `pse`, `bancolombia_transfer`, `bancolombia_button`,
`bancolombia_qr`, `card`.

### Secretos (no versionar)

```bash
cd CanchasSinteticas.Api
dotnet user-secrets set "Payments:Wompi:PrivateKey"      "prv_test_xxx"
dotnet user-secrets set "Payments:Wompi:EventsSecret"    "events_test_xxx"
dotnet user-secrets set "Payments:Wompi:IntegritySecret" "integrity_test_xxx"
```

En producción, por variables de entorno / gestor de secretos. Para recibir webhooks en desarrollo,
exponer la API local con un túnel HTTPS y registrar `https://<túnel>/api/payments/webhook`.

### Seguridad

- El webhook valida la **firma (SHA-256 + events secret)**; los eventos no auténticos no cambian estado.
- Procesamiento **idempotente** (`ProcessedWebhookEvent`): reenvíos no duplican cobros ni confirmaciones.
- Comprobantes accesibles sólo por el titular del pago y el dueño de la sede.
- Ningún secreto en código ni en logs; el webhook no expone detalles internos.

### Notas de MVP

- Persistencia en memoria: un reinicio pierde pagos `Pending`/`Processing` (reconciliables con el
  proveedor). Migración a EF Core habilitada por las interfaces `IRepository`.
- Correo y WhatsApp/SMS: activables por `Payments:Notifications` (adaptador real por conectar).
- Marketplace: liquida el 100% al dueño; la comisión de plataforma queda fuera del MVP.
| No-show | Si se cancela con <2h anticipación |

## 📞 Soporte

Para reportar bugs o sugerir mejoras, abre un [issue en GitHub](https://github.com/JMoneda/canchas-sinteticas/issues).

## 📄 Licencia

Este proyecto es de uso privado. Todos los derechos reservados.

---

**Última actualización:** Febrero 2025  
**Versión API:** v1  
**Estado:** En desarrollo
