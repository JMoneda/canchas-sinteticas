# Quickstart: verificación del rediseño + validación

Guía de verificación end-to-end. No incluye código de implementación (eso vive en `tasks.md`).

## Prerrequisitos

- Backend .NET corriendo (para datos y auth). Desde `dotnet-backend/`: `dotnet run --project CanchasSinteticas.Api`.
- Node instalado. Desde `frontend/`: `npm install` (ya instalado; sin libs nuevas).

## Puertas automáticas

```powershell
cd frontend
npm run type-check   # tsc -b --noEmit → 0 errores
npm run build        # vite build → build exitoso
npm run dev          # levanta la SPA
```

Ambas puertas (`type-check`, `build`) DEBEN pasar antes de dar por completa la feature.

## Escenarios de validación (User Story 1 — P1)

1. **Contraseña débil** → en `/registro`, escribir `123456` en contraseña.
   - Esperado: error "Debe incluir letras y números" o "Mínimo 8 caracteres", medidor en "Muy débil/Débil",
     botón "Crear cuenta" deshabilitado. (SC-001)
2. **Confirmación no coincide** → contraseña `Futbol2026`, confirmación `Futbol2027`.
   - Esperado: error "Las contraseñas no coinciden"; envío bloqueado.
3. **Correo inválido** → `juan@` y quitar foco.
   - Esperado: error "Correo no válido".
4. **Teléfono opcional vacío** → dejar teléfono vacío con lo demás válido.
   - Esperado: sin error de teléfono; envío permitido.
5. **Teléfono inválido** → `abc123`.
   - Esperado: error "Teléfono no válido".
6. **Registro válido (cronometrado)** → nombre, correo válido, contraseña `Futbol2026`, confirmación igual.
   - Esperado: cuenta creada; redirección por rol (cliente `/`, dueño `/panel`).
   - Medir: un usuario nuevo completa el registro válido en **< 2 minutos** y ningún mensaje de
     error mostrado es ambiguo (cada error dice qué corregir). (SC-006)
7. **Error de backend** → registrar un correo ya existente.
   - Esperado: mensaje de error visible; los campos NO se borran. (FR-021)

## Escenarios de validación (Login y Panel — P2)

8. **Login inválido** → correo vacío/mal formado o contraseña vacía.
   - Esperado: error por campo con el mismo estilo del registro; botón deshabilitado. (FR-019)
9. **Panel de dueño** → en un formulario del panel, dejar un requerido vacío e intentar guardar.
   - Esperado: error por campo y guardado bloqueado. (FR-020)

## Escenarios visuales / accesibilidad (US2 y US3)

10. **Consistencia** → recorrer marketplace, detalle de sede, mis reservas, partidos, login,
    registro y panel (dashboard, sedes, detalle, agenda).
    - Esperado: mismos botones, cards, campos, y mismos estados de carga/vacío/error. (SC-002)
11. **Estados** → forzar carga (lenta), sin resultados de búsqueda, y error (API caída).
    - Esperado: Spinner/Skeleton, EmptyState y ErrorBanner consistentes en cada caso.
12. **Teclado** → navegar toda la app solo con Tab/Shift+Tab/Enter/Escape.
    - Esperado: foco visible en el 100% de los controles; diálogos cierran con Escape. (SC-003)
13. **Contraste** → verificar texto principal y controles cumplen AA (herramienta de contraste). (SC-004)
14. **Responsive** → probar en anchos 360px, 768px, 1024px, 1440px.
    - Esperado: sin desbordes horizontales ni solapamientos. (SC-005)

## No-regresión (SC-008)

15. Flujos completos: buscar sede → ver detalle → reservar franja → pagar (checkout Wompi sandbox);
    abrir partido; como dueño gestionar sede y ver agenda.
    - Esperado: todos funcionan igual que antes del rediseño (sin cambios de API).
