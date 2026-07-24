# Contrato: Sistema de diseño (componentes UI)

Contrato de los componentes compartidos en `frontend/src/components/ui.tsx`. Las páginas SOLO
deben consumir estos componentes/tokens; no deben redefinir estilos ad-hoc que rompan la
consistencia (SC-002, SC-007).

## Componentes existentes (se restilan, se mantiene la API pública)

| Componente | Props actuales | Cambio |
|-----------|----------------|--------|
| `Button` | `variant`, `size`, `...button` | Añadir tamaño `lg` y estado `loading` opcional; restyling premium |
| `buttonClasses(variant,size,extra)` | — | Se mantiene firma; nuevos tokens |
| `Card` | `children`, `className` | Radio/sombra premium |
| `Badge` | `children`, `className` | Sin cambio de API |
| `Spinner` | `label?` | Sin cambio de API |
| `ErrorBanner` | `message` | Restyle; `role="alert"` |
| `EmptyState` | `title`, `subtitle?` | Restyle; permitir `icon?`/`action?` opcionales |
| `Field` | `label`, `htmlFor?`, `children`, `hint?` | Añadir `error?` y `required?`; asociación a11y |
| `inputClasses` | — | Nuevos tokens; variante de error |
| `ModalShell` | `children`, `onClose`, `size?` | Restyle; foco atrapado + cierre con Escape |

## Componentes nuevos

| Componente | Props | Propósito |
|-----------|-------|-----------|
| `TextInput` | `value`, `onChange`, `error?`, `type?`, `...input` | Input controlado con estilo de error integrado y `aria-invalid`/`aria-describedby` |
| `PasswordField` | `value`, `onChange`, `error?`, `showToggle?` | Input de contraseña con botón mostrar/ocultar accesible |
| `PasswordStrengthMeter` | `score` (0–4), `label` | Barra + etiqueta de fortaleza |
| `FieldError` | `message?` | Mensaje de error por campo, `role="alert"`, oculto si vacío |
| `SectionHeading` | `title`, `subtitle?`, `action?` | Encabezado de sección consistente |
| `Skeleton` | `className` | Placeholder de carga premium (opcional donde aplique) |

## Invariantes del contrato

1. **Compatibilidad**: No romper las props ya usadas por las páginas; los cambios son aditivos o
   de estilo. Cualquier cambio de firma debe actualizar todas las llamadas.
2. **Tokens**: Todo color/sombra/radio/tipografía viene de tokens `@theme`; nada hardcodeado
   por fuera del sistema salvo casos justificados.
3. **Accesibilidad**: Todo control tiene `focus-visible` visible; inputs asocian label y error;
   diálogos manejan foco y Escape; contraste AA.
4. **Motion**: Transiciones ≤200ms y desactivadas bajo `prefers-reduced-motion`.
5. **Responsive**: Componentes fluidos; sin anchos fijos que provoquen desborde en 360px.
