# Contrato: Validación de formularios

API pública de `frontend/src/lib/validation.ts` y `frontend/src/hooks/useFormValidation.ts`,
y su aplicación por formulario. El backend sigue siendo la autoridad final (FR-021, FR-023).

## `lib/validation.ts` (funciones puras)

```
validateRequired(value: string, label?: string): string | null
validateEmail(value: string): string | null
validatePhone(value: string): string | null        // '' => null (válido, opcional)
validatePasswordPolicy(value: string): string | null // ≥8 y letra+número
validateMatch(a: string, b: string, msg?: string): string | null
passwordStrength(value: string): number             // 0..4
passwordStrengthLabel(score: number): string        // 'Muy débil'..'Excelente'
```

- Devuelven `null` cuando el valor es válido, o un mensaje en español cuando no.
- No tienen efectos secundarios ni dependencias de React.

## `hooks/useFormValidation.ts`

```
useFormValidation<TValues>(config: {
  initial: TValues,
  validators: { [K in keyof TValues]?: (value, allValues) => string | null }
}): {
  values, errors, touched, isValid,
  setValue(name, value), setTouched(name), reset(), validateAll(): boolean
}
```

- `errors[name]` solo se considera "mostrable" cuando `touched[name]` es `true` o tras
  `validateAll()`.
- `isValid` es `true` cuando ningún validador devuelve mensaje para los valores actuales.

## Aplicación por formulario

### Registro (`RegisterPage`) — P1
| Campo | Validador | Regla |
|-------|-----------|-------|
| name | `validateRequired` | No vacío (trim) |
| email | `validateEmail` | Formato válido |
| phone | `validatePhone` | Opcional; formato si presente |
| password | `validatePasswordPolicy` | ≥8, letra+número (rechaza `123456`) |
| confirmPassword | `validateMatch(password, confirm)` | Coincide |

- Muestra `PasswordStrengthMeter` con `passwordStrength(password)`.
- Botón "Crear cuenta" `disabled` mientras `!isValid`.
- Error del backend (p. ej. correo duplicado) se muestra en `ErrorBanner` sin borrar los campos.

### Login (`LoginPage`) — P2
| Campo | Validador | Regla |
|-------|-----------|-------|
| email | `validateEmail` | Formato válido |
| password | `validateRequired` | No vacío (sin política) |

- Botón "Ingresar" `disabled` mientras `!isValid`. Botones de demo siguen funcionando.

### Panel de dueño (formularios en `OwnerVenueDetailPage` y otros del panel) — P2
- Campos requeridos (nombre de sede/cancha, horarios, precios, etc.): `validateRequired` y, donde
  aplique, validación numérica/rango. Guardado bloqueado mientras haya errores.
- Mismo patrón visual de error por campo que el registro.

## Criterios de aceptación mapeados
- FR-011..FR-018 → Registro. FR-019 → Login. FR-020 → Panel. FR-021 → manejo de error backend.
- SC-001 (100% de contraseñas débiles rechazadas), SC-006 (registro < 2 min), SC-007 (patrón
  consistente).
