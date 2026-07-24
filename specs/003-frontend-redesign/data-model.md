# Data Model: Design tokens + reglas de validación

Esta feature no introduce entidades de datos de negocio. El "modelo" aquí son los **tokens del
sistema de diseño** y las **reglas/estados de validación** de los formularios.

## 1. Design tokens (definidos en `frontend/src/index.css` vía `@theme`)

### Color

| Token | Uso | Notas |
|-------|-----|-------|
| `--color-brand-50..900` | Verde de marca (emerald) | Refinado; 600/700 para CTAs, 50/100 para superficies suaves |
| `--color-accent-*` | Acento premium (teal/lima) | Datos destacados, precios, foco de énfasis |
| `--color-surface` / `--color-surface-muted` | Fondos de página y cards | Neutro cálido |
| `--color-ink` / `--color-ink-muted` | Texto principal / secundario | Contraste AA sobre superficies |
| `--color-danger-*`, `--color-warning-*`, `--color-info-*` | Estados | Errores, pendiente, informativos |

### Tipografía

| Token | Uso |
|-------|-----|
| `--font-display` | Titulares (peso 700–800), fuentes de sistema |
| `--font-sans` | Cuerpo/UI |
| Escala | `text-xs … text-4xl` con line-height y tracking ajustados para titulares |

### Forma y elevación

| Token | Uso |
|-------|-----|
| `--radius-card` (2xl/3xl), `--radius-control` (lg) | Radios consistentes |
| `--shadow-sm/md/lg` | Sombras suaves multicapa (premium) |
| Motion | Transición 150–200ms; respeta `prefers-reduced-motion` |

## 2. Estados de componente compartido

- **Button**: variantes `primary | secondary | ghost | danger` × tamaños `sm | md | lg`;
  estados `hover | focus-visible | active | disabled | loading`.
- **Field / TextInput**: estados `default | focus | error | disabled`; mensaje de error asociado
  vía `aria-describedby` con `role="alert"`.
- **Estados de página**: `loading` (Spinner/Skeleton), `empty` (EmptyState), `error` (ErrorBanner).

## 3. Reglas de validación (implementadas en `frontend/src/lib/validation.ts`)

### Campo: nombre
- Requerido. Se recorta (`trim`); si queda vacío → error "El nombre es obligatorio".

### Campo: correo
- Formato válido (regex pragmática `^[^\s@]+@[^\s@]+\.[^\s@]+$`).
- Vacío en registro/login → "El correo es obligatorio"; formato inválido → "Correo no válido".

### Campo: teléfono (opcional)
- Vacío → válido.
- Presente → 7–15 dígitos, admite `+`, espacios, guiones, paréntesis. Inválido → "Teléfono no válido".

### Campo: contraseña (registro)
- Longitud ≥ 8 → si no, "Mínimo 8 caracteres".
- Contiene al menos una letra **y** un número → si no, "Debe incluir letras y números".
- Ambas condiciones deben cumplirse para ser válida. `123456` → inválida.

### Campo: confirmación de contraseña (registro)
- Debe coincidir con contraseña → si no, "Las contraseñas no coinciden".

### Campo: contraseña (login)
- No vacía → si no, "La contraseña es obligatoria". (No se aplica política a login; el backend valida.)

### Medidor de fortaleza (registro)
- `passwordStrength(pw)` → entero 0–4 según longitud (≥8, ≥12) y variedad de clases de carácter
  (minúscula, mayúscula, dígito, símbolo).
- Mapeo a etiqueta: 0 Muy débil · 1 Débil · 2 Aceptable · 3 Fuerte · 4 Excelente.

## 4. Estado de formulario (hook `useFormValidation`)

| Campo del estado | Tipo | Descripción |
|------------------|------|-------------|
| `values` | objeto | Valores actuales por campo |
| `errors` | objeto | Mensaje de error por campo (derivado de validadores) |
| `touched` | objeto | Campos que el usuario ya tocó (para mostrar error tras blur/submit) |
| `isValid` | boolean | `true` si no hay errores en ningún campo requerido |
| `setValue(name, v)` | fn | Actualiza valor y re-valida |
| `setTouched(name)` | fn | Marca campo como tocado |
| `validateAll()` | fn | Marca todo como tocado y devuelve validez (para submit) |

## 5. Reglas de negocio (sin cambios)

Las reglas de dominio (reservas, pagos, cancelaciones) permanecen **exclusivamente en el backend**.
La validación de cliente es de entrada/UX y no duplica ni sustituye dichas reglas (FR-021, FR-023).
