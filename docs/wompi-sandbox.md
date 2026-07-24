# Guía: probar pagos en vivo con el sandbox de Wompi (T063)

Esta guía cierra la validación **en vivo** de la feature de pagos (`002-payments-gateway`). No es
necesaria para que el código sea correcto — los 44 tests automatizados ya cubren la lógica con un
gateway simulado — pero confirma el flujo real contra el proveedor antes de producción.

> ⚠️ **Nunca** pegues las llaves en el código, en commits ni en un chat. Se cargan localmente con
> `dotnet user-secrets` y se quedan en tu máquina.

---

## 1. Conseguir las llaves (sandbox)

1. Crea una cuenta de comercio en Wompi (Bancolombia).
2. Entra al ambiente **Sandbox / Pruebas** del panel del comercio.
3. Copia estos 4 valores (verifica los nombres exactos en tu panel; el prefijo puede variar):

| Valor | Prefijo típico | Uso |
|-------|----------------|-----|
| Llave pública | `pub_test_...` | Arma la URL de checkout |
| Llave privada | `prv_test_...` | Consultar estado y solicitar reembolsos (Bearer) |
| Secreto de integridad | `test_integrity_...` | Firma monto/referencia del checkout (anti-manipulación) |
| Secreto de eventos | `test_events_...` | Verifica la firma de los webhooks |

---

## 2. Cargar las llaves sin exponerlas

Solo la pública puede ir en `appsettings.json`. Las 3 secretas van en **user-secrets**:

```bash
cd dotnet-backend/CanchasSinteticas.Api
dotnet user-secrets set "Payments:Wompi:PublicKey"       "pub_test_xxx"
dotnet user-secrets set "Payments:Wompi:PrivateKey"      "prv_test_xxx"
dotnet user-secrets set "Payments:Wompi:IntegritySecret" "test_integrity_xxx"
dotnet user-secrets set "Payments:Wompi:EventsSecret"    "test_events_xxx"
```

Verifica el `BaseUrl` en `appsettings.json` → `Payments:Wompi:BaseUrl` = `https://sandbox.wompi.co/v1`.

> 🔒 Con el fix de seguridad aplicado, si `EventsSecret` está vacío el webhook **rechaza todos los
> eventos** (fail-closed). Por eso este paso es obligatorio para que la confirmación funcione.

---

## 3. Exponer el webhook con un túnel HTTPS

Wompi debe poder **llamar a tu API** cuando el pago cambia de estado, pero corre en `localhost`. Un
túnel publica tu `localhost:8080` con una URL pública temporal:

```bash
# opción A: ngrok
ngrok http 8080            # → https://ab12.ngrok.io

# opción B: cloudflared
cloudflared tunnel --url http://localhost:8080
```

En el panel de Wompi, registra la URL del webhook:

```
https://<tu-tunel>/api/payments/webhook
```

---

## 4. Levantar la aplicación

```bash
# backend
cd dotnet-backend
dotnet run --project CanchasSinteticas.Api      # http://localhost:8080

# frontend (otra terminal)
cd frontend
npm run dev                                     # http://localhost:5173
```

Inicia sesión con una cuenta demo (ver README): `cliente@canchas.co` / `password123`.

---

## 5. Escenarios a validar (A–H del quickstart)

| # | Escenario | Resultado esperado |
|---|-----------|--------------------|
| A | Pagar una reserva y **aprobar** en el checkout sandbox | La reserva pasa a `confirmed`; `GET /api/payments/{id}` → `paid` con `gateway_reference`; hay comprobante |
| B | Iniciar el pago y **rechazarlo** o dejarlo expirar (15 min) | Pago `rejected`/`expired`; la franja vuelve a estar disponible sin intervención |
| C | Reenviar el mismo evento aprobado al webhook | Un único cambio a `paid`, un único comprobante (idempotencia) |
| D | Enviar un evento con **checksum inválido** | El webhook no cambia ningún estado |
| E | Abrir un partido con pago dividido y pagar todas las partes | `amount_collected` sube por parte; al cubrir el total, la reserva del partido queda `confirmed` |
| F | Descargar el comprobante (PDF) tras un pago aprobado | Descarga el PDF; con otro usuario → **403** |
| G | Cancelar dentro de la ventana de la sede / fuera de ella | Dentro → `refund_status: refunded`; fuera (tardío) → `none` |
| H | Cambiar el modelo de recaudo de una sede (dueño) | `marketplace` ↔ `direct` se guarda y aplica |

### Tarjetas y usuarios de prueba

Wompi publica en su documentación de sandbox las **tarjetas de prueba** y los datos para simular
Nequi/PSE aprobados o rechazados. Úsalos en el checkout para forzar cada resultado (A vs B).

---

## 6. Cómo verificar cada paso

- **Estado del pago:** `GET /api/payments/{paymentId}` (con el token del cliente) o en la UI
  (la página de "Mis reservas" hace *polling* automático).
- **Webhook recibido:** revisa los logs del backend (`[notif:app] ...`) y el panel de eventos de
  Wompi (entrega/reintentos).
- **Idempotencia (C):** reenvía el evento desde el panel de Wompi o repite el POST al webhook con el
  mismo cuerpo; el estado no debe cambiar dos veces.

---

## 7. Notas

- La persistencia es **en memoria**: si reinicias el backend, se pierden los pagos `pending`/
  `processing` en curso. Reinicia el flujo desde una reserva nueva.
- Los canales de correo/WhatsApp están desactivados por defecto (`Payments:Notifications`); actívalos
  solo cuando conectes un proveedor de mensajería real.
- Para producción: cambia `BaseUrl` a `https://production.wompi.co/v1`, usa llaves `prod`, sirve la
  API por HTTPS con dominio propio (sin túnel) y considera rate-limiting en el webhook.
